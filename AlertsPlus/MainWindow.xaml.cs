using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AlertPlus
{
    public partial class MainContentArea : Window
    {
        public MainContentArea()
        {
            InitializeComponent();
            MainContentFrame.Content = new ViewHome();
            ApplyTitleBarStyle();
            ApplyTheme();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void ApplyTitleBarStyle()
        {
            string style = new SettingsRepository().GetSetting("TitleBarStyle", "MacOS");

            if (style == "Windows")
            {
                BtnClose.Visibility = Visibility.Collapsed;
                BtnMinimize.Visibility = Visibility.Collapsed;
                BtnMaximize.Visibility = Visibility.Collapsed;
                TitleText.Visibility = Visibility.Collapsed;
                MainBorder.CornerRadius = new CornerRadius(0);
                MainBorder.BorderThickness = new Thickness(0);
                SidebarBorder.CornerRadius = new CornerRadius(0);
            }
            else
            {
                BtnClose.Visibility = Visibility.Visible;
                BtnMinimize.Visibility = Visibility.Visible;
                BtnMaximize.Visibility = Visibility.Visible;
                TitleText.Visibility = Visibility.Visible;
                MainBorder.CornerRadius = new CornerRadius(12);
                MainBorder.BorderThickness = new Thickness(1);
                SidebarBorder.CornerRadius = new CornerRadius(12, 0, 0, 12);
            }
        }

        public void ApplyTheme()
        {
            string theme = new SettingsRepository().GetSetting("Theme", "Dark");

            if (theme == "Light")
            {
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(245, 245, 247));
                SidebarBorder.Background = new SolidColorBrush(Color.FromRgb(230, 230, 235));
                TitleText.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            }
            else
            {
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(18, 18, 20));
                SidebarBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 28));
                TitleText.Foreground = new SolidColorBrush(Color.FromRgb(96, 96, 96));
            }
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            // Zoom/Maximize
            this.WindowState = (this.WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Instead of closing, just hide the window
            e.Cancel = true;
            this.Hide();
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                var sidebar = (StackPanel)clickedButton.Parent;
                foreach (var child in sidebar.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Background = Brushes.Transparent;
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                    }
                }

                clickedButton.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                clickedButton.Foreground = Brushes.White;

                string? target = clickedButton?.Tag?.ToString();
                switch (target)
                {
                    case "Home":
                        MainContentFrame.Content = new ViewHome();
                        break;
                    case "Scheduler":
                        MainContentFrame.Content = new ViewScheduler();
                        break;
                    case "Temp":
                        MainContentFrame.Content = new ViewTempWatch();
                        break;
                    case "History":
                        MainContentFrame.Content = new ViewHistory();
                        break;
                    case "Settings":
                        MainContentFrame.Content = new ViewSettings();
                        break;
                }
            }
        }
    }
}