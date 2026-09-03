using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace AutoCadCopilot.Geometry
{
    public static class MathUtils
    {
        /// <summary>
        /// Détermine si un point (X,Y) est à l'intérieur d'un polygone.
        /// Utilise l'algorithme Ray-Casting.
        /// </summary>
        public static bool IsPointInPolygon(Point3d point, List<Point3d> polygon)
        {
            bool isInside = false;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (polygon[i].Y < point.Y && polygon[j].Y >= point.Y || polygon[j].Y < point.Y && polygon[i].Y >= point.Y)
                {
                    if (polygon[i].X + (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < point.X)
                    {
                        isInside = !isInside;
                    }
                }
                j = i;
            }
            return isInside;
        }

        /// <summary>
        /// Calcule l'aire d'un polygone (formule de l'aire de Gauss / Shoelace).
        /// </summary>
        public static double CalculateArea(List<Point3d> polygon)
        {
            double area = 0;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                area += (polygon[j].X + polygon[i].X) * (polygon[j].Y - polygon[i].Y);
                j = i;
            }
            return Math.Abs(area / 2.0);
        }

        /// <summary>
        /// Calcule le périmètre d'un polygone fermé.
        /// </summary>
        public static double CalculatePerimeter(List<Point3d> polygon)
        {
            if (polygon.Count < 2) return 0;

            double perimeter = 0;
            for (int i = 0; i < polygon.Count - 1; i++)
            {
                perimeter += polygon[i].DistanceTo(polygon[i + 1]);
            }
            // Si le polygone n'est pas explicitement fermé en ajoutant le premier point à la fin, on le ferme.
            if (polygon.First().DistanceTo(polygon.Last()) > 0.001)
            {
                 perimeter += polygon.Last().DistanceTo(polygon.First());
            }

            return perimeter;
        }

        /// <summary>
        /// Calcule le centre géométrique d'un polygone (moyenne des sommets pour un calcul rapide et suffisant).
        /// </summary>
        public static Point3d CalculateCentroid(List<Point3d> polygon)
        {
            if (polygon.Count == 0) return Point3d.Origin;

            double sumX = 0, sumY = 0;
            foreach (var p in polygon)
            {
                sumX += p.X;
                sumY += p.Y;
            }
            return new Point3d(sumX / polygon.Count, sumY / polygon.Count, 0);
        }

        /// <summary>
        /// Calcule la BoundingBox 3D d'une liste de points.
        /// </summary>
        public static Extents3d CalculateBoundingBox(List<Point3d> polygon)
        {
            if (polygon.Count == 0) return new Extents3d(Point3d.Origin, Point3d.Origin);

            Extents3d ext = new Extents3d(polygon[0], polygon[0]);
            foreach(var p in polygon)
            {
                ext.AddPoint(p);
            }
            return ext;
        }
    }
}
