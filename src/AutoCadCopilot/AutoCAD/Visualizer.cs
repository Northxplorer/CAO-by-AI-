using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AutoCadCopilot.Models;
using AutoCadCopilot.Geometry;
using System.Collections.Generic;

namespace AutoCadCopilot.AutoCAD
{
    public class Visualizer
    {
        public void DrawRoomDebugData(Transaction tr, Database db, List<Room> rooms)
        {
            string layerName = "IA_DEBUG_ROOMS";

            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = layerName;
                ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 3); // Vert par défaut
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }

            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            foreach (var room in rooms)
            {
                if (room.Contour.Count > 2)
                {
                    Polyline poly = new Polyline();
                    for (int i = 0; i < room.Contour.Count; i++)
                    {
                        poly.AddVertexAt(i, new Point2d(room.Contour[i].X, room.Contour[i].Y), 0, 0, 0);
                    }
                    poly.Closed = true;
                    poly.Layer = layerName;

                    if (room.Statut == RoomStatus.A_VERIFIER)
                        poly.ColorIndex = 40; // Orange
                    else
                        poly.ColorIndex = 3; // Vert

                    btr.AppendEntity(poly);
                    tr.AddNewlyCreatedDBObject(poly, true);
                }

                MText debugText = new MText();
                debugText.Location = room.Centre;
                debugText.TextHeight = 15.0;

                string content = $"{{\\C2;TYPE:}} {room.Type}\\P" +
                                 $"{{\\C2;NOM:}} {room.Nom}\\P" +
                                 $"{{\\C2;CONF:}} O:{room.OverallConfidence:P0} G:{room.GeometryConfidence:P0} T:{room.RoomTypeConfidence:P0}\\P" +
                                 $"{{\\C2;GAPS:}} {room.NbOuvertures}\\P";

                foreach(var msg in room.MessagesAvertissement)
                {
                    content += $"{{\\C1;! {msg}}}\\P"; // Rouge pour les messages
                }

                debugText.Contents = content;
                debugText.Layer = layerName;
                debugText.Attachment = AttachmentPoint.MiddleCenter;

                btr.AppendEntity(debugText);
                tr.AddNewlyCreatedDBObject(debugText, true);
            }
        }

        public void DrawRejectedSegments(Transaction tr, Database db, List<SegmentInfo> segments)
        {
            string layerName = "IA_DEBUG_REJECTED";

            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = layerName;
                ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Rouge
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }

            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            foreach (var seg in segments)
            {
                if (seg.Category == SegmentCategory.UNKNOWN || seg.Category == SegmentCategory.BEAM || seg.IsDuplicate)
                {
                    Line line = new Line(new Point3d(seg.Geometry.StartPoint.X, seg.Geometry.StartPoint.Y, 0),
                                         new Point3d(seg.Geometry.EndPoint.X, seg.Geometry.EndPoint.Y, 0));
                    line.Layer = layerName;

                    if (seg.IsDuplicate)
                        line.ColorIndex = 5; // Bleu pour les doublons
                    else
                        line.ColorIndex = 1; // Rouge pour les rejets

                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                }
            }
        }
    }
}
