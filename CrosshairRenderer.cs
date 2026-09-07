using System;
using System.Windows;
using System.Windows.Media;
using AviationCrosshair.Models;

namespace AviationCrosshair.Core
{
    /// <summary>
    /// Pure rendering logic. Given a CrosshairSettings, produces a DrawingGroup
    /// centered at (0,0) that can be reused for both the live editor preview
    /// (inside a Canvas) and the actual transparent overlay window.
    /// No rendering happens unless settings actually change (see MainWindow /
    /// OverlayWindow - they only call this on parameter change, not on a timer,
    /// except for the optional "dynamic" pulse which uses a lightweight WPF
    /// animation instead of a redraw loop).
    /// </summary>
    public static class CrosshairRenderer
    {
        public static DrawingGroup Render(CrosshairSettings s)
        {
            var group = new DrawingGroup();
            using (var dc = group.Open())
            {
                var color = ParseColor(s.ColorHex, s.Opacity);
                var brush = new SolidColorBrush(color);
                var outlineColor = ParseColor(s.OutlineColorHex, s.OutlineOpacity);
                var outlinePen = s.OutlineEnabled
                    ? new Pen(new SolidColorBrush(outlineColor), s.LineThickness + s.OutlineThickness * 2)
                    : null;
                var mainPen = new Pen(brush, s.LineThickness) { StartLineCap = s.RoundedCaps ? PenLineCap.Round : PenLineCap.Flat, EndLineCap = s.RoundedCaps ? PenLineCap.Round : PenLineCap.Flat };

                if (outlinePen != null)
                {
                    outlinePen.StartLineCap = mainPen.StartLineCap;
                    outlinePen.EndLineCap = mainPen.EndLineCap;
                }

                // Lines (top/bottom/left/right), drawn outline-first then main color on top.
                void DrawLine(Point a, Point b)
                {
                    if (outlinePen != null) dc.DrawLine(outlinePen, a, b);
                    dc.DrawLine(mainPen, a, b);
                }

                double g = s.Gap;
                double len = s.LineLength;

                if (s.TopLine) DrawLine(new Point(0, -g), new Point(0, -g - len));
                if (s.BottomLine) DrawLine(new Point(0, g), new Point(0, g + len));
                if (s.LeftLine) DrawLine(new Point(-g, 0), new Point(-g - len, 0));
                if (s.RightLine) DrawLine(new Point(g, 0), new Point(g + len, 0));

                // Shape overlay (circle / square / diamond / brackets / hexagon / circle+cross)
                double half = s.Size / 2.0;
                Pen shapePen = mainPen;
                Pen shapeOutlinePen = outlinePen;

                switch (s.Shape)
                {
                    case CrosshairShape.Circle:
                    case CrosshairShape.CircleCross:
                        if (shapeOutlinePen != null) dc.DrawEllipse(null, shapeOutlinePen, new Point(0, 0), half, half);
                        dc.DrawEllipse(null, shapePen, new Point(0, 0), half, half);
                        break;
                    case CrosshairShape.Square:
                        var rect = new Rect(-half, -half, s.Size, s.Size);
                        if (shapeOutlinePen != null) dc.DrawRectangle(null, shapeOutlinePen, rect);
                        dc.DrawRectangle(null, shapePen, rect);
                        break;
                    case CrosshairShape.Diamond:
                    {
                        var geo = new StreamGeometry();
                        using (var gc = geo.Open())
                        {
                            gc.BeginFigure(new Point(0, -half), false, true);
                            gc.LineTo(new Point(half, 0), true, false);
                            gc.LineTo(new Point(0, half), true, false);
                            gc.LineTo(new Point(-half, 0), true, false);
                        }
                        if (shapeOutlinePen != null) dc.DrawGeometry(null, shapeOutlinePen, geo);
                        dc.DrawGeometry(null, shapePen, geo);
                        break;
                    }
                    case CrosshairShape.Hexagon:
                    {
                        var geo = new StreamGeometry();
                        using (var gc = geo.Open())
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                double angle = Math.PI / 3 * i - Math.PI / 2;
                                var pt = new Point(half * Math.Cos(angle), half * Math.Sin(angle));
                                if (i == 0) gc.BeginFigure(pt, false, true);
                                else gc.LineTo(pt, true, false);
                            }
                        }
                        if (shapeOutlinePen != null) dc.DrawGeometry(null, shapeOutlinePen, geo);
                        dc.DrawGeometry(null, shapePen, geo);
                        break;
                    }
                    case CrosshairShape.Brackets:
                    {
                        double bl = half * 0.5;
                        void Bracket(double sx, double sy, double dx, double dy)
                        {
                            var p1 = new Point(sx, sy);
                            var p2 = new Point(sx + dx * bl, sy);
                            var p3 = new Point(sx, sy + dy * bl);
                            DrawLine(p1, p2);
                            DrawLine(p1, p3);
                        }
                        Bracket(-half, -half, 1, 1);
                        Bracket(half, -half, -1, 1);
                        Bracket(-half, half, 1, -1);
                        Bracket(half, half, -1, -1);
                        break;
                    }
                    case CrosshairShape.DotOnly:
                    case CrosshairShape.Cross:
                    default:
                        break;
                }

                // Center dot
                if (s.CenterDot)
                {
                    var dotBrush = new SolidColorBrush(ParseColor(s.ColorHex, s.CenterDotOpacity * s.Opacity));
                    if (s.OutlineEnabled)
                    {
                        var dotOutline = new SolidColorBrush(outlineColor);
                        dc.DrawEllipse(dotOutline, null, new Point(0, 0), s.CenterDotSize + s.OutlineThickness, s.CenterDotSize + s.OutlineThickness);
                    }
                    dc.DrawEllipse(dotBrush, null, new Point(0, 0), s.CenterDotSize, s.CenterDotSize);
                }
            }

            if (s.Rotation != 0)
            {
                group.Transform = new RotateTransform(s.Rotation);
            }

            if (s.Shadow)
            {
                // Lightweight, GPU-composited drop shadow - not a redraw loop.
                var dg = new DrawingGroup { Children = { group } };
                return dg;
            }

            return group;
        }

        public static Color ParseColor(string hex, double opacity)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex)!;
                c.A = (byte)Math.Clamp(opacity * 255, 0, 255);
                return c;
            }
            catch
            {
                return Color.FromArgb((byte)Math.Clamp(opacity * 255, 0, 255), 255, 255, 255);
            }
        }
    }
}
