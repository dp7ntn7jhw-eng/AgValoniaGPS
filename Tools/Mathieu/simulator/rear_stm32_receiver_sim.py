#!/usr/bin/env python3
import socket
import time
from dataclasses import dataclass

HOST = "127.0.0.1"
PORT = 12000

MAX_STEER_CDEG = 2500          # ±25.00°
COMMAND_TIMEOUT_S = 1.50       # timeout futur STM32
MAX_ABS_XTE_MM = 10000         # ±10 m, sécurité diagnostic


@dataclass
class RearCommand:
    seq: int
    ms: int
    valid: int
    xte_mm: int
    steer_cdeg: int
    flags: int
    track: str
    reason: str
    crc: str


def compute_checksum(payload: str) -> int:
    checksum = 0
    for ch in payload:
        checksum = (checksum + ord(ch)) & 0xFFFF
    return checksum


def parse_message(message: str):
    marker = ",crc="
    if marker not in message:
        return None, "missing crc"

    payload, crc_text = message.rsplit(marker, 1)
    crc_text = crc_text.strip()

    try:
        expected_crc = int(crc_text, 16)
    except ValueError:
        return None, "invalid crc format"

    actual_crc = compute_checksum(payload)
    if actual_crc != expected_crc:
        return None, f"bad crc expected={expected_crc:04X} actual={actual_crc:04X}"

    parts = payload.split(",")
    if not parts or parts[0] != "REARV1":
        return None, "unsupported protocol"

    fields: dict[str, str] = {}

    for part in parts[1:]:
        if "=" not in part:
            return None, f"invalid field '{part}'"
        key, value = part.split("=", 1)
        fields[key] = value

    required = ["seq", "ms", "valid", "xte_mm", "steer_cdeg", "flags", "track", "reason"]
    for key in required:
        if key not in fields:
            return None, f"missing field {key}"

    try:
        command = RearCommand(
            seq=int(fields["seq"]),
            ms=int(fields["ms"]),
            valid=int(fields["valid"]),
            xte_mm=int(fields["xte_mm"]),
            steer_cdeg=int(fields["steer_cdeg"]),
            flags=int(fields["flags"]),
            track=fields["track"],
            reason=fields["reason"],
            crc=crc_text,
        )
    except ValueError as exc:
        return None, f"invalid numeric field: {exc}"

    return command, "ok"


def validate_command(command: RearCommand, last_seq):
    missed_frames = None

    if last_seq is not None:
        expected = (last_seq + 1) & 0xFFFF
        if command.seq != expected:
            if command.seq > expected:
                missed_frames = command.seq - expected
            else:
                missed_frames = (65536 - expected) + command.seq

    if command.valid != 1:
        return False, f"invalid command: {command.reason or 'valid=0'}", missed_frames

    if abs(command.xte_mm) > MAX_ABS_XTE_MM:
        return False, f"xte out of range: {command.xte_mm} mm", missed_frames

    if abs(command.steer_cdeg) > MAX_STEER_CDEG:
        return False, f"steer out of range: {command.steer_cdeg} cdeg", missed_frames

    # Flags expected when command is valid:
    # bit0 rear GPS fix, bit1 rear recent, bit2 active track, bit3 diagnostic valid, bit4 command valid.
    required_flags = 0b11111
    if (command.flags & required_flags) != required_flags:
        return False, f"missing required flags: flags={command.flags}", missed_frames

    if not command.track or command.track == "n/a":
        return False, "missing active track", missed_frames

    return True, "accepted", missed_frames


def main() -> None:
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((HOST, PORT))
    sock.settimeout(0.1)

    print(f"Rear STM32 receiver simulator listening on UDP {HOST}:{PORT}")
    print("Ctrl+C pour arrêter")
    print()

    last_seq = None
    last_receive_time = time.monotonic()
    timeout_reported = False

    while True:
        try:
            data, addr = sock.recvfrom(4096)
        except socket.timeout:
            age = time.monotonic() - last_receive_time
            if age > COMMAND_TIMEOUT_S and not timeout_reported:
                print(f"TIMEOUT: no rear command for {age:.2f}s -> command disabled")
                timeout_reported = True
            continue

        now = time.monotonic()
        last_receive_time = now
        timeout_reported = False

        message = data.decode("ascii", errors="replace")
        command, parse_status = parse_message(message)

        if command is None:
            print(f"REJECT parse_error='{parse_status}' raw='{message}'")
            continue

        accepted, reason, missed_frames = validate_command(command, last_seq)

        seq_note = ""
        if missed_frames is not None and missed_frames > 0:
            seq_note = f" missed={missed_frames}"

        last_seq = command.seq

        steer_deg = command.steer_cdeg / 100.0
        xte_m = command.xte_mm / 1000.0

        if accepted:
            print(
                f"ACCEPT seq={command.seq}{seq_note} "
                f"xte={xte_m:+.3f}m steer={steer_deg:+.2f}deg "
                f"flags={command.flags} track={command.track} crc={command.crc}"
            )
        else:
            print(
                f"REJECT seq={command.seq}{seq_note} reason='{reason}' "
                f"xte={xte_m:+.3f}m steer={steer_deg:+.2f}deg "
                f"flags={command.flags} track={command.track} crc={command.crc}"
            )


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nRear STM32 receiver simulator stopped.")
