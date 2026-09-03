using System;
using System.Collections.Generic;
using AutoCadCopilot.Models;
using AutoCadCopilot.Geometry;

namespace AutoCadCopilot.Analysis
{
    public class TextAnalyzer
    {
        public void AssignTextsToRooms(List<Room> rooms, List<TextEntityInfo> texts)
        {
            foreach (var textInfo in texts)
            {
                CategorizeText(textInfo);

                // Ne pas associer le cartouche ou les dimensions aux pièces
                if (textInfo.Category == TextCategory.TITLE_BLOCK || textInfo.Category == TextCategory.DIMENSION)
                    continue;

                foreach (var room in rooms)
                {
                    // Amélioration : utiliser la BoundingBox pour un pré-filtre rapide avant le Point-In-Polygon
                    if (textInfo.Position.X >= room.BoundingBox.MinPoint.X && textInfo.Position.X <= room.BoundingBox.MaxPoint.X &&
                        textInfo.Position.Y >= room.BoundingBox.MinPoint.Y && textInfo.Position.Y <= room.BoundingBox.MaxPoint.Y &&
                        MathUtils.IsPointInPolygon(textInfo.Position, room.Contour))
                    {
                        room.TextesAssocies.Add(textInfo);
                        break;
                    }
                }
            }
        }

        private void CategorizeText(TextEntityInfo textInfo)
        {
            if (string.IsNullOrWhiteSpace(textInfo.Text))
            {
                textInfo.Category = TextCategory.UNKNOWN;
                return;
            }

            string lowerText = textInfo.Text.ToLowerInvariant();

            // 1. Title Block (Cartouches)
            if (lowerText.Contains("plan") || lowerText.Contains("architecte") ||
                lowerText.Contains("projet") || lowerText.Contains("echelle") ||
                lowerText.Contains("1/"))
            {
                textInfo.Category = TextCategory.TITLE_BLOCK;
                textInfo.CategoryConfidence = 0.9;
                return;
            }

            // 2. Dimensions (Cotes)
            if (double.TryParse(lowerText.Replace(",", "."), out _))
            {
                textInfo.Category = TextCategory.DIMENSION;
                textInfo.CategoryConfidence = 0.9;
                return;
            }

            // 3. Furniture (Mobilier) - Souvent écrit en petit
            if (textInfo.Height < 10.0 &&
                (lowerText.Contains("bureau") || lowerText.Contains("lit") || lowerText.Contains("table") || lowerText.Contains("chaise")))
            {
                textInfo.Category = TextCategory.FURNITURE;
                textInfo.CategoryConfidence = 0.8;
                return;
            }

            // 4. Room Name (Par défaut pour les textes restants s'ils ont une taille décente)
            if (textInfo.Height >= 10.0)
            {
                textInfo.Category = TextCategory.ROOM_NAME;
                textInfo.CategoryConfidence = 0.7; // Modéré, sera affiné par le Classifier
                return;
            }

            textInfo.Category = TextCategory.ANNOTATION;
            textInfo.CategoryConfidence = 0.5;
        }
    }
}
