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

            // 2. Recherche de boucles
            // On utilise une approche exhaustive simple pour le simulateur/MVP :
            // pour chaque segment, on tente de trouver le plus petit cycle le contenant, en tournant "à gauche" (ou à droite).

            for (int i = 0; i < splitLines.Count; i++)
            {
                // Essayer dans les deux sens
                var room1 = TryFindLoop(splitLines, splitLines[i], true);
                if (room1 != null && IsRoomValidAndUnique(rooms, room1)) rooms.Add(room1);

                var room2 = TryFindLoop(splitLines, splitLines[i], false);
                if (room2 != null && IsRoomValidAndUnique(rooms, room2)) rooms.Add(room2);
            }

            return rooms;
        }

        private bool IsRoomValidAndUnique(List<Room> existingRooms, Room candidate)
        {
            if (candidate.Surface < _config.MinimumRoomArea || candidate.Surface > _config.MaximumRoomArea)
                return false;

            // Anti-doublon plus agressif (centre de gravité ET surface)
            // L'algo génère parfois des boucles qui "incluent" d'autres pièces mais partagent le même centre.
            foreach (var r in existingRooms)
            {
                // On considère qu'il s'agit d'un doublon si les centres sont proches (à 1m près)
                // ET que la surface est quasi identique (à 1m² près).
                if (r.Centre.DistanceTo(candidate.Centre) < 100 && Math.Abs(r.Surface - candidate.Surface) < 10000)
                {
                    return false;
                }
            }
            return true;
        }

        private Room TryFindLoop(List<LineSegment2d> allLines, LineSegment2d startLine, bool forward)
        {
            var currentPolygon = new List<Point3d>();
            var localUsedLines = new HashSet<LineSegment2d> { startLine };

            Point3d startP = forward ? new Point3d(startLine.StartPoint.X, startLine.StartPoint.Y, 0) : new Point3d(startLine.EndPoint.X, startLine.EndPoint.Y, 0);
            Point3d endP = forward ? new Point3d(startLine.EndPoint.X, startLine.EndPoint.Y, 0) : new Point3d(startLine.StartPoint.X, startLine.StartPoint.Y, 0);

            currentPolygon.Add(startP);
            currentPolygon.Add(endP);

            bool loopClosed = false;
            int openingsDetected = 0;
            double geoConfidence = 1.0;

            while (!loopClosed && currentPolygon.Count < 30) // Sécurité
            {
                Point3d lastPoint = currentPolygon.Last();
                Point3d previousPoint = currentPolygon[currentPolygon.Count - 2];

                // Vecteur de direction actuel
                Vector2d currentDir = new Vector2d(lastPoint.X - previousPoint.X, lastPoint.Y - previousPoint.Y);
                if (currentDir.LengthSq == 0) break;
                currentDir = currentDir.GetNormal();

                LineSegment2d nextLine = null;
                Point3d nextPointToAdd = Point3d.Origin;
                bool isGap = false;

                double bestAngle = double.MaxValue; // On cherche à tourner le plus possible dans un sens (ex: contournement à gauche)

                foreach (var candidate in allLines)
                {
                    if (localUsedLines.Contains(candidate)) continue;

                    Point3d p1 = new Point3d(candidate.StartPoint.X, candidate.StartPoint.Y, 0);
                    Point3d p2 = new Point3d(candidate.EndPoint.X, candidate.EndPoint.Y, 0);

                    double distToP1 = lastPoint.DistanceTo(p1);
                    double distToP2 = lastPoint.DistanceTo(p2);

                    Point3d candidateStartPoint;
                    Point3d candidateEndPoint;
                    bool validCandidate = false;
                    bool candidateIsGap = false;

                    if (distToP1 <= _config.EndpointTolerance) { candidateStartPoint = p1; candidateEndPoint = p2; validCandidate = true; }
                    else if (distToP2 <= _config.EndpointTolerance) { candidateStartPoint = p2; candidateEndPoint = p1; validCandidate = true; }
                    else if (distToP1 > _config.EndpointTolerance && distToP1 <= _config.MaxAutoCloseGap) { candidateStartPoint = p1; candidateEndPoint = p2; validCandidate = true; candidateIsGap = true; }
                    else if (distToP2 > _config.EndpointTolerance && distToP2 <= _config.MaxAutoCloseGap) { candidateStartPoint = p2; candidateEndPoint = p1; validCandidate = true; candidateIsGap = true; }
                    else continue;

                    if (validCandidate)
                    {
                        // Calculer l'angle orienté (vecteur u -> v)
                        Vector2d candidateDir = new Vector2d(candidateEndPoint.X - candidateStartPoint.X, candidateEndPoint.Y - candidateStartPoint.Y);
                        if (candidateDir.LengthSq == 0) continue;
                        candidateDir = candidateDir.GetNormal();

                        // Produit scalaire (Dot) et déterminant (Cross 2D) pour trouver l'angle relatif
                        double dot = currentDir.X * candidateDir.X + currentDir.Y * candidateDir.Y;
                        double det = currentDir.X * candidateDir.Y - currentDir.Y * candidateDir.X;
                        double angle = Math.Atan2(det, dot); // [-PI, PI]

                        // Ramener l'angle à [0, 2PI] (Angle depuis la gauche vers la droite)
                        // Si forward (vrai) => on cherche à tourner le plus à gauche possible => on minimise l'angle [0, 2PI]
                        double scoreAngle = angle;
                        if (scoreAngle < 0) scoreAngle += 2 * Math.PI;

                        if (scoreAngle < bestAngle)
                        {
                            bestAngle = scoreAngle;
                            nextLine = candidate;
                            nextPointToAdd = candidateEndPoint;
                            isGap = candidateIsGap;
                        }
                    }
                }

                if (nextLine != null)
                {
                    if (isGap)
                    {
                        openingsDetected++;
                        geoConfidence -= 0.1;
                        // On ajoute le début du gap
                        if (nextPointToAdd == new Point3d(nextLine.EndPoint.X, nextLine.EndPoint.Y, 0))
                            currentPolygon.Add(new Point3d(nextLine.StartPoint.X, nextLine.StartPoint.Y, 0));
                        else
                            currentPolygon.Add(new Point3d(nextLine.EndPoint.X, nextLine.EndPoint.Y, 0));
                    }

                    currentPolygon.Add(nextPointToAdd);
                    localUsedLines.Add(nextLine);

                    // Vérifier si on a fermé la boucle sur le TOUT PREMIER point
                    if (nextPointToAdd.DistanceTo(currentPolygon.First()) <= _config.MaxAutoCloseGap)
                    {
                        // On ferme seulement si on a au moins 3 vrais murs distincts
                        if (localUsedLines.Count >= 3)
                        {
                            loopClosed = true;
                            if (nextPointToAdd.DistanceTo(currentPolygon.First()) > _config.EndpointTolerance)
                            {
                                openingsDetected++;
                                geoConfidence -= 0.1;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if (loopClosed && currentPolygon.Count > 2)
            {
                double surface = MathUtils.CalculateArea(currentPolygon);
                if (surface >= _config.MinimumRoomArea && surface <= _config.MaximumRoomArea)
                {
                    var room = new Room();
                    room.Contour = currentPolygon;
                    room.Surface = surface;
                    room.Centre = MathUtils.CalculateCentroid(room.Contour);
                    room.Perimetre = MathUtils.CalculatePerimeter(room.Contour);
                    room.BoundingBox = MathUtils.CalculateBoundingBox(room.Contour);
                    room.NbSegments = localUsedLines.Count;
                    room.NbOuvertures = openingsDetected;
                    room.GeometryConfidence = Math.Max(0.1, geoConfidence);
                    return room;
                }
            }
            return null;
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

                // Retirer les points en double pour éviter les segments de longueur 0
                var uniquePoints = new List<Point2d>();
                foreach (var pt in sortedPoints)
                {
                    if (uniquePoints.Count == 0 || pt.GetDistanceTo(uniquePoints.Last()) > 1e-4)
                    {
                        uniquePoints.Add(pt);
                    }
                }

                // Créer de nouveaux petits segments
                for (int k = 0; k < uniquePoints.Count - 1; k++)
                {
                    if (uniquePoints[k].GetDistanceTo(uniquePoints[k+1]) > 0.1) // Segment min
                    {
                        splitLines.Add(new LineSegment2d(uniquePoints[k], uniquePoints[k+1]));
                    }
                }
            }

            return splitLines;
        }
    }
}
