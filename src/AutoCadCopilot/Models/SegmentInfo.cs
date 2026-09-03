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
        public SegmentCategory Category { get; set; } = SegmentCategory.UNKNOWN;
        public double Score { get; set; } = 0.0;
        public bool IsDuplicate { get; set; } = false;

        public SegmentInfo(LineSegment2d geom, string layer)
        {
            Geometry = geom;
            Layer = layer;
        }
    }
}
