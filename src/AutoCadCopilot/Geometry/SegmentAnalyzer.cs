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

        public SegmentDiagnosticReport CategorizeSegments(List<SegmentInfo> segments)
        {
            var report = new SegmentDiagnosticReport();

            // 1. Enregistrement initial
            foreach (var seg in segments)
            {
                report.RegisterSegment(seg.Layer, seg.EntityType);
            }

            // 2. Marquer les doublons (géométries quasi-identiques)
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
                        segments[j].RejectionReason = "Doublon géométrique";
                        report.RegisterRejection(segments[j].RejectionReason);
                    }
                }
            }

            // 3. Score et Catégorisation
            foreach (var seg in segments)
            {
                if (seg.IsDuplicate) continue;

                string upperLayer = seg.Layer.ToUpperInvariant();

                // Ignorer formellement via le calque
                if (_config.IgnoreLayerHints.Any(h => upperLayer.Contains(h)))
                {
                    seg.Category = SegmentCategory.UNKNOWN;
                    seg.Score = -1.0;
                    seg.RejectionReason = $"Calque ignoré : {seg.Layer}";
                    report.RegisterRejection(seg.RejectionReason);
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

                // Si le segment est issu d'une polyligne fermée, il y a de fortes chances que ce soit un pilier ou un mur,
                // même si le calque n'est pas "MUR".
                if (seg.EntityType == "Polyline")
                {
                    seg.Score += 0.5; // Bonus léger pour la structure polyligne
                }

                // Pour accepter des lignes sur un calque inconnu (0.0 de base), il faut qu'elles soient significativement longues.
                // Cela évite de rejeter un plan entier où l'architecte a dessiné les murs sur un calque nommé "0" ou "Dessin".
                if (seg.Score >= 0.0 && length > 50.0 && seg.EntityType == "Line")
                {
                    seg.Score += 1.0;
                }

                // Décision MVP
                if (seg.Score >= 1.0)
                {
                    seg.Category = SegmentCategory.WALL;
                    report.TotalRetainedAsWall++;
                }
                else
                {
                    seg.Category = SegmentCategory.UNKNOWN;
                    seg.RejectionReason = $"Score insuffisant ({seg.Score:F1}) - L:{length:F1}";
                    report.RegisterRejection("Score insuffisant (probablement pas un mur architectural)");
                }
            }

            return report;
        }
    }
}
