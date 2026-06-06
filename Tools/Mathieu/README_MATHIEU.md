# Projet guidage double essieu - Mathieu

Objectif : adapter AgValoniaGPS pour tester un système de guidage avec deux essieux directeurs indépendants.

Architecture cible :
- GPS avant
- GPS arrière
- ligne AB commune
- calcul d'erreur latérale indépendant pour chaque essieu
- diagnostic temps réel
- sortie UDP vers STM32 avant
- sortie UDP vers STM32 arrière

Branche de travail :
2axles

Premières étapes :
1. compiler uniquement la version Desktop ;
2. créer un simulateur GPS double flux ;
3. identifier l'entrée GPS existante ;
4. ajouter un second flux GPS ;
5. afficher les diagnostics avant/arrière.
