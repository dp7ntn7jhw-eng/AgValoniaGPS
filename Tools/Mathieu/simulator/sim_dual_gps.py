#!/usr/bin/env python3
import socket
import time
import math
from datetime import datetime, timezone

# Simulateur double GPS pour AgValoniaGPS
# Objectif :
# - GPS avant sur UDP 127.0.0.1:9999
# - GPS arrière sur UDP 127.0.0.1:10000
# - messages NMEA GGA
# - trajectoire simple avec petite dérive latérale

AOG_IP = "127.0.0.1"

GPS_FRONT_PORT = 9999
GPS_REAR_PORT = 10000

START_LAT = 48.205000
START_LON = 7.362000

WHEELBASE_M = 3.00
SPEED_M_S = 1.50
HEADING_DEG = 0.0

SEND_PERIOD_S = 0.20


def meters_to_lat(meters):
    return meters / 111320.0


def meters_to_lon(meters, lat_deg):
    return meters / (111320.0 * math.cos(math.radians(lat_deg)))


def nmea_checksum(sentence_body):
    checksum = 0
    for char in sentence_body:
        checksum ^= ord(char)
    return f"{checksum:02X}"


def decimal_to_nmea_lat(lat):
    lat_abs = abs(lat)
    degrees = int(lat_abs)
    minutes = (lat_abs - degrees) * 60.0
    direction = "N" if lat >= 0 else "S"
    return f"{degrees:02d}{minutes:07.4f}", direction


def decimal_to_nmea_lon(lon):
    lon_abs = abs(lon)
    degrees = int(lon_abs)
    minutes = (lon_abs - degrees) * 60.0
    direction = "E" if lon >= 0 else "W"
    return f"{degrees:03d}{minutes:07.4f}", direction


def make_gga(lat, lon, fix_quality=4, sats=12, altitude_m=250.0):
    now = datetime.now(timezone.utc)
    utc_time = now.strftime("%H%M%S") + ".00"

    nmea_lat, lat_dir = decimal_to_nmea_lat(lat)
    nmea_lon, lon_dir = decimal_to_nmea_lon(lon)

    body = (
        f"GPGGA,{utc_time},"
        f"{nmea_lat},{lat_dir},"
        f"{nmea_lon},{lon_dir},"
        f"{fix_quality},{sats},0.8,"
        f"{altitude_m:.1f},M,48.0,M,,"
    )

    return f"${body}*{nmea_checksum(body)}\r\n"


def offset_position(lat, lon, along_m, lateral_m, heading_deg):
    heading = math.radians(heading_deg)

    north_m = along_m * math.cos(heading) - lateral_m * math.sin(heading)
    east_m = along_m * math.sin(heading) + lateral_m * math.cos(heading)

    new_lat = lat + meters_to_lat(north_m)
    new_lon = lon + meters_to_lon(east_m, lat)

    return new_lat, new_lon


def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    print("=== Simulateur double GPS AgValoniaGPS ===")
    print(f"GPS avant  -> {AOG_IP}:{GPS_FRONT_PORT}")
    print(f"GPS arrière -> {AOG_IP}:{GPS_REAR_PORT}")
    print(f"Position départ : {START_LAT}, {START_LON}")
    print("Ctrl+C pour arrêter")
    print()

    t0 = time.time()

    while True:
        elapsed_s = time.time() - t0
        along_m = SPEED_M_S * elapsed_s

        # Dérive volontaire pour rendre le diagnostic visible.
        lateral_front_m = 0.35 * math.sin(elapsed_s / 8.0)
        lateral_rear_m = 0.20 * math.sin(elapsed_s / 8.0 + 0.4)

        front_lat, front_lon = offset_position(
            START_LAT,
            START_LON,
            along_m + WHEELBASE_M / 2.0,
            lateral_front_m,
            HEADING_DEG,
        )

        rear_lat, rear_lon = offset_position(
            START_LAT,
            START_LON,
            along_m - WHEELBASE_M / 2.0,
            lateral_rear_m,
            HEADING_DEG,
        )

        front_gga = make_gga(front_lat, front_lon)
        rear_gga = make_gga(rear_lat, rear_lon)

        sock.sendto(front_gga.encode("ascii"), (AOG_IP, GPS_FRONT_PORT))
        sock.sendto(rear_gga.encode("ascii"), (AOG_IP, GPS_REAR_PORT))

        print(
            f"t={elapsed_s:6.1f}s | "
            f"front lat={front_lat:.8f} lon={front_lon:.8f} lat_err={lateral_front_m:+.2f}m | "
            f"rear lat={rear_lat:.8f} lon={rear_lon:.8f} lat_err={lateral_rear_m:+.2f}m"
        )

        time.sleep(SEND_PERIOD_S)


if __name__ == "__main__":
    main()
