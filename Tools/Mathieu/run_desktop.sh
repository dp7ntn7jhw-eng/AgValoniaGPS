#!/bin/bash
set -e

echo "=== Lancement AgValoniaGPS Desktop ==="
dotnet run --project Platforms/AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj
