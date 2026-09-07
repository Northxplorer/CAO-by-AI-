using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutoCadCopilot.Models
{
    public class SpaceDetectionDiagnosticReport
    {
        public int SegmentsEntrants { get; set; } = 0;
        public int IntersectionsTrouvees { get; set; } = 0;
        public int SegmentsScindes { get; set; } = 0;

        // Métriques du graphe
        public int NoeudsUniques { get; set; } = 0;
        public int AretesFinales { get; set; } = 0;
        public int NoeudsDegre1 { get; set; } = 0;
        public int NoeudsDegre2 { get; set; } = 0;
        public int NoeudsDegre3Plus { get; set; } = 0;
        public int ComposantesConnexes { get; set; } = 0;
        public int SegmentsIsolees { get; set; } = 0; // Segments sans aucune connexion aux deux bouts

        public int BouclesCandidates { get; set; } = 0;
        public int CulDeSacRencontres { get; set; } = 0;
        public int BouclesRejeteesSurface { get; set; } = 0;
        public int BouclesRejeteesPerimetre { get; set; } = 0;
        public int BouclesRejeteesDoublon { get; set; } = 0;
        public int ContoursFinaux { get; set; } = 0;

        public List<List<Point3d>> ImpassesGeometriques { get; set; } = new List<List<Point3d>>();
        public List<Point3d> NoeudsCritiques { get; set; } = new List<Point3d>(); // Nœuds de degré 1 pour le debug visuel
        public List<LineSegment2d> SegmentsGraphe { get; set; } = new List<LineSegment2d>(); // Pour affichage
    }

    public class SegmentDiagnosticReport
    {
        public int TotalExtracted { get; set; } = 0;
        public int TotalRetainedAsWall { get; set; } = 0;
        public int TotalRejected { get; set; } = 0;

        public int DoublonsStricts { get; set; } = 0;
        public int LignesParallelesProches { get; set; } = 0;
        public int ChevauchementsPartiels { get; set; } = 0;
        public double ToleranceDoublonStricte { get; set; } = 0.0;

        public Dictionary<string, int> SegmentsByLayer { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SegmentsByEntityType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RejectionReasons { get; set; } = new Dictionary<string, int>();

        public void RegisterSegment(string layer, string entityType)
        {
            TotalExtracted++;

            if (!SegmentsByLayer.ContainsKey(layer)) SegmentsByLayer[layer] = 0;
            SegmentsByLayer[layer]++;

            if (!SegmentsByEntityType.ContainsKey(entityType)) SegmentsByEntityType[entityType] = 0;
            SegmentsByEntityType[entityType]++;
        }

        public void RegisterRejection(string reason)
        {
            TotalRejected++;
            if (!RejectionReasons.ContainsKey(reason)) RejectionReasons[reason] = 0;
            RejectionReasons[reason]++;
        }
    }

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
