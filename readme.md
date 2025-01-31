# SAE S6
[![forthebadge](https://forthebadge.com/images/badges/made-with-c-sharp.svg)](https://forthebadge.com)
[![forthebadge](https://forthebadge.com/images/badges/made-with-python.svg)](https://forthebadge.com)
[![forthebadge](https://forthebadge.com/images/badges/made-with-c.svg)](https://forthebadge.com)

Le projet consiste à créer une voiture complétement autonome, la seule interaction avec l'utilisateur doit être le lancement du programme via les boutons présent sur la voiture.
Pour plus d'informations on peut s'appuyer sur ce [repos GIT](https://github.com/ajuton-ens/CourseVoituresAutonomesSaclay).

## Pour commencer

Voici les différents éléments présent dans la voiture :

|                   | Rpi  | STM32 |
|-------------------|------|-------|
| accéléromètre     | I2C  |       |
| Lidar             | UART |       |
| caméra            | X    |       |
| capteur ultrasons |      | I2C   |
| moteur direction  |      | GPIO  |
| moteur propulsion |      | GPIO  |
| écran oled        | I2C  |       |
| buzzer            | GPIO |       |
| boutons poussoir  | GPIO |       |


### étapes de conception

Ce que nous devons réaliser :

- Découverte du cahier des charges
- Mise en place du capteur ultrasons
- Mise en place de la caméra (détection rouge/vert)
- Mise en place du LiDAR
- Mise en place des boutons poussoirs
- Mise en place du buzzer et boutons poussoirs

### Programmation
Le code coté raspberry pi sera écrit en `C#` et le code coté STM32 sera réalisé en `C` ou en `C++`. 
Si nous ne parvenons pas à trouver des bibliothèques adapté en `C#` nous pourrons utiliser du code `python` qui sera exécuté par le code `C#`.
La majorité du code doit être sur la raspberry pi car plus puissant et plus simple à debbuger, le code de la STM32 ne doit servir que d'interface entre le raspberry pi et les capteurs et actioneurs.
### Outils

Comme outils je recommande d'utiliser le logiciel [Rider](https://www.jetbrains.com/fr-fr/rider/) pour le code C#, [PyCharm](https://www.jetbrains.com/fr-fr/pycharm/) pour le code python et [CLion](https://www.jetbrains.com/fr-fr/clion/) pour le code C de la STM32.

## Fabriqué avec

Entrez les programmes/logiciels/ressources que vous avez utilisé pour développer votre projet

_exemples :_
* [Materialize.css](http://materializecss.com) - Framework CSS (front-end)
* [Atom](https://atom.io/) - Editeur de textes

## Contributing

Si vous souhaitez contribuer, lisez le fichier [CONTRIBUTING.md](https://example.org) pour savoir comment le faire.

## Versions
Listez les versions ici 
_exemple :_
**Dernière version stable :** 5.0
**Dernière version :** 5.1
Liste des versions : [Cliquer pour afficher](https://github.com/your/project-name/tags)
_(pour le lien mettez simplement l'URL de votre projets suivi de ``/tags``)_

## Auteurs
Listez le(s) auteur(s) du projet ici !
* **Jhon doe** _alias_ [@outout14](https://github.com/outout14)

Lisez la liste des [contributeurs](https://github.com/your/project/contributors) pour voir qui à aidé au projet !

_(pour le lien mettez simplement l'URL de votre projet suivi de ``/contirubors``)_

## License

Ce projet est sous licence ``exemple: WTFTPL`` - voir le fichier [LICENSE.md](LICENSE.md) pour plus d'informations

