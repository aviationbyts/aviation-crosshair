using System;
using System.Drawing;
using System.Windows.Forms;

namespace AviationCrosshair.Services
{
    /// <summary>
    /// Real Windows system tray integration (System.Windows.Forms.NotifyIcon,
    /// enabled via UseWindowsForms in the csproj - this is the standard,
    /// supported way to get a tray icon from a WPF app without extra packages).
    /// </summary>
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;

        public event Action? ShowRequested;
        public event Action? ToggleOverlayRequested;
        public event Action? NextCrosshairRequested;
        public event Action? PreviousCrosshairRequested;
        public event Action? ExitRequested;

        public TrayService(Icon icon)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show Aviation Crosshair", null, (_, _) => ShowRequested?.Invoke());
            menu.Items.Add("Toggle Overlay", null, (_, _) => ToggleOverlayRequested?.Invoke());
            menu.Items.Add("Next Crosshair", null, (_, _) => NextCrosshairRequested?.Invoke());
            menu.Items.Add("Previous Crosshair", null, (_, _) => PreviousCrosshairRequested?.Invoke());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Visible = false,
                Text = "Aviation Crosshair",
                ContextMenuStrip = menu
            };
            _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke();
        }

        public void Show() => _notifyIcon.Visible = true;
        public void Hide() => _notifyIcon.Visible = false;

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
