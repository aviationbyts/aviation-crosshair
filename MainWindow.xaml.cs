using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AviationCrosshair.Core;
using AviationCrosshair.Hotkeys;
using AviationCrosshair.Models;
using AviationCrosshair.Overlay;
using AviationCrosshair.Services;
using Microsoft.Win32;

namespace AviationCrosshair
{
    public partial class MainWindow : Window
    {
        private enum LibraryFilter { All, Favorites, Custom }

        private readonly StorageService _storage = new();
        private AppData _data = new();
        private List<CrosshairSettings> _builtIns = new();
        private LibraryFilter _filter = LibraryFilter.All;
        private string _searchText = "";

        private CrosshairSettings? _selectedSource;   // the stored preset/custom this editor is based on
        private CrosshairSettings _editing = new();   // live working copy driven by the editor controls
        private bool _suppressEvents = false;

        private OverlayWindow? _overlay;
        private bool _overlayEnabled = false;
        private HotkeyManager? _hotkeys;
        private TrayService? _trayService;
        private readonly List<int> _registeredHotkeyIds = new();

        private static readonly (string Name, string Hex)[] PresetColors = new[]
        {
            ("White", "#FFFFFF"), ("Red", "#FF3B30"), ("Green", "#39FF14"), ("Blue", "#0A84FF"),
            ("Cyan", "#00FFFF"), ("Yellow", "#FFEE00"), ("Purple", "#BF5AF2"), ("Orange", "#FF9F0A"), ("Pink", "#FF2D95")
        };

        public MainWindow()
        {
            InitializeComponent();
            _builtIns = CrosshairPresets.BuildAll();
            _data = _storage.Load();

            BuildShapeCombo();
            BuildColorSwatches();
            BuildMonitorCombo();
            LoadSettingsIntoUi();

            RefreshLibraryList();
            SelectInitialCrosshair();

            _hotkeys = new HotkeyManager(this);
            Loaded += (_, _) => RegisterAllHotkeys();

            _trayService = new TrayService(LoadAppIcon());
            _trayService.ShowRequested += () => Dispatcher.Invoke(RestoreFromTray);
            _trayService.ToggleOverlayRequested += () => Dispatcher.Invoke(ToggleOverlay);
            _trayService.NextCrosshairRequested += () => Dispatcher.Invoke(() => StepCrosshair(1));
            _trayService.PreviousCrosshairRequested += () => Dispatcher.Invoke(() => StepCrosshair(-1));
            _trayService.ExitRequested += () => Dispatcher.Invoke(() => { _forceExit = true; Close(); });

            if (_data.Settings.StartOverlayAutomatically)
                ToggleOverlay();
        }

        private bool _forceExit = false;

        private System.Drawing.Icon LoadAppIcon()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null) return icon;
                }
            }
            catch { /* fall through to default */ }
            return System.Drawing.SystemIcons.Application;
        }

        // ===================================================================
        // Setup helpers
        // ===================================================================

        private void BuildShapeCombo()
        {
            ShapeCombo.ItemsSource = Enum.GetValues(typeof(CrosshairShape));
        }

        private void BuildColorSwatches()
        {
            ColorSwatches.Children.Clear();
            foreach (var (name, hex) in PresetColors)
            {
                var btn = new Button
                {
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Tag = hex,
                    ToolTip = name,
                    Cursor = Cursors.Hand
                };
                btn.Click += (_, _) => { HexBox.Text = hex; };
                ColorSwatches.Children.Add(btn);
            }
        }

        private void BuildMonitorCombo()
        {
            MonitorCombo.Items.Clear();
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                MonitorCombo.Items.Add($"Monitor {i + 1} ({screens[i].Bounds.Width}x{screens[i].Bounds.Height}){(screens[i].Primary ? " - Primary" : "")}");
            }
            if (MonitorCombo.Items.Count > 0)
                MonitorCombo.SelectedIndex = Math.Clamp(_data.Settings.OverlayMonitorIndex, 0, MonitorCombo.Items.Count - 1);
        }

        private void LoadSettingsIntoUi()
        {
            _suppressEvents = true;
            var s = _data.Settings;
            ChkStartWithWindows.IsChecked = s.StartWithWindows;
            ChkMinimizeToTray.IsChecked = s.MinimizeToTray;
            ChkStartOverlayAuto.IsChecked = s.StartOverlayAutomatically;
            ChkRememberCrosshair.IsChecked = s.RememberLastCrosshair;
            ChkRememberProfile.IsChecked = s.RememberLastProfile;
            ChkAlwaysOnTop.IsChecked = s.OverlayAlwaysOnTop;
            ChkClickThrough.IsChecked = s.OverlayClickThrough;
            SliderOverlayOpacity.Value = s.OverlayOpacity;
            HotkeyToggleBox.Text = s.Hotkeys.ToggleOverlay;
            HotkeyNextBox.Text = s.Hotkeys.NextCrosshair;
            HotkeyPrevBox.Text = s.Hotkeys.PreviousCrosshair;
            HotkeyHideBox.Text = s.Hotkeys.HideUi;
            ThemeCombo.SelectedIndex = s.Theme == "Light" ? 1 : 0;
            ApplyStartWithWindows(s.StartWithWindows);
            _suppressEvents = false;
        }

        // ===================================================================
        // Library list (Crosshairs / Favorites / Custom)
        // ===================================================================

        public class CrosshairListItem
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public bool IsFavorite { get; set; }
            public CrosshairSettings Source { get; set; } = new();
        }

        private IEnumerable<CrosshairSettings> AllCrosshairs => _builtIns.Concat(_data.CustomCrosshairs);

        private void RefreshLibraryList()
        {
            IEnumerable<CrosshairSettings> source = _filter switch
            {
                LibraryFilter.Favorites => AllCrosshairs.Where(c => _data.Settings.FavoriteCrosshairIds.Contains(c.Id)),
                LibraryFilter.Custom => _data.CustomCrosshairs,
                _ => AllCrosshairs
            };

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var q = _searchText.Trim().ToLowerInvariant();
                source = source.Where(c =>
                    c.Name.ToLowerInvariant().Contains(q) ||
                    c.Category.ToLowerInvariant().Contains(q) ||
                    c.Tags.Any(t => t.ToLowerInvariant().Contains(q)));
            }

            var items = source.Select(c => new CrosshairListItem
            {
                Id = c.Id,
                Name = c.Name,
                Category = c.Category,
                IsFavorite = _data.Settings.FavoriteCrosshairIds.Contains(c.Id),
                Source = c
            }).ToList();

            CrosshairList.ItemsSource = items;
        }

        private void SelectInitialCrosshair()
        {
            CrosshairSettings? initial = null;
            if (_data.Settings.RememberLastCrosshair && !string.IsNullOrEmpty(_data.Settings.LastSelectedCrosshairId))
                initial = AllCrosshairs.FirstOrDefault(c => c.Id == _data.Settings.LastSelectedCrosshairId);
            initial ??= _builtIns.FirstOrDefault();
            if (initial != null) LoadCrosshairIntoEditor(initial);
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var b in new[] { NavCrosshairs, NavFavorites, NavCustom, NavProfiles, NavSettings, NavAbout })
                b.Style = (Style)FindResource("SidebarButton");

            LibrarySection.Visibility = Visibility.Collapsed;
            ProfilesSection.Visibility = Visibility.Collapsed;
            SettingsSection.Visibility = Visibility.Collapsed;
            AboutSection.Visibility = Visibility.Collapsed;
            EditorPanel.Visibility = Visibility.Visible;

            var btn = (Button)sender;
            btn.Style = (Style)FindResource("SidebarButtonActive");

            if (btn == NavCrosshairs) { _filter = LibraryFilter.All; LibrarySection.Visibility = Visibility.Visible; RefreshLibraryList(); }
            else if (btn == NavFavorites) { _filter = LibraryFilter.Favorites; LibrarySection.Visibility = Visibility.Visible; RefreshLibraryList(); }
            else if (btn == NavCustom) { _filter = LibraryFilter.Custom; LibrarySection.Visibility = Visibility.Visible; RefreshLibraryList(); }
            else if (btn == NavProfiles) { ProfilesSection.Visibility = Visibility.Visible; EditorPanel.Visibility = Visibility.Collapsed; RefreshProfileList(); }
            else if (btn == NavSettings) { SettingsSection.Visibility = Visibility.Visible; EditorPanel.Visibility = Visibility.Collapsed; }
            else if (btn == NavAbout) { AboutSection.Visibility = Visibility.Visible; EditorPanel.Visibility = Visibility.Collapsed; }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            RefreshLibraryList();
        }

        private void CrosshairList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CrosshairList.SelectedItem is CrosshairListItem item)
                LoadCrosshairIntoEditor(item.Source);
        }

        private void FavoriteToggle_Click(object sender, RoutedEventArgs e)
        {
            var id = (string)((ToggleButton)sender).Tag;
            if (_data.Settings.FavoriteCrosshairIds.Contains(id))
                _data.Settings.FavoriteCrosshairIds.Remove(id);
            else
                _data.Settings.FavoriteCrosshairIds.Add(id);
            _storage.Save(_data);
            RefreshLibraryList();
        }

        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            var rnd = new Random();
            var shapes = (CrosshairShape[])Enum.GetValues(typeof(CrosshairShape));
            _editing.Shape = shapes[rnd.Next(shapes.Length)];
            _editing.LineLength = rnd.Next(4, 22);
            _editing.LineThickness = rnd.Next(1, 5);
            _editing.Gap = rnd.Next(0, 16);
            _editing.Size = rnd.Next(10, 32);
            _editing.Rotation = rnd.Next(0, 4) * 90;
            _editing.CenterDot = rnd.Next(0, 2) == 1;
            _editing.CenterDotSize = rnd.Next(1, 5);
            _editing.OutlineEnabled = rnd.Next(0, 2) == 1;
            var (_, hex) = PresetColors[rnd.Next(PresetColors.Length)];
            _editing.ColorHex = hex;
            _editing.TopLine = rnd.Next(0, 2) == 1 || true;
            _editing.BottomLine = rnd.Next(0, 2) == 1 || true;
            _editing.LeftLine = rnd.Next(0, 2) == 1 || true;
            _editing.RightLine = rnd.Next(0, 2) == 1 || true;
            _editing.Name = "Random " + rnd.Next(1000, 9999);
            PushEditingToControls();
            RenderPreview();
        }

        // ===================================================================
        // Editor <-> preview
        // ===================================================================

        private void LoadCrosshairIntoEditor(CrosshairSettings source)
        {
            _selectedSource = source;
            _editing = (CrosshairSettings)source.Clone();
            _editing.Id = source.Id; // keep identity for Apply-to-custom logic
            EditorTitle.Text = source.Name;
            BtnDeleteCustom.Visibility = (!source.IsBuiltIn) ? Visibility.Visible : Visibility.Collapsed;
            PushEditingToControls();
            RenderPreview();
        }

        private void PushEditingToControls()
        {
            _suppressEvents = true;
            ShapeCombo.SelectedItem = _editing.Shape;
            SizeSlider.Value = _editing.Size;
            ThicknessSlider.Value = _editing.LineThickness;
            GapSlider.Value = _editing.Gap;
            LineLengthSlider.Value = _editing.LineLength;
            OpacitySlider.Value = _editing.Opacity;
            RotationSlider.Value = _editing.Rotation;
            ChkTop.IsChecked = _editing.TopLine;
            ChkBottom.IsChecked = _editing.BottomLine;
            ChkLeft.IsChecked = _editing.LeftLine;
            ChkRight.IsChecked = _editing.RightLine;
            ChkRoundedCaps.IsChecked = _editing.RoundedCaps;
            ChkCenterDot.IsChecked = _editing.CenterDot;
            DotSizeSlider.Value = _editing.CenterDotSize;
            HexBox.Text = _editing.ColorHex;
            ChkOutline.IsChecked = _editing.OutlineEnabled;
            OutlineThicknessSlider.Value = _editing.OutlineThickness;
            OutlineHexBox.Text = _editing.OutlineColorHex;
            ChkShadow.IsChecked = _editing.Shadow;
            ChkDynamic.IsChecked = _editing.DynamicBehavior;
            _suppressEvents = false;
        }

        private void Editor_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;
            PullControlsIntoEditing();
            RenderPreview();
        }

        private void Editor_SliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressEvents) return;
            PullControlsIntoEditing();
            RenderPreview();
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressEvents) return;
            _editing.ColorHex = HexBox.Text;
            RenderPreview();
        }

        private void OutlineHexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressEvents) return;
            _editing.OutlineColorHex = OutlineHexBox.Text;
            RenderPreview();
        }

        private void PullControlsIntoEditing()
        {
            if (ShapeCombo.SelectedItem is CrosshairShape shape) _editing.Shape = shape;
            _editing.Size = SizeSlider.Value;
            _editing.LineThickness = ThicknessSlider.Value;
            _editing.Gap = GapSlider.Value;
            _editing.LineLength = LineLengthSlider.Value;
            _editing.Opacity = OpacitySlider.Value;
            _editing.Rotation = RotationSlider.Value;
            _editing.TopLine = ChkTop.IsChecked == true;
            _editing.BottomLine = ChkBottom.IsChecked == true;
            _editing.LeftLine = ChkLeft.IsChecked == true;
            _editing.RightLine = ChkRight.IsChecked == true;
            _editing.RoundedCaps = ChkRoundedCaps.IsChecked == true;
            _editing.CenterDot = ChkCenterDot.IsChecked == true;
            _editing.CenterDotSize = DotSizeSlider.Value;
            _editing.OutlineEnabled = ChkOutline.IsChecked == true;
            _editing.OutlineThickness = OutlineThicknessSlider.Value;
            _editing.Shadow = ChkShadow.IsChecked == true;
            _editing.DynamicBehavior = ChkDynamic.IsChecked == true;
        }

        private void RenderPreview()
        {
            PreviewCanvas.Children.Clear();
            PreviewNameText.Text = _editing.Name;
            DrawPreviewGrid();

            var group = CrosshairRenderer.Render(_editing);
            var drawingImage = new DrawingImage(group) { };
            var img = new System.Windows.Controls.Image
            {
                Source = drawingImage,
                Width = Math.Max(1, group.Bounds.Width + 60),
                Height = Math.Max(1, group.Bounds.Height + 60),
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };
            Canvas.SetLeft(img, PreviewCanvas.ActualWidth / 2 - img.Width / 2);
            Canvas.SetTop(img, PreviewCanvas.ActualHeight / 2 - img.Height / 2);
            PreviewCanvas.Children.Add(img);

            if (_overlayEnabled && _overlay != null)
            {
                _overlay.UpdateCrosshair(_editing);
                PositionOverlay();
            }
        }

        private void DrawPreviewGrid()
        {
            PreviewGridCanvas.Children.Clear();
            double w = PreviewGridCanvas.ActualWidth <= 0 ? 700 : PreviewGridCanvas.ActualWidth;
            double h = PreviewGridCanvas.ActualHeight <= 0 ? 260 : PreviewGridCanvas.ActualHeight;
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            for (double x = 0; x < w; x += 40)
            {
                var line = new System.Windows.Shapes.Line { X1 = x, Y1 = 0, X2 = x, Y2 = h, Stroke = brush, StrokeThickness = 1 };
                PreviewGridCanvas.Children.Add(line);
            }
            for (double y = 0; y < h; y += 40)
            {
                var line = new System.Windows.Shapes.Line { X1 = 0, Y1 = y, X2 = w, Y2 = y, Stroke = brush, StrokeThickness = 1 };
                PreviewGridCanvas.Children.Add(line);
            }
        }

        // ===================================================================
        // Apply / Save / Reset / Export / Import / Delete
        // ===================================================================

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSource != null && !_selectedSource.IsBuiltIn)
            {
                // Persist changes back into this custom crosshair.
                var idx = _data.CustomCrosshairs.FindIndex(c => c.Id == _selectedSource.Id);
                if (idx >= 0)
                {
                    _editing.IsBuiltIn = false;
                    _data.CustomCrosshairs[idx] = (CrosshairSettings)_editing.Clone();
                    _data.CustomCrosshairs[idx].Id = _selectedSource.Id;
                }
            }
            _data.Settings.LastSelectedCrosshairId = _editing.Id;
            _storage.Save(_data);
            RefreshLibraryList();
            if (_overlayEnabled && _overlay != null)
            {
                _overlay.UpdateCrosshair(_editing);
                PositionOverlay();
            }
            MessageBox.Show("Applied.", "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveAsNew_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SimpleInputDialog("Save Crosshair", "Name for this crosshair:", _editing.Name);
            if (dlg.ShowDialog() != true) return;

            var copy = (CrosshairSettings)_editing.Clone();
            copy.Name = dlg.Value;
            copy.Category = "Custom";
            copy.IsBuiltIn = false;
            _data.CustomCrosshairs.Add(copy);
            _storage.Save(_data);
            _filter = LibraryFilter.Custom;
            RefreshLibraryList();
            LoadCrosshairIntoEditor(copy);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSource != null) LoadCrosshairIntoEditor(_selectedSource);
        }

        private void BtnDeleteCustom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSource == null || _selectedSource.IsBuiltIn) return;
            var result = MessageBox.Show($"Delete '{_selectedSource.Name}'?", "Aviation Crosshair", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            _data.CustomCrosshairs.RemoveAll(c => c.Id == _selectedSource.Id);
            _data.Settings.FavoriteCrosshairIds.Remove(_selectedSource.Id);
            _storage.Save(_data);
            RefreshLibraryList();
            SelectInitialCrosshair();
        }

        private void BtnExportCrosshair_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "Crosshair JSON (*.json)|*.json", FileName = _editing.Name + ".json" };
            if (dlg.ShowDialog() == true)
            {
                if (!_storage.ExportCrosshair(_editing, dlg.FileName))
                    MessageBox.Show("Could not export crosshair.", "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportCrosshair_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Crosshair JSON (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                if (_storage.TryImportCrosshair(dlg.FileName, out var imported, out var error) && imported != null)
                {
                    _data.CustomCrosshairs.Add(imported);
                    _storage.Save(_data);
                    _filter = LibraryFilter.Custom;
                    RefreshLibraryList();
                    LoadCrosshairIntoEditor(imported);
                }
                else
                {
                    MessageBox.Show(error, "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ===================================================================
        // Overlay
        // ===================================================================

        private void BtnToggleOverlay_Click(object sender, RoutedEventArgs e) => ToggleOverlay();

        private void ToggleOverlay()
        {
            if (!_overlayEnabled)
            {
                _overlay ??= new OverlayWindow();
                _overlay.Show();
                _overlay.ApplyClickThrough(_data.Settings.OverlayClickThrough);
                _overlay.Topmost = _data.Settings.OverlayAlwaysOnTop;
                _overlay.Opacity = _data.Settings.OverlayOpacity;
                _overlay.UpdateCrosshair(_editing);
                PositionOverlay();
                _overlayEnabled = true;
                OverlayStatusText.Text = "Overlay: on";
                BtnToggleOverlay.Content = "Disable Overlay";
            }
            else
            {
                _overlay?.Hide();
                _overlayEnabled = false;
                OverlayStatusText.Text = "Overlay: off";
                BtnToggleOverlay.Content = "Enable Overlay";
            }
        }

        private void PositionOverlay()
        {
            if (_overlay == null) return;
            _overlay.ApplyPosition(_data.Settings.OverlayMonitorIndex, _data.Settings.OverlayX, _data.Settings.OverlayY, _data.Settings.OverlayScale);
        }

        private void StepCrosshair(int direction)
        {
            var list = ((IEnumerable<CrosshairListItem>)CrosshairList.ItemsSource ?? Enumerable.Empty<CrosshairListItem>()).ToList();
            if (list.Count == 0) return;
            int idx = list.FindIndex(i => i.Id == _editing.Id);
            idx = (idx < 0) ? 0 : (idx + direction + list.Count) % list.Count;
            CrosshairList.SelectedIndex = idx;
        }

        // ===================================================================
        // Profiles
        // ===================================================================

        private void RefreshProfileList() => ProfileList.ItemsSource = _data.Profiles.ToList();

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileList.SelectedItem is Profile p) LoadProfile(p);
        }

        private void LoadProfile(Profile p)
        {
            var crosshair = AllCrosshairs.FirstOrDefault(c => c.Id == p.SelectedCrosshairId);
            if (crosshair != null) LoadCrosshairIntoEditor(crosshair);
            _data.Settings.OverlayX = p.OverlayX;
            _data.Settings.OverlayY = p.OverlayY;
            _data.Settings.OverlayScale = p.OverlayScale;
            _data.Settings.OverlayMonitorIndex = p.MonitorIndex;
            _data.Settings.LastProfileId = p.Id;
            _storage.Save(_data);
            PositionOverlay();
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SimpleInputDialog("New Profile", "Profile name:", "New Profile");
            if (dlg.ShowDialog() != true) return;
            var p = new Profile { Name = dlg.Value, SelectedCrosshairId = _editing.Id };
            _data.Profiles.Add(p);
            _storage.Save(_data);
            RefreshProfileList();
        }

        private void BtnRenameProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileList.SelectedItem is not Profile p) return;
            var dlg = new SimpleInputDialog("Rename Profile", "Profile name:", p.Name);
            if (dlg.ShowDialog() != true) return;
            p.Name = dlg.Value;
            _storage.Save(_data);
            RefreshProfileList();
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileList.SelectedItem is not Profile p) return;
            _data.Profiles.Remove(p);
            _storage.Save(_data);
            RefreshProfileList();
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileList.SelectedItem is not Profile p)
            {
                BtnNewProfile_Click(sender, e);
                return;
            }
            p.SelectedCrosshairId = _editing.Id;
            p.OverlayX = _data.Settings.OverlayX;
            p.OverlayY = _data.Settings.OverlayY;
            p.OverlayScale = _data.Settings.OverlayScale;
            p.MonitorIndex = _data.Settings.OverlayMonitorIndex;
            _storage.Save(_data);
        }

        private void BtnExportProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileList.SelectedItem is not Profile p) return;
            var dlg = new SaveFileDialog { Filter = "Profile JSON (*.json)|*.json", FileName = p.Name + ".json" };
            if (dlg.ShowDialog() == true) _storage.ExportProfile(p, dlg.FileName);
        }

        private void BtnImportProfile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Profile JSON (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                if (_storage.TryImportProfile(dlg.FileName, out var profile, out var error) && profile != null)
                {
                    _data.Profiles.Add(profile);
                    _storage.Save(_data);
                    RefreshProfileList();
                }
                else
                {
                    MessageBox.Show(error, "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ===================================================================
        // Settings page
        // ===================================================================

        private void SettingsChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;
            var s = _data.Settings;
            s.StartWithWindows = ChkStartWithWindows.IsChecked == true;
            s.MinimizeToTray = ChkMinimizeToTray.IsChecked == true;
            s.StartOverlayAutomatically = ChkStartOverlayAuto.IsChecked == true;
            s.RememberLastCrosshair = ChkRememberCrosshair.IsChecked == true;
            s.RememberLastProfile = ChkRememberProfile.IsChecked == true;
            s.OverlayAlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;
            s.OverlayClickThrough = ChkClickThrough.IsChecked == true;
            ApplyStartWithWindows(s.StartWithWindows);
            if (_overlay != null)
            {
                _overlay.Topmost = s.OverlayAlwaysOnTop;
                _overlay.ApplyClickThrough(s.OverlayClickThrough);
            }
            _storage.Save(_data);
        }

        private void SettingsSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressEvents) return;
            _data.Settings.OverlayOpacity = SliderOverlayOpacity.Value;
            if (_overlay != null) _overlay.Opacity = _data.Settings.OverlayOpacity;
            _storage.Save(_data);
        }

        private void MonitorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressEvents) return;
            _data.Settings.OverlayMonitorIndex = MonitorCombo.SelectedIndex;
            _storage.Save(_data);
            PositionOverlay();
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressEvents) return;
            _data.Settings.Theme = ThemeCombo.SelectedIndex == 1 ? "Light" : "Dark";
            _storage.Save(_data);
            MessageBox.Show("Theme will apply on next launch.", "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnOpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", _storage.DataFolder);
        }

        private void ApplyStartWithWindows(bool enabled)
        {
            try
            {
                const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key == null) return;
                if (enabled)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath)) key.SetValue("AviationCrosshair", exePath);
                }
                else
                {
                    key.DeleteValue("AviationCrosshair", false);
                }
            }
            catch { /* Non-fatal: registry access can fail under restricted accounts. */ }
        }

        // ===================================================================
        // Hotkeys
        // ===================================================================

        private void RegisterAllHotkeys()
        {
            if (_hotkeys == null) return;
            foreach (var id in _registeredHotkeyIds) _hotkeys.Unregister(id);
            _registeredHotkeyIds.Clear();

            TryRegisterOne(_data.Settings.Hotkeys.ToggleOverlay, ToggleOverlay);
            TryRegisterOne(_data.Settings.Hotkeys.NextCrosshair, () => StepCrosshair(1));
            TryRegisterOne(_data.Settings.Hotkeys.PreviousCrosshair, () => StepCrosshair(-1));
            TryRegisterOne(_data.Settings.Hotkeys.HideUi, ToggleUiVisibility);
        }

        private void TryRegisterOne(string keyName, Action action)
        {
            if (_hotkeys != null && _hotkeys.TryRegister(keyName, action, out var id))
                _registeredHotkeyIds.Add(id);
        }

        private void ToggleUiVisibility()
        {
            if (WindowState == WindowState.Minimized) RestoreFromTray();
            else MinimizeToTrayNow();
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.System) return;
            var mods = new List<string>();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods.Add("Ctrl");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mods.Add("Alt");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mods.Add("Shift");
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift) return;

            var combo = string.Join("+", mods.Append(key.ToString()));
            var box = (TextBox)sender;

            // Reject if already used by another action.
            var current = _data.Settings.Hotkeys;
            bool clash =
                (box != HotkeyToggleBox && combo == current.ToggleOverlay) ||
                (box != HotkeyNextBox && combo == current.NextCrosshair) ||
                (box != HotkeyPrevBox && combo == current.PreviousCrosshair) ||
                (box != HotkeyHideBox && combo == current.HideUi);

            if (clash)
            {
                MessageBox.Show("That hotkey is already in use.", "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            box.Text = combo;
            if (box == HotkeyToggleBox) current.ToggleOverlay = combo;
            else if (box == HotkeyNextBox) current.NextCrosshair = combo;
            else if (box == HotkeyPrevBox) current.PreviousCrosshair = combo;
            else if (box == HotkeyHideBox) current.HideUi = combo;

            _storage.Save(_data);
            RegisterAllHotkeys();
        }

        // ===================================================================
        // Window lifecycle / tray
        // ===================================================================

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _data.Settings.MinimizeToTray)
                MinimizeToTrayNow();
        }

        private void MinimizeToTrayNow()
        {
            Hide();
            _trayService?.Show();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _trayService?.Hide();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_forceExit && _data.Settings.MinimizeToTray)
            {
                e.Cancel = true;
                MinimizeToTrayNow();
                return;
            }

            _data.Settings.LastSelectedCrosshairId = _editing.Id;
            _storage.Save(_data);
            _hotkeys?.Dispose();
            _overlay?.Close();
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
