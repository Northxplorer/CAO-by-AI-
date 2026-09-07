using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AutoCadCopilot.Models;
using AutoCadCopilot.Geometry;

namespace AutoCadCopilot.AutoCAD
{
    public class EntityExtractor
    {
        public List<SegmentInfo> ExtractSegments(Transaction tr, BlockTableRecord btr)
        {
            List<SegmentInfo> segments = new List<SegmentInfo>();

            foreach (ObjectId objId in btr)
            {
                Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);

                if (ent is Line line)
                {
                    segments.Add(new SegmentInfo(
                        new LineSegment2d(new Point2d(line.StartPoint.X, line.StartPoint.Y), new Point2d(line.EndPoint.X, line.EndPoint.Y)),
                        ent.Layer,
                        "Line"
                    ));
                }
                else if (ent is Polyline polyline)
                {
                    int maxSegments = polyline.Closed ? polyline.NumberOfVertices : polyline.NumberOfVertices - 1;
                    for (int i = 0; i < maxSegments; i++)
                    {
                        if (polyline.GetSegmentType(i) == SegmentType.Line)
                        {
                            segments.Add(new SegmentInfo(polyline.GetLineSegment2dAt(i), ent.Layer, "Polyline"));
                        }
                    }
                }
            }

            return segments;
        }

        public List<TextEntityInfo> ExtractTexts(Transaction tr, BlockTableRecord btr)
        {
            List<TextEntityInfo> texts = new List<TextEntityInfo>();

            foreach (ObjectId objId in btr)
            {
                Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);

                if (ent is DBText dbText)
                {
                    texts.Add(new TextEntityInfo
                    {
                        Text = dbText.TextString,
                        Position = dbText.Position,
                        Height = dbText.Height
                    });
                }
                else if (ent is MText mText)
                {
                    texts.Add(new TextEntityInfo
                    {
                        Text = mText.Text, // Contenu texte brut (sans formatage)
                        Position = mText.Location,
                        Height = mText.TextHeight
                    });
                }
            }

            return texts;
        }
    }
}
