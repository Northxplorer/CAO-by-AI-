using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace AutoCadCopilot.Models
{
    public enum RoomType
    {
        Bureau,
        Chambre,
        Sejour,
        Cuisine,
        SalleDeBain,
        WC,
        Circulation,
        Hall,
        LocalTechnique,
        Rangement,
        Autre,
        Inconnu
    }

    public enum RoomStatus
    {
        OK,
        A_VERIFIER
    }

    public enum TextCategory
    {
        ROOM_NAME,
        FURNITURE,
        ANNOTATION,
        DIMENSION,
        TITLE_BLOCK,
        UNKNOWN
    }

    public class TextEntityInfo
    {
        public string Text { get; set; }
        public Point3d Position { get; set; }
        public double Height { get; set; }
        public TextCategory Category { get; set; } = TextCategory.UNKNOWN;
        public double CategoryConfidence { get; set; } = 0.0;
    }

    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nom { get; set; } = string.Empty;
        public RoomType Type { get; set; } = RoomType.Inconnu;

        // Géométrie
        public double Surface { get; set; }
        public double Perimetre { get; set; }
        public Point3d Centre { get; set; }
        public List<Point3d> Contour { get; set; } = new List<Point3d>();
        public Extents3d BoundingBox { get; set; }
        public int NbOuvertures { get; set; }
        public int NbSegments { get; set; }

        public List<TextEntityInfo> TextesAssocies { get; set; } = new List<TextEntityInfo>();

        // Confiance (0.0 à 1.0)
        public double GeometryConfidence { get; set; } = 0.0;
        public double RoomTypeConfidence { get; set; } = 0.0;
        public double OverallConfidence { get; set; } = 0.0;

        public RoomStatus Statut { get; set; } = RoomStatus.A_VERIFIER;
        public List<string> MessagesAvertissement { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"[{Type}] {Nom} (Surf: {Surface:F1}, TypeConf: {RoomTypeConfidence:P0}, GeoConf: {GeometryConfidence:P0}, Statut: {Statut})";
        }
    }
}
