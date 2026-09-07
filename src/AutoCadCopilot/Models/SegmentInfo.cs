using Autodesk.AutoCAD.Geometry;

namespace AutoCadCopilot.Geometry
{
    public enum SegmentCategory
    {
        WALL,
        OPENING,
        DOOR,
        COLUMN,
        BEAM,
        UNKNOWN
    }

    public class SegmentInfo
    {
        public LineSegment2d Geometry { get; set; }
        public string Layer { get; set; }
        public string EntityType { get; set; } = "Unknown";
        public SegmentCategory Category { get; set; } = SegmentCategory.UNKNOWN;
        public double Score { get; set; } = 0.0;
        public bool IsDuplicate { get; set; } = false;
        public string RejectionReason { get; set; } = string.Empty;

        public SegmentInfo(LineSegment2d geom, string layer, string entityType = "Unknown")
        {
            Geometry = geom;
            Layer = layer;
            EntityType = entityType;
        }
    }
}
