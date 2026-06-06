#include <Arduino.h>
#include <SPI.h>
#include <Ethernet.h>
#include <EthernetUdp.h>
#include "RearCommandProtocol.h"
#include "ActuatorController.h"

#ifndef REAR_CMD_UDP_PORT
#define REAR_CMD_UDP_PORT 12000
#endif

#ifndef W5500_CS_PIN
#define W5500_CS_PIN PA4
#endif

static constexpr int32_t MAX_STEER_CDEG = 2500;       // ±25.00°
static constexpr int32_t MAX_ABS_XTE_MM = 10000;      // ±10 m
static constexpr uint32_t COMMAND_TIMEOUT_MS = 1500;  // 1.5 s

// Adresse MAC locale fictive. À personnaliser si plusieurs modules.
byte mac[] = { 0x02, 0xA0, 0x47, 0x12, 0x00, 0x02 };

// Réglage IP fixe de test.
// À adapter à ton réseau ou remplacer par DHCP.
IPAddress localIp(192, 168, 1, 62);
IPAddress dnsIp(192, 168, 1, 1);
IPAddress gatewayIp(192, 168, 1, 1);
IPAddress subnetMask(255, 255, 255, 0);

EthernetUDP udp;

// IP du PC AgValoniaGPS qui recevra les retours STM32.
// A adapter plus tard a l'adresse reelle du Mac/PC sur le reseau Ethernet.
IPAddress statusTargetIp(192, 168, 1, 50);
static constexpr uint16_t STATUS_UDP_PORT = 12001;

ActuatorController actuator;

char packetBuffer[256];
char statusBuffer[256];

uint16_t lastSeq = 0;
bool hasLastSeq = false;
uint32_t lastCommandReceivedMs = 0;
bool timeoutReported = false;

bool lastCommandAccepted = false;
bool lastCommandValid = false;
int32_t lastXteMm = 0;
int32_t lastSteerCdeg = 0;
uint16_t statusSeq = 0;

static bool validateCommand(const RearCommand& cmd, char* reason, size_t reasonSize)
{
    if (!cmd.valid)
    {
        snprintf(reason, reasonSize, "valid=0 %s", cmd.reason);
        return false;
    }

    if (labs(cmd.xteMm) > MAX_ABS_XTE_MM)
    {
        snprintf(reason, reasonSize, "xte out of range %ld", static_cast<long>(cmd.xteMm));
        return false;
    }

    if (labs(cmd.steerCdeg) > MAX_STEER_CDEG)
    {
        snprintf(reason, reasonSize, "steer out of range %ld", static_cast<long>(cmd.steerCdeg));
        return false;
    }

    const uint16_t requiredFlags = 0b11111;
    if ((cmd.flags & requiredFlags) != requiredFlags)
    {
        snprintf(reason, reasonSize, "missing flags %u", cmd.flags);
        return false;
    }

    if (cmd.track[0] == '\0' || strcmp(cmd.track, "n/a") == 0)
    {
        snprintf(reason, reasonSize, "missing track");
        return false;
    }

    snprintf(reason, reasonSize, "accepted");
    return true;
}

static void printCommand(const RearCommand& cmd, bool accepted, const char* reason, uint16_t missedFrames)
{
    Serial.print(accepted ? "ACCEPT" : "REJECT");
    Serial.print(" seq=");
    Serial.print(cmd.seq);

    if (missedFrames > 0)
    {
        Serial.print(" missed=");
        Serial.print(missedFrames);
    }

    Serial.print(" xte_mm=");
    Serial.print(cmd.xteMm);

    Serial.print(" steer_cdeg=");
    Serial.print(cmd.steerCdeg);

    Serial.print(" flags=");
    Serial.print(cmd.flags);

    Serial.print(" track=");
    Serial.print(cmd.track);

    Serial.print(" reason=");
    Serial.println(reason);
}

static uint16_t computeMissedFrames(uint16_t seq)
{
    if (!hasLastSeq)
    {
        return 0;
    }

    uint16_t expected = static_cast<uint16_t>(lastSeq + 1);
    if (seq == expected)
    {
        return 0;
    }

    return static_cast<uint16_t>(seq - expected);
}

static void handlePacket(int packetSize)
{
    if (packetSize <= 0)
    {
        return;
    }

    int len = udp.read(packetBuffer, sizeof(packetBuffer) - 1);
    if (len <= 0)
    {
        return;
    }

    packetBuffer[len] = '\0';

    RearCommandParseResult parseResult = rearParseCommand(packetBuffer);

    if (!parseResult.ok)
    {
        Serial.print("REJECT parse=");
        Serial.println(parseResult.error);
        return;
    }

    uint16_t missedFrames = computeMissedFrames(parseResult.command.seq);
    lastSeq = parseResult.command.seq;
    hasLastSeq = true;
    lastCommandReceivedMs = millis();
    timeoutReported = false;

    char reason[80];
    bool accepted = validateCommand(parseResult.command, reason, sizeof(reason));

    lastCommandValid = parseResult.command.valid;
    lastCommandAccepted = accepted;
    lastXteMm = parseResult.command.xteMm;
    lastSteerCdeg = parseResult.command.steerCdeg;

    printCommand(parseResult.command, accepted, reason, missedFrames);

    if (accepted)
    {
        actuator.setEnabled(true);
        actuator.setTargetSteerCdeg(parseResult.command.steerCdeg);
    }
}

static void checkTimeout()
{
    if (lastCommandReceivedMs == 0)
    {
        return;
    }

    uint32_t age = millis() - lastCommandReceivedMs;
    if (age > COMMAND_TIMEOUT_MS && !timeoutReported)
    {
        actuator.setEnabled(false);
        lastCommandAccepted = false;

        Serial.print("TIMEOUT no rear command for ");
        Serial.print(age);
        Serial.println(" ms -> command disabled");
        timeoutReported = true;
    }
}

static uint16_t computeAsciiChecksum(const char* payload)
{
    uint16_t checksum = 0;

    for (const char* p = payload; *p != '\0'; ++p)
    {
        checksum = static_cast<uint16_t>((checksum + static_cast<uint8_t>(*p)) & 0xFFFF);
    }

    return checksum;
}

static void sendRearStatus()
{
    uint16_t seq = statusSeq++;

    snprintf(
        statusBuffer,
        sizeof(statusBuffer),
        "REAR_STATUS,seq=%u,cmd_valid=%u,accepted=%u,xte_mm=%ld,steer_cdeg=%ld,target_adc=%d,feedback_adc=%d,pwm=%d,enabled=%u,timeout=%u",
        seq,
        lastCommandValid ? 1 : 0,
        lastCommandAccepted ? 1 : 0,
        static_cast<long>(lastXteMm),
        static_cast<long>(lastSteerCdeg),
        actuator.getTargetAdc(),
        actuator.getFeedbackAdc(),
        actuator.getLastPwm(),
        actuator.isEnabled() ? 1 : 0,
        timeoutReported ? 1 : 0);

    uint16_t crc = computeAsciiChecksum(statusBuffer);

    char finalMessage[320];
    snprintf(
        finalMessage,
        sizeof(finalMessage),
        "%s,crc=%04X",
        statusBuffer,
        crc);

    udp.beginPacket(statusTargetIp, STATUS_UDP_PORT);
    udp.write(reinterpret_cast<const uint8_t*>(finalMessage), strlen(finalMessage));
    udp.endPacket();
}

void setup()
{
    Serial.begin(115200);
    delay(1000);

    Serial.println();
    Serial.println("Rear axle STM32/W5500 receiver starting...");

    actuator.begin();
    actuator.setEnabled(false);

    Ethernet.init(W5500_CS_PIN);
    Ethernet.begin(mac, localIp, dnsIp, gatewayIp, subnetMask);

    delay(1000);

    Serial.print("IP: ");
    Serial.println(Ethernet.localIP());

    if (udp.begin(REAR_CMD_UDP_PORT) == 0)
    {
        Serial.println("UDP begin failed");
    }
    else
    {
        Serial.print("Listening UDP port ");
        Serial.println(REAR_CMD_UDP_PORT);
    }
}

void loop()
{
    static uint32_t lastStatusMs = 0;

    int packetSize = udp.parsePacket();
    if (packetSize > 0)
    {
        handlePacket(packetSize);
    }

    checkTimeout();
    actuator.update();

    uint32_t now = millis();
    if (now - lastStatusMs >= 500)
    {
        lastStatusMs = now;
        sendRearStatus();
    }
}
