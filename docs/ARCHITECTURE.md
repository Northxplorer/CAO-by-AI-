# Architecture du Copilote d'Implantation Électrique pour AutoCAD

Ce document décrit l'architecture globale du plugin AutoCAD, conçu pour être évolutif, maintenable et séparé en différentes couches de responsabilités conformément au cahier des charges.

## 1. Technologies & Prérequis
- **Langage** : C#
- **Plateforme** : .NET Framework 4.8 (Requis pour AutoCAD 2023)
- **API AutoCAD** : ObjectARX (Managed .NET API) avec `acmgd.dll`, `acdbmgd.dll`, `accoremgd.dll`.
- **Formats de données** : JSON (pour les règles métier et la configuration).

## 2. Séparation des responsabilités (Couches)

Le projet est divisé en plusieurs modules conceptuels (qui seront traduits en dossiers/namespaces dans le code) :

### A. Couche d'Intégration AutoCAD (`AutoCadCopilot.AutoCAD`)
- **Rôle** : C'est la seule couche qui interagit directement avec l'API AutoCAD.
- **Responsabilités** :
  - Enregistrement des commandes (`[CommandMethod]`).
  - Lecture des entités du DWG (lignes, blocs, textes).
  - Écriture/Modification du DWG (création de calques, insertion de blocs).
  - Gestion des transactions AutoCAD.

### B. Moteur Géométrique (`AutoCadCopilot.Geometry`)
- **Rôle** : Analyser l'espace indépendamment du format DWG.
- **Responsabilités** :
  - Détection des contours fermés (pièces).
  - Calculs de surface, de distances, intersections.
  - Détermination des points de placement géométriques (ex: centre de la pièce).

### C. Analyseur Architectural & IA (`AutoCadCopilot.Analysis`)
- **Rôle** : Donner du sens à la géométrie et aux textes.
- **Responsabilités** :
  - Associer un texte (ex: "BUREAU 01") à une géométrie (contour).
  - Classification : Déterminer le type de local avec un score de confiance.
  - Préparation des données pour des appels éventuels à une API IA externe si ambiguïté.

### D. Moteur de Règles Métier (`AutoCadCopilot.Rules`)
- **Rôle** : Appliquer les règles électriques configurables.
- **Responsabilités** :
  - Charger les règles depuis un fichier externe (JSON/SQLite).
  - Prendre un "Type de local" en entrée et sortir une "Liste d'équipements à placer".
  - Ne contient **aucune règle en dur** dans le code C#.

### E. Moteur d'Implantation (`AutoCadCopilot.Placement`)
- **Rôle** : Calculer les coordonnées finales des blocs.
- **Responsabilités** :
  - Prendre les intentions du moteur de règles et le contexte géométrique.
  - Éviter les conflits (portes, autres objets).
  - Générer des "Candidats de positionnement".

### F. Interface Utilisateur (`AutoCadCopilot.UI`)
- **Rôle** : Interaction avec l'utilisateur AutoCAD.
- **Responsabilités** :
  - Palettes ou boîtes de dialogue (WPF ou WinForms).
  - Mode Aperçu / Validation.

---

## 3. Arborescence des Fichiers (Phase 1)

Pour la **Phase 1**, nous nous concentrons uniquement sur l'initialisation et la lecture/écriture de base, pour valider que le pont C# <-> AutoCAD fonctionne.

```text
CopilotProject/
├── src/
│   └── AutoCadCopilot/
│       ├── AutoCadCopilot.csproj      # Fichier de projet C# (.NET 4.8)
│       ├── Plugin.cs                  # Point d'entrée du plugin AutoCAD (IExtensionApplication)
│       └── Commands/
│           └── TestCommands.cs        # Commandes pour tester la lecture et la création (Phase 1)
├── docs/
│   └── ARCHITECTURE.md                # Ce document
└── README.md                          # Instructions de compilation et d'utilisation
```

## 4. Plan de la Phase 1 (MVP Technique de base)

**Objectif** : Prouver la capacité à s'interfacer avec AutoCAD 2023, lire le dessin et y injecter des données, sans toucher à l'IA ou aux algorithmes complexes pour le moment.

**Livrables Phase 1** :
1. Une commande `TEST_READ_ENTITIES` qui parcourt le modèle AutoCAD et affiche dans la console le nombre de lignes, de textes et de blocs.
2. Une commande `TEST_CREATE_BLOCK` qui crée un bloc simple (ex: un cercle avec un attribut "TEST") et l'insère au point (0,0) sur un calque "ELEC_PROPOSITION".
3. Un guide pour que l'utilisateur compile la DLL et la charge via `NETLOAD` dans son AutoCAD.
