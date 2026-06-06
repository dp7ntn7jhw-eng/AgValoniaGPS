# Rear axle STM32/W5500 receiver

Prototype PlatformIO pour recevoir la consigne arrière publiée par AgValoniaGPS.

## Matériel cible

- STM32F103 Blue Pill
- Module Ethernet W5500
- Framework Arduino
- UDP port 12000

## Protocole reçu

Exemple :

REARV1,seq=42,ms=123456,valid=1,xte_mm=248,steer_cdeg=-198,flags=31,track=Sim_AB_North,reason=,crc=1A2B

## Sécurités implémentées

- CRC ASCII 16 bits par somme des octets
- contrôle compteur seq
- rejet valid=0
- rejet flags incomplets
- rejet steer_cdeg hors +/-2500
- rejet xte_mm hors +/-10000
- timeout si aucune trame pendant 1500 ms

## À venir

- conversion steer_cdeg en position de vérin
- boucle PID vérin
- retour diagnostic UDP vers AgValoniaGPS
- arrêt matériel de sécurité

## Commande vérin prototype

Brochage proposé :

- IBT-2 RPWM : PB0
- IBT-2 LPWM : PB1
- IBT-2 REN  : PB10
- IBT-2 LEN  : PB11
- Potentiomètre retour : PA0

Calibration provisoire :

- -25 degrés : ADC 900
- 0 degré    : ADC 2048
- +25 degrés : ADC 3196

La commande est désactivée automatiquement si aucune trame UDP valide n'est reçue pendant 1500 ms.
