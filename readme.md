# CovaPsy Tours Groupe 4 - Voiture Autonome

[![forthebadge](https://forthebadge.com/images/badges/made-with-c-sharp.svg)](https://forthebadge.com)
[![forthebadge](https://forthebadge.com/images/badges/made-with-c.svg)](https://forthebadge.com)

## 🏎️ Projet : Voiture Autonome

Ce projet a pour objectif de développer une voiture autonome capable de participer à une course sur un circuit dont le tracé n'est pas connu à l'avance. L'ensemble du système repose principalement sur un Raspberry Pi et une carte STM32, chacun ayant des rôles bien définis.

La voiture fonctionne de manière totalement autonome, et la seule interaction utilisateur consiste à mettre sous tension le véhicule, puis via l'interface utilisateur cliquer sur démarrer. Le programme se lance automatiquement à la mise sous tension du raspberry Pi.

Le git du concours est disponible sur ce [dépôt GitHub](https://github.com/ajuton-ens/CourseVoituresAutonomesSaclay)

<img alt="photo du véhicule" height="300" src="https://github.com/ptitloup-51/SAES6/blob/main/Documents/Photos/Sujet.png" width="300"/>

---
## 🗂️ Architecture du Projet
Le projet est structuré en trois dossiers principaux :

### 🏎️ Dossier `Voiture Autonome`
Contient le projet en C# déployé sur le Raspberry Pi. Ce programme gère :
- Le capteur LiDAR pour la détection d'obstacles et le guidage
- La communication avec la carte STM32 via SPI
- La gestion du serveur web permettant de recevoir des requêtes API
- La gestion de la direction et de la propulsion
- l'algorithme de follow the gap

### 📱️ Dossier `Remote Client`
Contient le projet d'interface utilisateur, développé en C# avec MAUI. Cette interface permet de :
- Superviser l'état du véhicule en temps réel
- démarrer et arrêter la voiture

<p align="Left">
    <img height="400" src="https://github.com/ptitloup-51/SAES6/blob/main/Documents/Photos/Simulator%20Screenshot%20-%20iPhone%2016%20-%202025-03-14%20at%2015.29.53.png"/>
    <img height="400" src="https://github.com/ptitloup-51/SAES6/blob/main/Documents/Photos/Simulator%20Screenshot%20-%20iPhone%2016%20-%202025-03-14%20at%2015.33.29.png"/>
</p>

### 🎚️ Dossier `STM32`
Contient le projet CubeIDE pour la carte STM32, qui gère :
- La communication SPI avec le Raspberry Pi
- La mesure de la vitesse à l'aide d'un encodeur
- La lecture de la tension de la batterie
- L'affichage des données (vitesse, tension) sur un écran OLED

---
## 📝 Caractéristiques Techniques

| Matériel    | Référence                 |
|-------------|---------------------------|
| Moteur      | Mabushi RS-540            |
| Servo moteur | Konect 0913LVMG           |
| Esc         | ?                         |
| Lidar       | Slamtec A2M12             |
| STM32       | STM32G431KB                          |
| Batterie    | T2M Ni-MH de 3000mAh 7,2V |



---
## 📚 Plan de Développement

### 🔢 Étapes de conception

- [x] Analyse du cahier des charges
- [x] Implémentation du la propulsion et de la direction
- [x] Intégration de la caméra pour détection rouge/vert
- [x] Mise en place du LiDAR
- [x] Développement de la communication Raspberry Pi - STM32
- [x] Affichage des valeurs de vitesse et tension sur l'écran OLED
- [x] Création d'une interface client pour le débogage et l'utilisation
- [x] Développement d'un algorithme de follow the gap

### 👨‍💻 Programmation

- **Code sur Raspberry Pi** :
    - Langage utilisé : `C#`
- **Code sur STM32** :
    - Langages : `C`
    - Fonctionnalités principales : communication SPI, lecture capteurs, affichage OLED
- **Code client** :
  - Langage utilisé : `C#`
  - Platforme supportées:
    - MacOS
    - Windows
    - Android
    - IOS
  
Le Raspberry Pi effectue la majeure partie du traitement car il est plus puissant et plus simple à déboguer. La STM32 sert principalement d'interface entre le Raspberry Pi et les capteurs/actionneurs.

---
## 🛠️ Outils de Développement

| Type                                 | Outils que nous avons utilisé                                                                                                               |
|--------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| C# (Raspberry Pi & interface client) | [Rider](https://www.jetbrains.com/fr-fr/rider/)                                                                                             |
| C (STM32)                            | [CLion](https://www.jetbrains.com/fr-fr/clion/) & [CubeIDE](https://www.google.com/search?client=safari&rls=en&q=cubeide&ie=UTF-8&oe=UTF-8) |
| Simulation (C#)                      | [Rider](https://www.jetbrains.com/fr-fr/rider/) & [Unity6](https://unity.com/fr)                                                            |

---
## 📅 Releases

| Version | Détails |
|---------|---------|
| [**1.0**](https://github.com/ptitloup-51/CovaPsy-Tours-G4/releases/tag/v1.0.0) | Dernière version |

_Contient le code source de Remote Client, Le code source de Voiture Autonome, Le code source de STM32 ainsi que les versions compilés de RemoteClient et Voiture Autonome._


[Voir toutes les versions](https://github.com/ptitloup-51/SAES6/tags)

---
## 👤 Auteurs

| Nom                  | Alias GitHub |
|----------------------|-------------|
| **Loup Lavabre**     | [@ptitloup-51](https://github.com/ptitloup-51) |
| **Baptiste De Paul** | [@Baptiste-dp](https://github.com/Baptiste-dp) |
| **Timothée Jaffres** | [@ChiberMoule](https://github.com/ChiberMoule) |


---
##  👏Remerciements
- 🏫 University of Virginia pour l'algorithme de [follow the gap](https://ras.papercept.net/images/temp/IROS/files/3775.pdf)
- 🎓 IUT GEII de Tours
- le repos Github de [asheesh.maheshwari](https://github.com/asheesh1996/RpLidar.NET) pour son implémentation du Lidar en C#

---

🔗 **Retrouvez toutes les mises à jour et contributeurs sur le [repo GitHub](https://github.com/ajuton-ens/CourseVoituresAutonomesSaclay).**



