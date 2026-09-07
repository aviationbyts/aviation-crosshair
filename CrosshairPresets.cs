using System.Collections.Generic;
using AviationCrosshair.Models;

namespace AviationCrosshair.Core
{
    /// <summary>
    /// Builds the full built-in preset library. Every preset is a real, distinct
    /// combination of shape/lines/color/outline/dot parameters - nothing here is
    /// a placeholder, each one renders visibly differently from the others.
    /// </summary>
    public static class CrosshairPresets
    {
        private static CrosshairSettings Make(
            string name, string category, string color,
            CrosshairShape shape = CrosshairShape.Cross,
            double lineLength = 10, double thickness = 2, double gap = 4,
            bool top = true, bool bottom = true, bool left = true, bool right = true,
            bool dot = false, double dotSize = 2,
            bool outline = true, string outlineColor = "#000000", double outlineThickness = 1,
            double rotation = 0, double size = 20, bool shadow = false,
            bool dynamic = false, bool roundedCaps = false, params string[] tags)
        {
            return new CrosshairSettings
            {
                Name = name,
                Category = category,
                Tags = tags,
                IsBuiltIn = true,
                Shape = shape,
                ColorHex = color,
                LineLength = lineLength,
                LineThickness = thickness,
                Gap = gap,
                TopLine = top,
                BottomLine = bottom,
                LeftLine = left,
                RightLine = right,
                CenterDot = dot,
                CenterDotSize = dotSize,
                OutlineEnabled = outline,
                OutlineColorHex = outlineColor,
                OutlineThickness = outlineThickness,
                Rotation = rotation,
                Size = size,
                Shadow = shadow,
                DynamicBehavior = dynamic,
                RoundedCaps = roundedCaps
            };
        }

        public static List<CrosshairSettings> BuildAll()
        {
            var list = new List<CrosshairSettings>();

            // ---------------- Classic (10) ----------------
            list.Add(Make("Classic Cross", "Classic", "#FFFFFF", lineLength: 10, thickness: 2, gap: 4, tags: new[] { "classic", "cross" }));
            list.Add(Make("Classic Dot", "Classic", "#FFFFFF", shape: CrosshairShape.DotOnly, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 3, tags: new[] { "classic", "dot" }));
            list.Add(Make("Classic Circle", "Classic", "#FFFFFF", shape: CrosshairShape.Circle, size: 14, top: false, bottom: false, left: false, right: false, tags: new[] { "classic", "circle" }));
            list.Add(Make("Classic Plus", "Classic", "#FFFFFF", lineLength: 12, thickness: 3, gap: 0, tags: new[] { "classic", "plus" }));
            list.Add(Make("Classic Four Lines", "Classic", "#FFFFFF", lineLength: 8, thickness: 2, gap: 6, tags: new[] { "classic" }));
            list.Add(Make("Classic Small", "Classic", "#FFFFFF", lineLength: 6, thickness: 1, gap: 2, tags: new[] { "classic", "small" }));
            list.Add(Make("Classic Large", "Classic", "#FFFFFF", lineLength: 18, thickness: 3, gap: 6, tags: new[] { "classic", "large" }));
            list.Add(Make("Classic Thin", "Classic", "#FFFFFF", lineLength: 12, thickness: 1, gap: 4, tags: new[] { "classic", "thin" }));
            list.Add(Make("Classic Thick", "Classic", "#FFFFFF", lineLength: 12, thickness: 4, gap: 4, tags: new[] { "classic", "thick" }));
            list.Add(Make("Classic Precision", "Classic", "#FFFFFF", lineLength: 14, thickness: 1, gap: 8, dot: true, dotSize: 1, tags: new[] { "classic", "precision" }));

            // ---------------- Tactical (10) ----------------
            list.Add(Make("Tactical 1", "Tactical", "#00FF00", lineLength: 9, thickness: 2, gap: 5, dot: true, dotSize: 1.5, tags: new[] { "tactical" }));
            list.Add(Make("Tactical 2", "Tactical", "#00FF00", lineLength: 11, thickness: 2, gap: 3, tags: new[] { "tactical" }));
            list.Add(Make("Tactical 3", "Tactical", "#00FF00", lineLength: 7, thickness: 3, gap: 2, tags: new[] { "tactical" }));
            list.Add(Make("Tactical Compact", "Tactical", "#00FF00", lineLength: 5, thickness: 2, gap: 2, tags: new[] { "tactical", "compact" }));
            list.Add(Make("Tactical Precision", "Tactical", "#00FF00", lineLength: 16, thickness: 1, gap: 10, dot: true, dotSize: 1, tags: new[] { "tactical", "precision" }));
            list.Add(Make("Tactical Open", "Tactical", "#00FF00", lineLength: 10, thickness: 2, gap: 12, tags: new[] { "tactical", "open" }));
            list.Add(Make("Tactical Closed", "Tactical", "#00FF00", lineLength: 10, thickness: 2, gap: 0, tags: new[] { "tactical", "closed" }));
            list.Add(Make("Tactical Ring", "Tactical", "#00FF00", shape: CrosshairShape.CircleCross, size: 16, lineLength: 6, thickness: 2, gap: 10, tags: new[] { "tactical", "ring" }));
            list.Add(Make("Tactical Dot", "Tactical", "#00FF00", shape: CrosshairShape.DotOnly, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 2.5, tags: new[] { "tactical", "dot" }));
            list.Add(Make("Tactical Minimal", "Tactical", "#00FF00", lineLength: 4, thickness: 1, gap: 3, tags: new[] { "tactical", "minimal" }));

            // ---------------- Aviation (15) ----------------
            list.Add(Make("Aviation Primary", "Aviation", "#00E5FF", shape: CrosshairShape.CircleCross, size: 22, lineLength: 8, thickness: 2, gap: 12, dot: true, dotSize: 1.5, tags: new[] { "aviation", "hud" }));
            list.Add(Make("Aviation HUD", "Aviation", "#00E5FF", lineLength: 14, thickness: 1, gap: 16, top: true, bottom: false, left: true, right: true, tags: new[] { "aviation", "hud" }));
            list.Add(Make("Aviation Flight Director", "Aviation", "#00E5FF", shape: CrosshairShape.Brackets, size: 20, lineLength: 8, thickness: 2, gap: 10, tags: new[] { "aviation" }));
            list.Add(Make("Aviation Precision", "Aviation", "#00E5FF", lineLength: 16, thickness: 1, gap: 14, dot: true, dotSize: 1, tags: new[] { "aviation", "precision" }));
            list.Add(Make("Aviation Reticle", "Aviation", "#00E5FF", shape: CrosshairShape.Circle, size: 20, top: true, bottom: true, left: false, right: false, lineLength: 6, gap: 12, tags: new[] { "aviation" }));
            list.Add(Make("Aviation Target", "Aviation", "#FF3B30", shape: CrosshairShape.CircleCross, size: 24, lineLength: 5, thickness: 2, gap: 14, tags: new[] { "aviation", "target" }));
            list.Add(Make("Aviation Radar", "Aviation", "#00E5FF", shape: CrosshairShape.Circle, size: 26, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 2, tags: new[] { "aviation", "radar" }));
            list.Add(Make("Aviation Gun Sight", "Aviation", "#FF3B30", lineLength: 10, thickness: 2, gap: 6, dot: true, dotSize: 2, tags: new[] { "aviation" }));
            list.Add(Make("Aviation Marker", "Aviation", "#00E5FF", shape: CrosshairShape.Diamond, size: 16, top: false, bottom: false, left: false, right: false, tags: new[] { "aviation" }));
            list.Add(Make("Aviation Minimal", "Aviation", "#00E5FF", lineLength: 5, thickness: 1, gap: 6, tags: new[] { "aviation", "minimal" }));
            list.Add(Make("Aviation Pro", "Aviation", "#00E5FF", shape: CrosshairShape.CircleCross, size: 20, lineLength: 6, thickness: 2, gap: 10, outline: true, outlineThickness: 1.5, tags: new[] { "aviation" }));
            list.Add(Make("Aviation Combat", "Aviation", "#FF3B30", lineLength: 12, thickness: 3, gap: 4, dot: true, dotSize: 2, tags: new[] { "aviation", "combat" }));
            list.Add(Make("Aviation Range", "Aviation", "#00E5FF", shape: CrosshairShape.Brackets, size: 24, lineLength: 6, thickness: 1, gap: 16, tags: new[] { "aviation" }));
            list.Add(Make("Aviation Lock", "Aviation", "#FF9F0A", shape: CrosshairShape.Square, size: 18, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 1.5, tags: new[] { "aviation", "lock" }));
            list.Add(Make("Aviation Flight", "Aviation", "#00E5FF", lineLength: 10, thickness: 1, gap: 8, rotation: 45, tags: new[] { "aviation" }));

            // ---------------- Minimal (10) ----------------
            list.Add(Make("Tiny Dot", "Minimal", "#FFFFFF", shape: CrosshairShape.DotOnly, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 1, outline: false, tags: new[] { "minimal", "tiny" }));
            list.Add(Make("Micro Cross", "Minimal", "#FFFFFF", lineLength: 3, thickness: 1, gap: 1, outline: false, tags: new[] { "minimal" }));
            list.Add(Make("Single Dot", "Minimal", "#FFFFFF", shape: CrosshairShape.DotOnly, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 2, tags: new[] { "minimal" }));
            list.Add(Make("Four Pixels", "Minimal", "#FFFFFF", lineLength: 1, thickness: 1, gap: 4, tags: new[] { "minimal" }));
            list.Add(Make("Minimal Plus", "Minimal", "#FFFFFF", lineLength: 6, thickness: 1, gap: 0, tags: new[] { "minimal" }));
            list.Add(Make("Minimal Circle", "Minimal", "#FFFFFF", shape: CrosshairShape.Circle, size: 10, top: false, bottom: false, left: false, right: false, outline: false, tags: new[] { "minimal" }));
            list.Add(Make("Minimal T", "Minimal", "#FFFFFF", top: false, bottom: true, left: true, right: true, lineLength: 6, thickness: 1, gap: 2, tags: new[] { "minimal" }));
            list.Add(Make("Minimal X", "Minimal", "#FFFFFF", rotation: 45, lineLength: 6, thickness: 1, gap: 2, tags: new[] { "minimal" }));
            list.Add(Make("Minimal Square", "Minimal", "#FFFFFF", shape: CrosshairShape.Square, size: 8, top: false, bottom: false, left: false, right: false, outline: false, tags: new[] { "minimal" }));
            list.Add(Make("Minimal Diamond", "Minimal", "#FFFFFF", shape: CrosshairShape.Diamond, size: 8, top: false, bottom: false, left: false, right: false, outline: false, tags: new[] { "minimal" }));

            // ---------------- Geometric (10) ----------------
            list.Add(Make("Circle Cross", "Geometric", "#FF9F0A", shape: CrosshairShape.CircleCross, size: 18, lineLength: 8, thickness: 2, gap: 8, tags: new[] { "geometric" }));
            list.Add(Make("Double Circle", "Geometric", "#FF9F0A", shape: CrosshairShape.Circle, size: 22, top: false, bottom: false, left: false, right: false, outlineThickness: 3, tags: new[] { "geometric" }));
            list.Add(Make("Square", "Geometric", "#FF9F0A", shape: CrosshairShape.Square, size: 16, top: false, bottom: false, left: false, right: false, tags: new[] { "geometric" }));
            list.Add(Make("Diamond", "Geometric", "#FF9F0A", shape: CrosshairShape.Diamond, size: 16, top: false, bottom: false, left: false, right: false, tags: new[] { "geometric" }));
            list.Add(Make("Diamond Cross", "Geometric", "#FF9F0A", shape: CrosshairShape.Diamond, size: 14, lineLength: 6, thickness: 1, gap: 10, tags: new[] { "geometric" }));
            list.Add(Make("Hexagon", "Geometric", "#FF9F0A", shape: CrosshairShape.Hexagon, size: 18, top: false, bottom: false, left: false, right: false, tags: new[] { "geometric" }));
            list.Add(Make("Ring + Dot", "Geometric", "#FF9F0A", shape: CrosshairShape.Circle, size: 18, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 2, tags: new[] { "geometric" }));
            list.Add(Make("Four Corners", "Geometric", "#FF9F0A", shape: CrosshairShape.Brackets, size: 22, lineLength: 5, thickness: 2, gap: 14, tags: new[] { "geometric" }));
            list.Add(Make("Brackets", "Geometric", "#FF9F0A", shape: CrosshairShape.Brackets, size: 16, lineLength: 6, thickness: 2, gap: 8, tags: new[] { "geometric" }));
            list.Add(Make("Double Cross", "Geometric", "#FF9F0A", lineLength: 10, thickness: 2, gap: 4, rotation: 45, tags: new[] { "geometric" }));

            // ---------------- Competitive (10) ----------------
            list.Add(Make("Competitive Green", "Competitive", "#39FF14", lineLength: 9, thickness: 2, gap: 3, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Cyan", "Competitive", "#00FFFF", lineLength: 9, thickness: 2, gap: 3, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Red", "Competitive", "#FF073A", lineLength: 9, thickness: 2, gap: 3, tags: new[] { "competitive" }));
            list.Add(Make("Competitive White", "Competitive", "#FFFFFF", lineLength: 9, thickness: 2, gap: 3, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Yellow", "Competitive", "#FFEE00", lineLength: 9, thickness: 2, gap: 3, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Dot", "Competitive", "#39FF14", shape: CrosshairShape.DotOnly, top: false, bottom: false, left: false, right: false, dot: true, dotSize: 2, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Thin", "Competitive", "#39FF14", lineLength: 12, thickness: 1, gap: 4, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Dynamic", "Competitive", "#39FF14", lineLength: 9, thickness: 2, gap: 4, dynamic: true, tags: new[] { "competitive", "dynamic" }));
            list.Add(Make("Competitive Precision", "Competitive", "#39FF14", lineLength: 14, thickness: 1, gap: 10, dot: true, dotSize: 1, tags: new[] { "competitive" }));
            list.Add(Make("Competitive Small", "Competitive", "#39FF14", lineLength: 5, thickness: 1, gap: 2, tags: new[] { "competitive" }));

            return list;
        }
    }
}
