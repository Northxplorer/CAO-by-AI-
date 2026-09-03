using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using AutoCadCopilot.Models;

namespace AutoCadCopilot.Geometry
{
    public class SegmentAnalyzer
    {
        private RoomDetectionConfig _config;

        public SegmentAnalyzer(RoomDetectionConfig config)
        {
            _config = config;
        }

        public void CategorizeSegments(List<SegmentInfo> segments)
        {
            // 1. Marquer les doublons (géométries quasi-identiques)
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].IsDuplicate) continue;

                for (int j = i + 1; j < segments.Count; j++)
                {
                    if (segments[j].IsDuplicate) continue;

                    // Si les segments ont (à peu près) les mêmes start/end points
                    if ((segments[i].Geometry.StartPoint.GetDistanceTo(segments[j].Geometry.StartPoint) < _config.EndpointTolerance &&
                         segments[i].Geometry.EndPoint.GetDistanceTo(segments[j].Geometry.EndPoint) < _config.EndpointTolerance) ||
                        (segments[i].Geometry.StartPoint.GetDistanceTo(segments[j].Geometry.EndPoint) < _config.EndpointTolerance &&
                         segments[i].Geometry.EndPoint.GetDistanceTo(segments[j].Geometry.StartPoint) < _config.EndpointTolerance))
                    {
                        segments[j].IsDuplicate = true;
                    }
                }
            }

            // 2. Score et Catégorisation
            foreach (var seg in segments)
            {
                if (seg.IsDuplicate) continue;

                string upperLayer = seg.Layer.ToUpperInvariant();

                // Ignorer formellement via le calque
                if (_config.IgnoreLayerHints.Any(h => upperLayer.Contains(h)))
                {
                    seg.Category = SegmentCategory.UNKNOWN;
                    seg.Score = -1.0;
                    continue;
                }

                // Indices pour les murs
                if (_config.WallLayerHints.Any(h => upperLayer.Contains(h)))
                {
                    seg.Score += 2.0;
                }

                // Score de base basé sur la longueur (des petits segments de moins de 10 unités sont souvent du bruit)
                double length = seg.Geometry.Length;
                if (length < 10.0)
                    seg.Score -= 1.0;
                else if (length > 100.0)
                    seg.Score += 1.0;

                // Décision MVP
                if (seg.Score >= 1.0)
                {
                    seg.Category = SegmentCategory.WALL;
                }
                else
                {
                    seg.Category = SegmentCategory.UNKNOWN;
                }
            }
        }
    }
}
