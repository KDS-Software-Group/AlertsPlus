using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;

namespace AlertPlus
{
    [SupportedOSPlatform("windows")]
    public partial class ViewSettings : UserControl
    {
        private readonly SettingsRepository _repo = new SettingsRepository();
        private bool _isInitializing = true;

        public ViewSettings()
        {
            InitializeComponent();
            LoadSettings();
            _isInitializing = false;
        }

        // very basic loads settings and applies them to the UI
        private void LoadSettings()
        {
            string titleBar = _repo.GetSetting("TitleBarStyle", "MacOS");
            MacOsStyle.IsChecked = titleBar == "MacOS";
            WindowsStyle.IsChecked = titleBar == "Windows";

            string position = _repo.GetSetting("NotificationPosition", "BottomRight");
            PosTopRight.IsChecked = position == "TopRight";
            PosBottomRight.IsChecked = position == "BottomRight";
            PosTopLeft.IsChecked = position == "TopLeft";
            PosBottomLeft.IsChecked = position == "BottomLeft";

            AutoLaunchToggle.IsChecked = IsAutoLaunchEnabled();
        }

        // saves settings based on what the user has selected and restarts the app to apply them
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string titleBar = MacOsStyle.IsChecked == true ? "MacOS" : "Windows";
            _repo.SaveSetting("TitleBarStyle", titleBar);

            string position = "BottomRight";
            if (PosTopRight.IsChecked == true) position = "TopRight";
            else if (PosTopLeft.IsChecked == true) position = "TopLeft";
            else if (PosBottomLeft.IsChecked == true) position = "BottomLeft";
            _repo.SaveSetting("NotificationPosition", position);

            string exePath = Process.GetCurrentProcess().MainModule!.FileName;
            Process.Start(exePath);
            Application.Current.Shutdown();
        }

        // auto launch settings stuff
        private void AutoLaunch_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (AutoLaunchToggle.IsChecked == true)
                EnableAutoLaunch();
            else
                DisableAutoLaunch();
        }

        private bool IsAutoLaunchEnabled()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("AlertsPlus") != null;
        }

        private void EnableAutoLaunch()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            string exePath = Process.GetCurrentProcess().MainModule!.FileName;
            key?.SetValue("AlertsPlus", $"\"{exePath}\"");
        }

        private void DisableAutoLaunch()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("AlertsPlus", false);
        }
    }
}