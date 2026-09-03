# Copilote d'Implantation Électrique pour AutoCAD (MVP - Phase 1)

Ce projet est un prototype de copilote d'implantation électrique développé pour AutoCAD 2023.
Il est écrit en C# et cible le `.NET Framework 4.8` via l'API AutoCAD Managed.

Ce dépôt contient l'état d'avancement de la **Phase 1** du projet, dont l'objectif est d'établir la connexion avec AutoCAD, de lire les entités du dessin et d'écrire de la géométrie de base de façon scriptée.

## Architecture

L'architecture globale et l'explication des choix de conception sont disponibles dans le dossier `docs/` :
- [ARCHITECTURE.md](docs/ARCHITECTURE.md)

## Instructions de compilation et de lancement (Windows)

Puisque ce plugin interagit directement avec l'API C# locale d'AutoCAD, vous devez le compiler depuis une machine Windows équipée d'AutoCAD 2023.

### 1. Préparation dans Visual Studio
1. Ouvrez Visual Studio (2019 ou 2022).
2. Créez un nouveau projet "Bibliothèque de classes (.NET Framework)" ciblant **.NET Framework 4.8**.
3. Remplacez les fichiers générés par défaut par les fichiers présents dans le dossier `src/AutoCadCopilot/` de ce dépôt.
4. Dans l'Explorateur de solutions, faites un clic droit sur "Références" -> "Ajouter une référence".
5. Cliquez sur "Parcourir" et allez chercher les DLLs suivantes dans le répertoire d'installation d'AutoCAD 2023 (par défaut `C:\Program Files\Autodesk\AutoCAD 2023\`) :
   - `acmgd.dll`
   - `acdbmgd.dll`
   - `accoremgd.dll`
6. **Important** : Dans les propriétés de ces 3 références, mettez l'option **"Copie locale" (Copy Local) sur `False`**.
7. Compilez le projet en mode **Release** ou **Debug**. Cela générera un fichier `AutoCadCopilot.dll`.

### 2. Chargement dans AutoCAD 2023
1. Lancez AutoCAD 2023 et ouvrez un plan DWG (ou un nouveau dessin).
2. Dans la ligne de commande AutoCAD, tapez la commande `NETLOAD`.
3. Une boîte de dialogue s'ouvre : sélectionnez le fichier `AutoCadCopilot.dll` que vous venez de compiler.
4. Dans la console AutoCAD, vous devriez voir le message : `[AutoCadCopilot] Plugin initialisé avec succès.`

### 3. Exécution des commandes de test (Critères de réussite de la Phase 1)

Ce MVP fournit deux commandes pour valider l'intégration :

**A. Tester la lecture**
- Tapez `TEST_READ_ENTITIES` dans AutoCAD.
- Le plugin analysera l'espace objet et affichera dans la console AutoCAD un résumé du nombre de lignes, textes et blocs présents.
- *Succès si* : Les nombres correspondent approximativement au contenu du DWG (sans planter).

**B. Tester l'écriture et le mode proposition**
- Tapez `TEST_CREATE_BLOCK` dans AutoCAD.
- Le plugin créera automatiquement un calque nommé `ELEC_PROPOSITION` (en rouge).
- Il définira un nouveau bloc nommé `TEST_ELEC_BLOCK` contenant un cercle de rayon 5.
- Il insérera ce bloc aux coordonnées (0,0,0).
- *Succès si* : Le bloc rouge apparaît bien à l'origine du dessin.

## Prochaines étapes (Phase 2)
Une fois cette Phase 1 validée sur votre poste, la Phase 2 consistera à ajouter le "Moteur Géométrique" et "l'Analyseur Architectural" pour détecter les contours de pièces et lire spécifiquement leurs noms dans le DWG, préparant ainsi le terrain pour les règles métier.