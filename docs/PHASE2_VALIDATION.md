# PHASE 2 : VALIDATION ET RAPPORT

Ce document constitue le rapport de fin de Phase 2, détaillant l'architecture implémentée, les résultats des tests synthétiques, les limites connues de l'algorithme MVP, ainsi que la procédure de validation dans AutoCAD 2023.

## 1. Architecture Finale de la Phase 2

La Phase 2 se décompose en plusieurs sous-systèmes indépendants (respectant la consigne de ne pas utiliser d'IA pour la géométrie) :
- **Extraction (`AutoCAD/EntityExtractor.cs`)** : Lit le DWG et convertit les lignes et polylignes en objets neutres `SegmentInfo`, et les textes en `TextEntityInfo`.
- **Analyse des Segments (`Geometry/SegmentAnalyzer.cs`)** : Identifie les doublons et filtre les lignes non-architecturales selon leur calque, leur longueur, etc.
- **Détection Géométrique (`Geometry/SpaceDetector.cs`)** : Algorithme de reconstruction par "contournement" (Walk-around algorithm). Il scinde les lignes aux intersections pour traiter les murs en "T" et gère un paramètre de "snapping" (fermeture de gaps) pour tolérer les ouvertures/portes.
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
- **Entrée** : 6 murs, dont un gap de 5cm (erreur) et un gap de 90cm (porte).
- **Résultat** : **PASS (Partiel)**. L'espace est bien refermé et détecté (Surface: 200,000, Périmètre: 1800). La `GeometryConfidence` chute bien en dessous de 1.0. (Le simulateur compte parfois 1 gap au lieu de 2 selon le sens de parcours, mais la pièce est correctement reconstruite et marquée `A_VERIFIER`).

### TEST 4 : Pièces adjacentes avec murs croisés (02)
- **Entrée** : Un grand rectangle divisé par un mur mitoyen en "T".
- **Résultat** : **FAIL (Limite connue)**. L'algorithme trouve les 2 pièces attendues, mais détecte également l'union des deux (le contour extérieur complet) car la structure Half-Edge complète (faces vs trous) n'est pas implémentée dans ce MVP. Les surfaces des sous-pièces sont cependant exactes.

### TEST 5 : Sémantique Ambiguë (Meuble Bureau)
- **Entrée** : Une petite géométrie rectangulaire avec un texte "bureau" de hauteur 5.0.
- **Résultat** : **PASS**. La classification détecte correctement l'incohérence entre la surface et le type supposé, et force le statut à `A_VERIFIER` avec une `RoomTypeConfidence` effondrée.

---

## 3. Limites Connues (Faux Positifs / Faux Négatifs)

- **Faux Positifs (Locaux Fantômes)** : L'algorithme MVP "tourne-à-gauche/droite" ne construit pas un graphe planaire formel (DCEL / Half-Edge). Conséquence : dans un bâtiment complexe, il détectera chaque pièce, mais il détectera **aussi** le grand contour extérieur du bâtiment comme s'il s'agissait d'une immense "pièce".
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
- Le plugin affichera un **rapport de détection** textuel détaillé (Nombre d'espaces, nommés, classés, à vérifier, etc.).
- Aucun objet de votre plan original ne sera modifié ni déplacé.
- Un calque **IA_DEBUG_ROOMS** sera créé (Couleur Vert). Il contiendra les polygones des pièces détectées et les textes descriptifs (Type, Surface, Confiance) au centre. Les pièces douteuses (gaps ou classification ambiguë) seront tracées en **Orange**.
- Un calque **IA_DEBUG_REJECTED** sera créé (Couleur Rouge/Bleu). Il affichera les lignes ignorées (doublons ou calques non pertinents).

### Nettoyage :
Pour supprimer l'aperçu, il suffit de supprimer ou masquer les calques commençant par `IA_DEBUG_`.

---
**CRITÈRE DE FIN DE PHASE** : La Phase 2 ne pourra être formellement considérée achevée qu'après un test sur votre poste avec AutoCAD 2023, en confirmant que l'algorithme isole de façon acceptable une majorité des pièces d'un vrai plan. Ne pas entamer la Phase 3 avant cette validation.