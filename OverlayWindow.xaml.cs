using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms; // for Screen (multi-monitor enumeration)
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AviationCrosshair.Core;
using AviationCrosshair.Models;

namespace AviationCrosshair.Overlay
{
    public partial class OverlayWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x80; // keeps it out of Alt-Tab

        private bool _clickThroughApplied = false;

        public OverlayWindow()
        {
            InitializeComponent();
            SourceInitialized += (_, _) => ApplyClickThrough(true);
        }

        /// <summary>
        /// Makes the window pass all mouse/keyboard input straight to whatever is
        /// underneath it - true click-through, not just "topmost". This is the
        /// standard supported technique on Windows (extended window style bits);
        /// there is no supported way to make a window *partially* click-through,
        /// so the whole overlay is transparent to input while visible.
        /// </summary>
        public void ApplyClickThrough(bool enabled)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enabled)
                exStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW;
            else
                exStyle = (exStyle | WS_EX_LAYERED | WS_EX_TOOLWINDOW) & ~WS_EX_TRANSPARENT;

            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
            _clickThroughApplied = enabled;
        }

        public void UpdateCrosshair(CrosshairSettings settings)
        {
            var group = CrosshairRenderer.Render(settings);
            var bounds = group.Bounds;

            // Pad so outline/shadow never gets clipped.
            double pad = 24;
            double w = Math.Max(1, bounds.Width + pad * 2);
            double h = Math.Max(1, bounds.Height + pad * 2);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.PushTransform(new TranslateTransform(w / 2 - bounds.X - bounds.Width / 2, h / 2 - bounds.Y - bounds.Height / 2));
                dc.DrawDrawing(group);
                dc.Pop();
            }

            var rtb = new RenderTargetBitmap((int)Math.Ceiling(w), (int)Math.Ceiling(h), 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            CrosshairImage.Source = rtb;
            Width = w;
            Height = h;
        }

        /// <summary>
        /// Positions the overlay on the chosen monitor at the given fractional
        /// X/Y (0-1 of that monitor's working area) and applies the scale.
        /// </summary>
        public void ApplyPosition(int monitorIndex, double fracX, double fracY, double scale)
        {
            var screens = Screen.AllScreens;
            if (screens.Length == 0) return;
            var screen = monitorIndex >= 0 && monitorIndex < screens.Length ? screens[monitorIndex] : screens[0];
            var bounds = screen.Bounds;

            double targetCenterX = bounds.Left + bounds.Width * fracX;
            double targetCenterY = bounds.Top + bounds.Height * fracY;

            ScaleTransform.ScaleX = scale;
            ScaleTransform.ScaleY = scale;

            Left = targetCenterX - Width / 2;
            Top = targetCenterY - Height / 2;
        }

        private ScaleTransform ScaleTransform
        {
            get
            {
                if (CrosshairImage.LayoutTransform is ScaleTransform st) return st;
                var newSt = new ScaleTransform(1, 1);
                CrosshairImage.LayoutTransform = newSt;
                return newSt;
            }
        }
    }
}
