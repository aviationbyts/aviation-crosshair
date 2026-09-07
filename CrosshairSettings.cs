using System;

namespace AviationCrosshair.Models
{
    public enum CrosshairShape
    {
        Cross,
        Circle,
        Square,
        Diamond,
        Brackets,
        DotOnly,
        CircleCross,
        Hexagon
    }

    /// <summary>
    /// A fully self-contained description of a crosshair. Every field here maps
    /// directly to something the user can control in the editor and something
    /// CrosshairRenderer.cs uses to actually draw the crosshair.
    /// </summary>
    public class CrosshairSettings : ICloneable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Untitled";
        public string Category { get; set; } = "Custom";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool IsBuiltIn { get; set; } = false;

        // Shape
        public CrosshairShape Shape { get; set; } = CrosshairShape.Cross;

        // Lines
        public bool TopLine { get; set; } = true;
        public bool BottomLine { get; set; } = true;
        public bool LeftLine { get; set; } = true;
        public bool RightLine { get; set; } = true;
        public double LineLength { get; set; } = 10;
        public double LineThickness { get; set; } = 2;
        public double Gap { get; set; } = 4;
        public bool RoundedCaps { get; set; } = false;

        // Center dot
        public bool CenterDot { get; set; } = false;
        public double CenterDotSize { get; set; } = 2;
        public double CenterDotOpacity { get; set; } = 1.0;

        // Color
        public string ColorHex { get; set; } = "#00FF00";
        public double Opacity { get; set; } = 1.0;

        // Rotation
        public double Rotation { get; set; } = 0;

        // Outline
        public bool OutlineEnabled { get; set; } = true;
        public string OutlineColorHex { get; set; } = "#000000";
        public double OutlineThickness { get; set; } = 1;
        public double OutlineOpacity { get; set; } = 1.0;

        // Shadow
        public bool Shadow { get; set; } = false;

        // Dynamic behavior (simple, deterministic - pulses opacity slightly; safe/no external deps)
        public bool DynamicBehavior { get; set; } = false;

        // Overall size multiplier (applies to circle/square/diamond radius etc.)
        public double Size { get; set; } = 20;

        public object Clone()
        {
            return new CrosshairSettings
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = Name,
                Category = Category,
                Tags = (string[])Tags.Clone(),
                IsBuiltIn = false,
                Shape = Shape,
                TopLine = TopLine,
                BottomLine = BottomLine,
                LeftLine = LeftLine,
                RightLine = RightLine,
                LineLength = LineLength,
                LineThickness = LineThickness,
                Gap = Gap,
                RoundedCaps = RoundedCaps,
                CenterDot = CenterDot,
                CenterDotSize = CenterDotSize,
                CenterDotOpacity = CenterDotOpacity,
                ColorHex = ColorHex,
                Opacity = Opacity,
                Rotation = Rotation,
                OutlineEnabled = OutlineEnabled,
                OutlineColorHex = OutlineColorHex,
                OutlineThickness = OutlineThickness,
                OutlineOpacity = OutlineOpacity,
                Shadow = Shadow,
                DynamicBehavior = DynamicBehavior,
                Size = Size
            };
        }
    }
}
