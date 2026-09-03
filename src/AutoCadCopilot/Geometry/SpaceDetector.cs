using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using AutoCadCopilot.Models;

namespace AutoCadCopilot.Geometry
{
    public class SpaceDetector
    {
        private RoomDetectionConfig _config;

        public SpaceDetector(RoomDetectionConfig config)
        {
            _config = config;
        }

        public List<Room> DetectSpaces(List<SegmentInfo> segmentsInfos)
        {
            var rooms = new List<Room>();

            // Ne garder que les murs valides, non dupliqués
            var walls = segmentsInfos.Where(s => s.Category == SegmentCategory.WALL && !s.IsDuplicate).ToList();
            if (walls.Count == 0) return rooms;

            // 1. Scission des segments aux points d'intersection pour supporter les murs en "T"
            var splitLines = SplitAtIntersections(walls);

            // 2. Recherche de boucles via un parcours du graphe
            // Note: C'est un algorithme basique de contournement "à gauche" (ou plus proche)
            // pour simuler la reconstruction topologique demandée.
            var usedEdgesForRooms = new HashSet<string>();

            for (int i = 0; i < splitLines.Count; i++)
            {
                var startLine = splitLines[i];
                var pStart = startLine.StartPoint;
                var pEnd = startLine.EndPoint;

                // On tente de former une boucle à partir de ce segment
                var currentPolygon = new List<Point3d> { new Point3d(pStart.X, pStart.Y, 0), new Point3d(pEnd.X, pEnd.Y, 0) };
                var localUsedLines = new HashSet<LineSegment2d> { startLine };

                bool loopClosed = false;
                int openingsDetected = 0;
                double geoConfidence = 1.0;

                while (!loopClosed && currentPolygon.Count < 50) // Sécurité contre les boucles infinies
                {
                    Point3d lastPoint = currentPolygon.Last();

                    LineSegment2d nextLine = null;
                    Point3d nextPointToAdd = Point3d.Origin;
                    double minDistance = double.MaxValue;
                    bool isGap = false;

                    foreach (var candidate in splitLines)
                    {
                        if (localUsedLines.Contains(candidate)) continue;

                        Point3d p1 = new Point3d(candidate.StartPoint.X, candidate.StartPoint.Y, 0);
                        Point3d p2 = new Point3d(candidate.EndPoint.X, candidate.EndPoint.Y, 0);

                        double distToP1 = lastPoint.DistanceTo(p1);
                        double distToP2 = lastPoint.DistanceTo(p2);

                        // Si le point est parfaitement connecté (ou dans la tolérance de snapping)
                        if (distToP1 <= _config.EndpointTolerance && distToP1 < minDistance)
                        {
                            minDistance = distToP1;
                            nextLine = candidate;
                            nextPointToAdd = p2;
                            isGap = false;
                        }
                        else if (distToP2 <= _config.EndpointTolerance && distToP2 < minDistance)
                        {
                            minDistance = distToP2;
                            nextLine = candidate;
                            nextPointToAdd = p1;
                            isGap = false;
                        }
                        // Autrement, si c'est un Gap acceptable (ex: porte)
                        else if (distToP1 > _config.EndpointTolerance && distToP1 <= _config.MaxAutoCloseGap && distToP1 < minDistance)
                        {
                            minDistance = distToP1;
                            nextLine = candidate;
                            nextPointToAdd = p2;
                            isGap = true;
                        }
                        else if (distToP2 > _config.EndpointTolerance && distToP2 <= _config.MaxAutoCloseGap && distToP2 < minDistance)
                        {
                            minDistance = distToP2;
                            nextLine = candidate;
                            nextPointToAdd = p1;
                            isGap = true;
                        }
                    }

                    if (nextLine != null)
                    {
                        if (isGap)
                        {
                            openingsDetected++;
                            geoConfidence -= 0.1; // On perd en confiance à chaque Gap "magiquement" fermé
                            // On ajoute le point de début du gap pour faire un vrai polygone fermé
                            if (nextPointToAdd == new Point3d(nextLine.EndPoint.X, nextLine.EndPoint.Y, 0))
                                currentPolygon.Add(new Point3d(nextLine.StartPoint.X, nextLine.StartPoint.Y, 0));
                            else
                                currentPolygon.Add(new Point3d(nextLine.EndPoint.X, nextLine.EndPoint.Y, 0));
                        }

                        currentPolygon.Add(nextPointToAdd);
                        localUsedLines.Add(nextLine);

                        // Vérifie si on a fermé la boucle (retour au premier point)
                        if (nextPointToAdd.DistanceTo(currentPolygon.First()) <= _config.MaxAutoCloseGap)
                        {
                            loopClosed = true;
                            if (nextPointToAdd.DistanceTo(currentPolygon.First()) > _config.EndpointTolerance)
                            {
                                openingsDetected++;
                                geoConfidence -= 0.1;
                            }
                        }
                    }
                    else
                    {
                        break; // Cul-de-sac
                    }
                }

                if (loopClosed && currentPolygon.Count > 2)
                {
                    var room = new Room();
                    room.Contour = currentPolygon;
                    room.Surface = MathUtils.CalculateArea(room.Contour);

                    if (room.Surface >= _config.MinimumRoomArea && room.Surface <= _config.MaximumRoomArea)
                    {
                        room.Centre = MathUtils.CalculateCentroid(room.Contour);

                        // Anti-doublon par centre de gravité
                        if (!rooms.Any(r => r.Centre.DistanceTo(room.Centre) < 50))
                        {
                            room.BoundingBox = MathUtils.CalculateBoundingBox(room.Contour);
                            room.NbSegments = localUsedLines.Count;
                            room.NbOuvertures = openingsDetected;
                            room.GeometryConfidence = Math.Max(0.1, geoConfidence);
                            rooms.Add(room);
                        }
                    }
                }
            }

            return rooms;
        }

        // Divise les lignes qui se croisent (pour les jonctions en T)
        private List<LineSegment2d> SplitAtIntersections(List<SegmentInfo> walls)
        {
            var splitLines = new List<LineSegment2d>();

            foreach (var w in walls)
            {
                var points = new List<Point2d> { w.Geometry.StartPoint, w.Geometry.EndPoint };

                foreach (var other in walls)
                {
                    if (w == other) continue;

                    var intersection = w.Geometry.IntersectWith(other.Geometry);
                    if (intersection != null && intersection.Length > 0)
                    {
                        points.Add(intersection[0]);
                    }
                }

                // Trier les points le long de la ligne
                var sortedPoints = points.OrderBy(p => p.GetDistanceTo(w.Geometry.StartPoint)).ToList();

                // Créer de nouveaux petits segments
                for (int k = 0; k < sortedPoints.Count - 1; k++)
                {
                    if (sortedPoints[k].GetDistanceTo(sortedPoints[k+1]) > _config.EndpointTolerance)
                    {
                        splitLines.Add(new LineSegment2d(sortedPoints[k], sortedPoints[k+1]));
                    }
                }
            }

            return splitLines;
        }
    }
}
