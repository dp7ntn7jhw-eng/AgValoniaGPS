#!/bin/bash
set -e

echo "=== Build AgValoniaGPS Desktop ==="
dotnet build Platforms/AgValoniaGPS.Desktop/AgValoniaGPS.Desktop.csproj
echo
echo "=== Build Desktop terminé ==="
