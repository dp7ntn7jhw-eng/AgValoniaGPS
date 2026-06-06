#!/usr/bin/env python3
import socket

HOST = "127.0.0.1"
PORT = 12001


def compute_checksum(payload: str) -> int:
    checksum = 0
    for ch in payload:
        checksum = (checksum + ord(ch)) & 0xFFFF
    return checksum


def check_crc(message: str) -> str:
    marker = ",crc="
    if marker not in message:
        return "crc=missing"

    payload, crc_text = message.rsplit(marker, 1)

    try:
        expected = int(crc_text.strip(), 16)
    except ValueError:
        return "crc=invalid-format"

    actual = compute_checksum(payload)

    if actual == expected:
        return "crc=ok"

    return f"crc=bad expected={expected:04X} actual={actual:04X}"


sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((HOST, PORT))

print(f"Listening rear STM32 status on UDP {HOST}:{PORT}")
print("Ctrl+C pour arrêter")

while True:
    data, addr = sock.recvfrom(4096)
    message = data.decode("ascii", errors="replace")
    print(f"{addr}: {message} | {check_crc(message)}")
