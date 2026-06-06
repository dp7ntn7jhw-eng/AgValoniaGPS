#!/usr/bin/env python3
import socket

HOST = "127.0.0.1"
PORT = 12000

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((HOST, PORT))

print(f"Listening rear axle commands on UDP {HOST}:{PORT}")
print("Ctrl+C pour arrêter")

while True:
    data, addr = sock.recvfrom(4096)
    print(f"{addr}: {data.decode('ascii', errors='replace')}")
