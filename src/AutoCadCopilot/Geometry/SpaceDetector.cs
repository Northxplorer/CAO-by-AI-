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

            // 3. Filtrage : On retire d'abord les géométries suspectes/incohérentes (ex: boucles infinies)
            var cleanRooms = new List<Room>();
            foreach (var r in rooms)
            {
                if (r.Perimetre > 0 && r.Surface / r.Perimetre > 0.1 && r.NbSegments > 2)
                {
                    // L'algorithme tourne-à-gauche exhaustif génère les MÊMES pièces géométriquement avec des subdivisions différentes (et parfois des gaps).
                    // On privilégie la version de la pièce qui a la meilleure GeometryConfidence (le moins de gaps).
                    // ATTENTION: La tolérance de centre (10.0) ne suffit pas toujours si la pièce est trouvée avec des "bavures" qui décalent le centre.
                    // On vérifie donc aussi si elles ont pratiquement la même Bounding Box ET la même surface.
                    var existingIdentical = cleanRooms.FirstOrDefault(existing =>
                         (existing.Centre.DistanceTo(r.Centre) < 10.0 ||
                          (existing.BoundingBox.MinPoint.DistanceTo(r.BoundingBox.MinPoint) < 10.0 && existing.BoundingBox.MaxPoint.DistanceTo(r.BoundingBox.MaxPoint) < 10.0))
                         && Math.Abs(existing.Surface - r.Surface) < existing.Surface * 0.05);

                    if (existingIdentical == null)
                    {
                        cleanRooms.Add(r);
                    }
                    else if (r.GeometryConfidence > existingIdentical.GeometryConfidence)
                    {
                        cleanRooms.Remove(existingIdentical);
                        cleanRooms.Add(r);
                    }
                }
            }

            // 4. Filtre final : Retirer les "faux espaces extérieurs" (contours englobants)
            return FilterEnclosingRooms(cleanRooms);
        }

        private List<Room> FilterEnclosingRooms(List<Room> rooms)
        {
            var validRooms = new List<Room>();

            // On trie par surface (de la plus grande à la plus petite)
            var sortedRooms = rooms.OrderByDescending(r => r.Surface).ToList();

            for (int i = 0; i < sortedRooms.Count; i++)
            {
                bool isEnclosingRoom = false;
                for (int j = 0; j < sortedRooms.Count; j++)
                {
                    if (i == j) continue;

                    // Englobement 1 : Vraie pièce englobante (plus grande)
                    // Si J est DANS I (on vérifie avec tous les points du polygone J pour être sûr, pas juste le centre).
                    if (sortedRooms[i].Surface >= sortedRooms[j].Surface * 1.1)
                    {
                         bool isCompletelyInside = true;
                         foreach(var pt in sortedRooms[j].Contour)
                         {
                              // Petite marge de débordement pour les murs mitoyens
                              if (!MathUtils.IsPointInPolygon(pt, sortedRooms[i].Contour) &&
                                  !IsPointCloseToPolygon(pt, sortedRooms[i].Contour, 10.0))
                              {
                                   isCompletelyInside = false;
                                   break;
                              }
                         }

                         if (isCompletelyInside)
                         {
                              // La grande pièce I n'est pas une "vraie" pièce de vie, c'est l'union de plusieurs pièces.
                              isEnclosingRoom = true;
                              break;
                         }
                    }

                    // Englobement 2 : Pièces quasiment identiques (surface similaire)
                    else if (Math.Abs(sortedRooms[i].Surface - sortedRooms[j].Surface) <= sortedRooms[i].Surface * 0.05)
                    {
                         // Supprimer le contour qui a le plus grand périmètre (donc qui a fait des allers-retours inutiles)
                         if (sortedRooms[i].Perimetre > sortedRooms[j].Perimetre * 1.05)
                         {
                              isEnclosingRoom = true;
                              break;
                         }
                         // Doublon exact non attrapé précédemment (même taille, même périmètre, même endroit)
                         else if (Math.Abs(sortedRooms[i].Perimetre - sortedRooms[j].Perimetre) <= sortedRooms[j].Perimetre * 0.05
                                  && sortedRooms[i].Centre.DistanceTo(sortedRooms[j].Centre) < 50.0
                                  && i > j)
                         {
                              isEnclosingRoom = true;
                              break;
                         }
                    }
                }

                if (!isEnclosingRoom)
                {
                    validRooms.Add(sortedRooms[i]);
                }
            }

            return validRooms;
        }

        private bool IsPointCloseToPolygon(Point3d point, List<Point3d> polygon, double tolerance)
        {
            for (int i = 0; i < polygon.Count - 1; i++)
            {
                if (DistancePointLine(point, polygon[i], polygon[i+1]) < tolerance) return true;
            }
            if (DistancePointLine(point, polygon.Last(), polygon.First()) < tolerance) return true;
            return false;
        }

        private double DistancePointLine(Point3d pt, Point3d lineStart, Point3d lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            if (dx == 0 && dy == 0) return pt.DistanceTo(lineStart);

            double t = ((pt.X - lineStart.X) * dx + (pt.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
            if (t < 0) return pt.DistanceTo(lineStart);
            if (t > 1) return pt.DistanceTo(lineEnd);

            return new Point3d(lineStart.X + t * dx, lineStart.Y + t * dy, 0).DistanceTo(pt);
        }


        private bool IsRoomValidAndUnique(List<Room> existingRooms, Room candidate)
        {
            if (candidate.Surface < _config.MinimumRoomArea || candidate.Surface > _config.MaximumRoomArea)
                return false;

            if (candidate.Surface / candidate.Perimetre < 10)
                return false;

            // Comparaison topologique stricte: deux pièces sont identiques si tous leurs sommets sont très proches
            foreach (var r in existingRooms)
            {
                if (r.Contour.Count == candidate.Contour.Count)
                {
                    bool allPointsMatch = true;
                    // On trie les points pour s'affranchir du point de départ
                    var orderedExisting = r.Contour.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                    var orderedCandidate = candidate.Contour.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

                    for (int i = 0; i < orderedExisting.Count; i++)
                    {
                        if (orderedExisting[i].DistanceTo(orderedCandidate[i]) > 1.0)
                        {
                            allPointsMatch = false;
                            break;
                        }
                    }

                    if (allPointsMatch)
                    {
                        return false;
                    }
                }
                // Dans le cas de pièces simples sans intersections, le graphe trouve souvent 4 fois la même pièce
                // On fusionne si surface et centre sont IDENTIQUES (Tolérance 5.0 sur le centre, 5.0 sur l'aire)
                // MAIS si la nouvelle pièce est "meilleure" (moins de gaps), on pourrait vouloir la garder et jeter l'ancienne.
                // On gère ça dans la phase de cleanRooms. Ici on se contente de dire si c'est un doublon grossier.
                if (r.Centre.DistanceTo(candidate.Centre) < 5.0 && Math.Abs(r.Surface - candidate.Surface) < 5.0)
                {
                    // L'ancienne pièce a peut-être un score plus mauvais.
                    // Pour ce MVP, l'unicité se fait dans l'étape `cleanRooms` avec la comparaison des GeometryConfidence.
                    // Donc ici on renvoie 'true' si c'est le même centre pour que cleanRooms puisse comparer les scores.
                    // MAIS si les sommets sont TOUS identiques (allPointsMatch ci-dessus), c'est un vrai clone inutile, on rejette.
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
                if (currentDir.Length == 0.0) break;
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
                        if (candidateDir.Length == 0.0) continue;
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
