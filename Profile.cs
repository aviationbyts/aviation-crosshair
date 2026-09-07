using System;
using System.Collections.Generic;

namespace AviationCrosshair.Models
{
    public class Profile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "New Profile";
        public string SelectedCrosshairId { get; set; } = "";
        public double OverlayX { get; set; } = 0.5;   // fraction of screen width, 0-1
        public double OverlayY { get; set; } = 0.5;   // fraction of screen height, 0-1
        public double OverlayScale { get; set; } = 1.0;
        public int MonitorIndex { get; set; } = 0;
    }

    public class HotkeyConfig
    {
        public string ToggleOverlay { get; set; } = "F8";
        public string NextCrosshair { get; set; } = "F9";
        public string PreviousCrosshair { get; set; } = "F7";
        public string HideUi { get; set; } = "F10";
    }

    public class AppSettings
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool StartOverlayAutomatically { get; set; } = false;
        public bool RememberLastCrosshair { get; set; } = true;
        public bool RememberLastProfile { get; set; } = true;

        public bool OverlayAlwaysOnTop { get; set; } = true;
        public bool OverlayClickThrough { get; set; } = true;
        public double OverlayOpacity { get; set; } = 1.0;
        public int OverlayMonitorIndex { get; set; } = 0;
        public double OverlayX { get; set; } = 0.5;
        public double OverlayY { get; set; } = 0.5;
        public double OverlayScale { get; set; } = 1.0;

        public HotkeyConfig Hotkeys { get; set; } = new HotkeyConfig();

        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
        public double UiScale { get; set; } = 1.0;

        public string LastSelectedCrosshairId { get; set; } = "";
        public string LastProfileId { get; set; } = "";

        public List<string> FavoriteCrosshairIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// Root object persisted to disk: everything the app needs to restore state.
    /// </summary>
    public class AppData
    {
        public AppSettings Settings { get; set; } = new AppSettings();
        public List<Profile> Profiles { get; set; } = new List<Profile>();
        public List<CrosshairSettings> CustomCrosshairs { get; set; } = new List<CrosshairSettings>();
    }
}
