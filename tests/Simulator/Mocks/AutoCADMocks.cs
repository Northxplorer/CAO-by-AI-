// Ce fichier fournit des remplacements minimaux (Mocks) pour les structs et classes AutoCAD (Point3d, LineSegment2d)
// utilisées dans la logique métier, afin que la logique métier puisse être compilée et testée de manière autonome sous Linux/CI.

#if !AUTOCAD

using System;
using System.Collections.Generic;

namespace Autodesk.AutoCAD.Geometry
{
    public struct Point2d
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Point2d(double x, double y) { X = x; Y = y; }

        public double GetDistanceTo(Point2d pt)
        {
            return Math.Sqrt(Math.Pow(pt.X - X, 2) + Math.Pow(pt.Y - Y, 2));
        }
    }

    public struct Point3d
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3d(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public double DistanceTo(Point3d pt)
        {
            return Math.Sqrt(Math.Pow(pt.X - X, 2) + Math.Pow(pt.Y - Y, 2) + Math.Pow(pt.Z - Z, 2));
        }

        public double GetDistanceTo(Point3d pt) => DistanceTo(pt);

        public static Point3d Origin => new Point3d(0, 0, 0);

        public static bool operator ==(Point3d p1, Point3d p2) => p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        public static bool operator !=(Point3d p1, Point3d p2) => !(p1 == p2);
        public override bool Equals(object obj) => obj is Point3d pt && this == pt;
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    public class LineSegment2d
    {
        public Point2d StartPoint { get; set; }
        public Point2d EndPoint { get; set; }

        public LineSegment2d(Point2d start, Point2d end)
        {
            StartPoint = start;
            EndPoint = end;
        }

        public double Length => StartPoint.GetDistanceTo(EndPoint);

        // Implémentation basique de l'intersection de deux segments 2D
        public Point2d[] IntersectWith(LineSegment2d other)
        {
            double x1 = StartPoint.X, y1 = StartPoint.Y;
            double x2 = EndPoint.X, y2 = EndPoint.Y;
            double x3 = other.StartPoint.X, y3 = other.StartPoint.Y;
            double x4 = other.EndPoint.X, y4 = other.EndPoint.Y;

            double denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            if (Math.Abs(denom) < 1e-6) return null; // Lignes parallèles

            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom;

            // Si l'intersection se trouve sur les deux segments (avec une petite tolérance)
            if (ua >= -1e-6 && ua <= 1.000001 && ub >= -1e-6 && ub <= 1.000001)
            {
                return new Point2d[] { new Point2d(x1 + ua * (x2 - x1), y1 + ua * (y2 - y1)) };
            }

            return null;
        }
    }

    public struct Vector2d
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2d(double x, double y)
        {
            X = x; Y = y;
        }

        public double LengthSq => X * X + Y * Y;
        public double Length => Math.Sqrt(LengthSq);

        public Vector2d GetNormal()
        {
            double len = Length;
            if (len == 0) return new Vector2d(0, 0);
            return new Vector2d(X / len, Y / len);
        }

        public double GetAngleTo(Vector2d other)
        {
            double dot = X * other.X + Y * other.Y;
            double det = X * other.Y - Y * other.X;
            return Math.Atan2(det, dot);
        }
    }
}

namespace Autodesk.AutoCAD.DatabaseServices
{
    using Autodesk.AutoCAD.Geometry;

    public struct Extents3d
    {
        public Point3d MinPoint { get; set; }
        public Point3d MaxPoint { get; set; }

        public Extents3d(Point3d min, Point3d max)
        {
            MinPoint = min;
            MaxPoint = max;
        }

        public void AddPoint(Point3d pt)
        {
            MinPoint = new Point3d(Math.Min(MinPoint.X, pt.X), Math.Min(MinPoint.Y, pt.Y), Math.Min(MinPoint.Z, pt.Z));
            MaxPoint = new Point3d(Math.Max(MaxPoint.X, pt.X), Math.Max(MaxPoint.Y, pt.Y), Math.Max(MaxPoint.Z, pt.Z));
        }
    }
}
#endif
