#include "RearCommandProtocol.h"
#include <string.h>
#include <stdlib.h>

static void setError(RearCommandParseResult& result, const char* msg)
{
    result.ok = false;
    strncpy(result.error, msg, sizeof(result.error) - 1);
    result.error[sizeof(result.error) - 1] = '\0';
}

uint16_t rearComputeChecksum(const char* payload)
{
    uint16_t checksum = 0;

    for (const char* p = payload; *p != '\0'; ++p)
    {
        checksum = static_cast<uint16_t>((checksum + static_cast<uint8_t>(*p)) & 0xFFFF);
    }

    return checksum;
}

static bool parseUint16(const char* text, uint16_t& value)
{
    if (text == nullptr || *text == '\0')
    {
        return false;
    }

    char* end = nullptr;
    unsigned long parsed = strtoul(text, &end, 10);

    if (*end != '\0' || parsed > 65535UL)
    {
        return false;
    }

    value = static_cast<uint16_t>(parsed);
    return true;
}

static bool parseUint32(const char* text, uint32_t& value)
{
    if (text == nullptr || *text == '\0')
    {
        return false;
    }

    char* end = nullptr;
    unsigned long parsed = strtoul(text, &end, 10);

    if (*end != '\0')
    {
        return false;
    }

    value = static_cast<uint32_t>(parsed);
    return true;
}

static bool parseInt32(const char* text, int32_t& value)
{
    if (text == nullptr || *text == '\0')
    {
        return false;
    }

    char* end = nullptr;
    long parsed = strtol(text, &end, 10);

    if (*end != '\0')
    {
        return false;
    }

    value = static_cast<int32_t>(parsed);
    return true;
}

static bool parseBool01(const char* text, bool& value)
{
    if (strcmp(text, "1") == 0)
    {
        value = true;
        return true;
    }

    if (strcmp(text, "0") == 0)
    {
        value = false;
        return true;
    }

    return false;
}

static const char* valueAfterEquals(char* token)
{
    char* eq = strchr(token, '=');
    if (eq == nullptr)
    {
        return nullptr;
    }

    *eq = '\0';
    return eq + 1;
}

RearCommandParseResult rearParseCommand(char* message)
{
    RearCommandParseResult result;

    if (message == nullptr || message[0] == '\0')
    {
        setError(result, "empty message");
        return result;
    }

    char* crcMarker = strstr(message, ",crc=");
    if (crcMarker == nullptr)
    {
        setError(result, "missing crc");
        return result;
    }

    *crcMarker = '\0';
    const char* crcText = crcMarker + 5;

    char* end = nullptr;
    unsigned long expectedCrc = strtoul(crcText, &end, 16);

    if (*end != '\0' || expectedCrc > 0xFFFFUL)
    {
        setError(result, "invalid crc format");
        return result;
    }

    uint16_t actualCrc = rearComputeChecksum(message);
    if (actualCrc != static_cast<uint16_t>(expectedCrc))
    {
        snprintf(
            result.error,
            sizeof(result.error),
            "bad crc exp=%04lX act=%04X",
            expectedCrc,
            actualCrc);
        result.ok = false;
        return result;
    }

    char* saveptr = nullptr;
    char* token = strtok_r(message, ",", &saveptr);

    if (token == nullptr || strcmp(token, "REARV1") != 0)
    {
        setError(result, "unsupported protocol");
        return result;
    }

    bool hasSeq = false;
    bool hasMs = false;
    bool hasValid = false;
    bool hasXte = false;
    bool hasSteer = false;
    bool hasFlags = false;
    bool hasTrack = false;
    bool hasReason = false;

    while ((token = strtok_r(nullptr, ",", &saveptr)) != nullptr)
    {
        const char* value = valueAfterEquals(token);
        if (value == nullptr)
        {
            setError(result, "invalid field");
            return result;
        }

        if (strcmp(token, "seq") == 0)
        {
            hasSeq = parseUint16(value, result.command.seq);
        }
        else if (strcmp(token, "ms") == 0)
        {
            hasMs = parseUint32(value, result.command.ms);
        }
        else if (strcmp(token, "valid") == 0)
        {
            hasValid = parseBool01(value, result.command.valid);
        }
        else if (strcmp(token, "xte_mm") == 0)
        {
            hasXte = parseInt32(value, result.command.xteMm);
        }
        else if (strcmp(token, "steer_cdeg") == 0)
        {
            hasSteer = parseInt32(value, result.command.steerCdeg);
        }
        else if (strcmp(token, "flags") == 0)
        {
            uint16_t parsedFlags = 0;
            hasFlags = parseUint16(value, parsedFlags);
            result.command.flags = parsedFlags;
        }
        else if (strcmp(token, "track") == 0)
        {
            strncpy(result.command.track, value, sizeof(result.command.track) - 1);
            result.command.track[sizeof(result.command.track) - 1] = '\0';
            hasTrack = true;
        }
        else if (strcmp(token, "reason") == 0)
        {
            strncpy(result.command.reason, value, sizeof(result.command.reason) - 1);
            result.command.reason[sizeof(result.command.reason) - 1] = '\0';
            hasReason = true;
        }
    }

    if (!hasSeq || !hasMs || !hasValid || !hasXte || !hasSteer || !hasFlags || !hasTrack || !hasReason)
    {
        setError(result, "missing or invalid field");
        return result;
    }

    result.ok = true;
    strcpy(result.error, "ok");
    return result;
}
