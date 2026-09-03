using System;
using System.Linq;
using AutoCadCopilot.Models;

namespace AutoCadCopilot.Analysis
{
    public class RoomClassifier
    {
        public void ClassifyRooms(System.Collections.Generic.List<Room> rooms)
        {
            foreach (var room in rooms)
            {
                // 1. Gérer les cas sans texte
                var roomNames = room.TextesAssocies.Where(t => t.Category == TextCategory.ROOM_NAME).ToList();
                if (roomNames.Count == 0)
                {
                    room.Type = RoomType.Inconnu;
                    room.Nom = "Sans Nom";
                    room.RoomTypeConfidence = 0.0;
                    room.MessagesAvertissement.Add("Aucun texte de nom de pièce trouvé.");
                    CalculateOverall(room);
                    continue;
                }

                if (roomNames.Count > 1)
                {
                    room.MessagesAvertissement.Add("Plusieurs textes potentiels pour le nom. Choix du plus grand.");
                }

                var mainTextInfo = roomNames.OrderByDescending(t => t.Height).First();
                room.Nom = mainTextInfo.Text;

                // 2. Logique heuristique de classification de base (Avant IA)
                string lowerNom = room.Nom.ToLowerInvariant();
                room.RoomTypeConfidence = 0.8;

                if (lowerNom.Contains("bureau")) room.Type = RoomType.Bureau;
                else if (lowerNom.Contains("chambre") || lowerNom.Contains("ch")) room.Type = RoomType.Chambre;
                else if (lowerNom.Contains("sejour") || lowerNom.Contains("salon") || lowerNom.Contains("séjour")) room.Type = RoomType.Sejour;
                else if (lowerNom.Contains("cuisine") || lowerNom.Contains("cuis")) room.Type = RoomType.Cuisine;
                else if (lowerNom.Contains("bain") || lowerNom.Contains("sdb") || lowerNom.Contains("eau") || lowerNom.Contains("sde")) room.Type = RoomType.SalleDeBain;
                else if (lowerNom.Contains("wc") || lowerNom.Contains("toilette")) room.Type = RoomType.WC;
                else if (lowerNom.Contains("circul") || lowerNom.Contains("couloir") || lowerNom.Contains("degt") || lowerNom.Contains("dégagement")) room.Type = RoomType.Circulation;
                else if (lowerNom.Contains("hall") || lowerNom.Contains("entree") || lowerNom.Contains("entrée")) room.Type = RoomType.Hall;
                else if (lowerNom.Contains("tech") || lowerNom.Contains("elec") || lowerNom.Contains("tgbt")) room.Type = RoomType.LocalTechnique;
                else if (lowerNom.Contains("rang") || lowerNom.Contains("placard") || lowerNom.Contains("celier")) room.Type = RoomType.Rangement;
                else
                {
                    room.Type = RoomType.Autre;
                    room.RoomTypeConfidence = 0.4;
                    room.MessagesAvertissement.Add($"Classification inconnue pour '{room.Nom}'.");
                }

                // 3. Cas Ambigus (ex: meuble 'bureau' confondu avec pièce 'bureau')
                if (room.Type == RoomType.Bureau && room.Surface < 20000)
                {
                    room.RoomTypeConfidence = 0.2;
                    room.MessagesAvertissement.Add("Surface très petite pour un bureau, probable confusion avec du mobilier.");
                }

                CalculateOverall(room);
            }
        }

        private void CalculateOverall(Room room)
        {
            // Overall = Moyenne pondérée (ou le minimum pour être conservateur)
            room.OverallConfidence = Math.Min(room.GeometryConfidence, room.RoomTypeConfidence);

            if (room.GeometryConfidence < 0.8)
                room.MessagesAvertissement.Add($"Qualité géométrique faible ({room.GeometryConfidence:P0}).");
            if (room.NbOuvertures > 0)
                room.MessagesAvertissement.Add($"{room.NbOuvertures} ouverture(s) (gaps) détectée(s) sur le contour.");

            if (room.OverallConfidence >= 0.8 && room.MessagesAvertissement.Count == 0)
                room.Statut = RoomStatus.OK;
            else
                room.Statut = RoomStatus.A_VERIFIER;
        }
    }
}
