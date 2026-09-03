using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AutoCadCopilot.Commands
{
    public class TestCommands
    {
        [CommandMethod("TEST_READ_ENTITIES")]
        public void TestReadEntities()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int lineCount = 0;
            int textCount = 0;
            int blockCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Ouvrir l'espace objet pour la lecture
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // Parcourir les entités de l'espace objet
                foreach (ObjectId objId in btr)
                {
                    Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);

                    if (ent is Line) lineCount++;
                    else if (ent is DBText || ent is MText) textCount++;
                    else if (ent is BlockReference) blockCount++;
                }

                tr.Commit();
            }

            ed.WriteMessage($"\n[AutoCadCopilot] Analyse terminée. Lignes: {lineCount}, Textes: {textCount}, Blocs: {blockCount}\n");
        }

        [CommandMethod("TEST_CREATE_BLOCK")]
        public void TestCreateBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string layerName = "ELEC_PROPOSITION";
            string blockName = "TEST_ELEC_BLOCK";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 1. Gérer le calque
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    LayerTableRecord ltr = new LayerTableRecord();
                    ltr.Name = layerName;
                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Rouge
                    ObjectId layerId = lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }

                // 2. Gérer la définition du bloc (BlockTableRecord)
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId blockDefId = ObjectId.Null;

                if (!bt.Has(blockName))
                {
                    bt.UpgradeOpen();
                    BlockTableRecord btrNew = new BlockTableRecord();
                    btrNew.Name = blockName;

                    // Ajouter une géométrie au bloc (un cercle)
                    Circle circle = new Circle();
                    circle.Center = Point3d.Origin;
                    circle.Radius = 5.0;
                    btrNew.AppendEntity(circle);

                    blockDefId = bt.Add(btrNew);
                    tr.AddNewlyCreatedDBObject(btrNew, true);
                    tr.AddNewlyCreatedDBObject(circle, true);
                }
                else
                {
                    blockDefId = bt[blockName];
                }

                // 3. Insérer la référence de bloc dans l'espace objet
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                BlockReference br = new BlockReference(new Point3d(0, 0, 0), blockDefId);
                br.Layer = layerName;

                modelSpace.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                tr.Commit();
                ed.WriteMessage($"\n[AutoCadCopilot] Bloc '{blockName}' inséré avec succès sur le calque '{layerName}'.\n");
            }
        }
    }
}
