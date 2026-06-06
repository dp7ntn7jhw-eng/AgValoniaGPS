#!/usr/bin/env python3
import socket
import time
import math

HOST = "127.0.0.1"
PORT = 12001


def compute_checksum(payload: str) -> int:
    checksum = 0
    for ch in payload:
        checksum = (checksum + ord(ch)) & 0xFFFF
    return checksum


sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

seq = 0
t0 = time.time()

print(f"Sending simulated rear STM32 status to UDP {HOST}:{PORT}")
print("Ctrl+C pour arrêter")

while True:
    elapsed = time.time() - t0
    xte_mm = int(round(250.0 * math.sin(elapsed / 4.0)))
    steer_cdeg = int(round(-8.0 * xte_mm / 10.0))
    target_adc = 2048 + int(round(steer_cdeg * 0.45))
    feedback_adc = target_adc + int(round(20.0 * math.sin(elapsed)))
    pwm = int(round((target_adc - feedback_adc) * 0.5))

    payload = (
        f"REAR_STATUS,seq={seq},"
        f"cmd_valid=1,"
        f"accepted=1,"
        f"xte_mm={xte_mm},"
        f"steer_cdeg={steer_cdeg},"
        f"target_adc={target_adc},"
        f"feedback_adc={feedback_adc},"
        f"pwm={pwm},"
        f"enabled=1,"
        f"timeout=0"
    )

    crc = compute_checksum(payload)
    message = f"{payload},crc={crc:04X}"

    sock.sendto(message.encode("ascii"), (HOST, PORT))

    print(message)

    seq = (seq + 1) & 0xFFFF
    time.sleep(0.5)
