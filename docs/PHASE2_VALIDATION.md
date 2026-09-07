# PHASE 2 : VALIDATION ET RAPPORT

Ce document constitue le rapport de fin de Phase 2, détaillant l'architecture implémentée, les résultats des tests synthétiques, les limites connues de l'algorithme MVP, ainsi que la procédure de validation dans AutoCAD 2023.

## 1. Architecture Finale de la Phase 2

La Phase 2 se décompose en plusieurs sous-systèmes indépendants (respectant la consigne de ne pas utiliser d'IA pour la géométrie) :
- **Extraction (`AutoCAD/EntityExtractor.cs`)** : Lit le DWG et convertit les lignes et polylignes en objets neutres `SegmentInfo`, et les textes en `TextEntityInfo`.
- **Analyse des Segments (`Geometry/SegmentAnalyzer.cs`)** : Tolère les calques non standards ("0", "Dessin") si la géométrie est pertinente (Polyligne fermée ou Ligne de plus de 50 unités). Différencie rigoureusement les "doublons stricts" (rejetés) des "lignes parallèles" (ex: murs à deux faces, conservés). Génère un `SegmentDiagnosticReport`.
- **Détection Géométrique (`Geometry/SpaceDetector.cs`)** : Algorithme de reconstruction par "contournement" (Walk-around algorithm). Il scinde les lignes aux intersections, gère un paramètre de "snapping" pour tolérer les portes, et filtre les "contours englobants" via une vérification topologique robuste (`Point-in-Polygon` + Bounding Box stricte).
- **Calculs Métriques (`Geometry/MathUtils.cs`)** : Aire (Gauss), Centre de gravité, et **Périmètre**.
- **Analyse Sémantique (`Analysis/TextAnalyzer.cs` et `RoomClassifier.cs`)** : Filtrage spatial (BoundingBox + RayCasting), catégorisation par taille/contenu (ex: distinction entre un cartouche, une annotation "bureau" de meuble, et un vrai nom de pièce).
- **Interface (`AutoCAD/Visualizer.cs` et `Commands/Phase2Commands.cs`)** : Calques de Debug (`IA_DEBUG_ROOMS` et `IA_DEBUG_REJECTED`) et rapport textuel dans la console.

---

## 2. Résultats des Tests Synthétiques (Simulateur C#)

Le simulateur géométrique autonome valide les cas suivants :

### TEST 1 : Pièce simple (01)
- **Entrée** : 4 murs parfaitement fermés + 1 texte "BUREAU 01".
- **Résultat** : **PASS**. 1 local détecté. Surface: 200,000 (unités carrées). Périmètre: 1800. Classification: BUREAU (Confiance Type: 80%, Géométrie: 100%).

### TEST 2 : Intersections imparfaites et doublons
- **Entrée** : 5 lignes dont un doublon parfait et 3 lignes qui se croisent en dépassant (baveuses).
- **Résultat** : **PASS**. L'algorithme détecte et ignore le doublon, scinde les murs aux intersections exactes, et détecte correctement l'espace unique au centre.

### TEST 3 : Gaps et Portes
- **Entrée** : 6 murs, dont un gap de 5cm (erreur d'intersection) et un gap de 90cm (porte).
- **Résultat** : **PASS**. L'espace est refermé et détecté (Surface: 200,000, Périmètre: 1800). Le gap de 5cm est ignoré car inclus dans l'`EndpointTolerance`. La porte de 90cm déclenche la fermeture virtuelle (`MaxAutoCloseGap`), ajoutant 1 gap au compteur et pénalisant la `GeometryConfidence` (0.90), forçant la pièce à l'état `A_VERIFIER`.

### TEST 4 : Pièces adjacentes avec murs croisés (02)
- **Entrée** : Un grand rectangle divisé par un mur mitoyen en "T".
- **Résultat** : **PASS**. L'algorithme trouve exactement les 2 pièces attendues. Le filtre anti-englobement topologique (vérification stricte de Bounding Box + Point-In-Polygon) a permis d'éliminer le "faux contour extérieur complet" qui était généré par l'algorithme "Walk-around" basique.

### TEST 5 : Sémantique Ambiguë (Meuble Bureau)
- **Entrée** : Une petite géométrie rectangulaire avec un texte "bureau" de hauteur 5.0 (très petit).
- **Résultat** : **PASS**. Le `TextAnalyzer` identifie le texte comme `FURNITURE` et l'ignore lors du `RoomClassifier`. La pièce n'ayant aucun vrai nom, elle tombe en type `Inconnu` (Statut: `A_VERIFIER`), ce qui est le comportement parfaitement attendu pour ne pas générer une fausse pièce de vie "Bureau" sur base d'un texte de mobilier.

### TEST 6 : Murs Multicouches (Lignes parallèles)
- **Entrée** : Un mur représenté par deux carrés imbriqués (une face intérieure, une face extérieure).
- **Résultat** : **PASS**. Le `SegmentAnalyzer` ne les supprime pas abusivement comme "doublon géométrique" mais les identifie bien comme 4 groupes de lignes parallèles, transmettant l'entièreté de la géométrie au moteur d'espace.

---

## 3. Limites Connues (Faux Positifs / Faux Négatifs)

- **Limites d'englobement complexe (Bâtiment en C ou U)** : Bien que l'algorithme anti-englobement supprime les faux contours extérieurs, un bâtiment très complexe (par exemple en U) pourrait toujours tromper l'heuristique de bounding box. Cela nécessiterait une librairie de triangulation de Delaunay complexe qui dépasse le cadre d'un algorithme déterministe autonome en C#.
- **Faux Négatifs** : Si un mur est dessiné avec des splines ou de multiples arcs complexes non décomposés, il sera ignoré car l'extracteur ne gère actuellement que les `Line` et `Polyline` rectilignes.
- **Paramètres de Tolérance** :
  - `EndpointTolerance = 10` : Lignes considérées connectées si l'écart < 10 unités.
  - `MaxAutoCloseGap = 120` : Les trous jusqu'à 120 unités (ex: portes de 90cm) sont pontés virtuellement, pénalisant la confiance.

---

## 4. Procédure de validation AutoCAD 2023

La validation ultime de la Phase 2 doit être réalisée **par vos soins** sur un véritable plan architectural.

### Étapes :
1. Compilez la DLL (`AutoCadCopilot.dll`) via Visual Studio (cible: .NET Framework 4.8).
2. Ouvrez AutoCAD 2023 et chargez un plan architectural DWG représentatif.
3. Tapez `NETLOAD` et sélectionnez la DLL.
4. Tapez la commande `DETECT_ROOMS`.

### Résultats attendus sur le vrai DWG :
- Le plugin affichera un **rapport de diagnostic complet** détaillant pourquoi les segments sont conservés ou rejetés, le top 5 des calques utilisés, et le nombre exact de "doublons stricts" vs "lignes parallèles" détectés.
- Le plugin affichera ensuite un **rapport de détection** textuel détaillé (Nombre d'espaces, nommés, classés, à vérifier, etc.).
- Aucun objet de votre plan original ne sera modifié ni déplacé.
- Un calque **IA_DEBUG_ROOMS** sera créé (Couleur Vert). Il contiendra les polygones des pièces détectées et les textes descriptifs (Type, Surface, Confiance) au centre. Les pièces douteuses (gaps ou classification ambiguë) seront tracées en **Orange**.
- Un calque **IA_DEBUG_REJECTED** sera créé (Couleur Rouge/Bleu). Il affichera les lignes ignorées (doublons ou calques non pertinents).

### Nettoyage :
Pour supprimer l'aperçu, il suffit de supprimer ou masquer les calques commençant par `IA_DEBUG_`.

---
**CRITÈRE DE FIN DE PHASE** : La Phase 2 ne pourra être formellement considérée achevée qu'après un test sur votre poste avec AutoCAD 2023, en confirmant que l'algorithme isole de façon acceptable une majorité des pièces d'un vrai plan. Ne pas entamer la Phase 3 avant cette validation.