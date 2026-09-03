using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AutoCadCopilot.Analysis;
using AutoCadCopilot.AutoCAD;
using AutoCadCopilot.Geometry;
using AutoCadCopilot.Models;
using System.Collections.Generic;

namespace AutoCadCopilot.Commands
{
    public class Phase2Commands
    {
        [CommandMethod("DETECT_ROOMS")]
        public void DetectRooms()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n=======================================================");
            ed.WriteMessage("\n[AutoCadCopilot] Démarrage de l'analyse architecturale (Phase 2)...");
            ed.WriteMessage("\n=======================================================");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // 1. Extraction
                var extractor = new EntityExtractor();
                var rawSegments = extractor.ExtractSegments(tr, btr);
                var texts = extractor.ExtractTexts(tr, btr);

                ed.WriteMessage($"\n[1] Extraction: {rawSegments.Count} segments bruts, {texts.Count} textes.");

                // 2. Configuration & Analyse des segments
                var config = new RoomDetectionConfig();
                var segmentAnalyzer = new SegmentAnalyzer(config);
                segmentAnalyzer.CategorizeSegments(rawSegments);

                int validWalls = rawSegments.Count(s => s.Category == SegmentCategory.WALL && !s.IsDuplicate);
                ed.WriteMessage($"\n[2] Analyse segments: {validWalls} considérés comme murs valides.");

                // 3. Détection géométrique
                var spaceDetector = new SpaceDetector(config);
                List<Room> rooms = spaceDetector.DetectSpaces(rawSegments);

                // 4. Analyse et Association des textes
                var textAnalyzer = new TextAnalyzer();
                textAnalyzer.AssignTextsToRooms(rooms, texts);

                // 5. Classification
                var classifier = new RoomClassifier();
                classifier.ClassifyRooms(rooms);

                // 6. Visualisation Debug
                var visualizer = new Visualizer();
                visualizer.DrawRoomDebugData(tr, db, rooms);
                // Afficher les murs rejetés et les doublons comme demandé dans l'objectif 8 (DEBUG)
                visualizer.DrawRejectedSegments(tr, db, rawSegments);

                tr.Commit();

                // 7. RAPPORT DE DÉTECTION
                int nbAverifier = rooms.Count(r => r.Statut == RoomStatus.A_VERIFIER);
                int nbGaps = rooms.Count(r => r.NbOuvertures > 0);
                int nbInconnus = rooms.Count(r => r.Type == RoomType.Inconnu || r.Type == RoomType.Autre);
                int textesIgnores = texts.Count(t => t.Category == TextCategory.TITLE_BLOCK || t.Category == TextCategory.DIMENSION);

                ed.WriteMessage("\n\n--- RAPPORT DE DÉTECTION ---");
                ed.WriteMessage($"\nLocaux détectés géométriquement : {rooms.Count}");
                ed.WriteMessage($"\nLocaux avec un nom associé      : {rooms.Count - nbInconnus}");
                ed.WriteMessage($"\nLocaux classifiés avec certitude: {rooms.Count - nbAverifier}");
                ed.WriteMessage($"\nLocaux marqués 'À VÉRIFIER'     : {nbAverifier}");
                ed.WriteMessage($"\nContours avec ouvertures (gaps) : {nbGaps}");
                ed.WriteMessage($"\nTextes ignorés (Bruit/Cartouche): {textesIgnores}");
                ed.WriteMessage("\n----------------------------");
                ed.WriteMessage("\n[AutoCadCopilot] Analyse terminée avec succès. Calque IA_DEBUG_ROOMS généré.\n");
            }
        }
    }
}
