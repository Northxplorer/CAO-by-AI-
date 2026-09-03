using System.Collections.Generic;

namespace AutoCadCopilot.Models
{
    public class RoomDetectionConfig
    {
        // Tolérance pour considérer que deux extrémités se touchent
        public double EndpointTolerance { get; set; } = 10.0;

        // Taille maximale d'un gap qui sera automatiquement fermé (ex: porte, petite ouverture)
        public double MaxAutoCloseGap { get; set; } = 120.0;

        // Taille typique d'une porte pour classifier les gaps (ex: entre 70 et 100)
        public double MinDoorGap { get; set; } = 70.0;
        public double MaxDoorGap { get; set; } = 100.0;

        // Limites de surface pour rejeter les faux positifs (ex: gaines techniques trop petites)
        public double MinimumRoomArea { get; set; } = 5000.0; // 0.5m²
        public double MaximumRoomArea { get; set; } = 2000000.0; // 200m²

        // Indices liés aux noms de calques
        public List<string> WallLayerHints { get; set; } = new List<string> { "MUR", "CLOISON", "WALL", "A-WALL" };
        public List<string> IgnoreLayerHints { get; set; } = new List<string> { "MOBILIER", "ELEC", "COTATION", "TEXT", "HACHURE" };
    }
}
