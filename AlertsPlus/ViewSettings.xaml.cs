using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace AlertPlus
{
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
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Save title bar
            string titleBar = MacOsStyle.IsChecked == true ? "MacOS" : "Windows";
            _repo.SaveSetting("TitleBarStyle", titleBar);

            // Save notification position
            string position = "BottomRight";
            if (PosTopRight.IsChecked == true) position = "TopRight";
            else if (PosTopLeft.IsChecked == true) position = "TopLeft";
            else if (PosBottomLeft.IsChecked == true) position = "BottomLeft";
            _repo.SaveSetting("NotificationPosition", position);

            // Restart the app
            string exePath = Process.GetCurrentProcess().MainModule!.FileName;
            Process.Start(exePath);
            Application.Current.Shutdown();
        }
    }
}