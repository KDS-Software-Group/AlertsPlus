using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;


namespace AlertsPlus
{
    public partial class MainContentArea : Window
    {
        public static bool IsRestarting = false;

        public MainContentArea()
        {
            InitializeComponent();
            MainContentFrame.Content = new ViewHome();
            ApplyTitleBarStyle();
        }

        // app restarting methods so app can restart with no issues if instance already was opened
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM_SHOWINSTANCE)
            {
                this.Show();
                this.Activate();
                this.WindowState = WindowState.Normal;
                handled = true;
            }
            return IntPtr.Zero;
        }

        // mac os button functions
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
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
                this.DragMove(); // allows dragging the window when macos styling selected
            }
        }

        // grabs what style is chosen in settings and applies it to the title bar
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

        // keeps process open in background
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (IsRestarting)
                return;

            e.Cancel = true;
            Dispatcher.BeginInvoke(new Action(() => this.Hide()));
        }

        // sidebar navigation thing
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
                    case "Settings":
                        MainContentFrame.Content = new ViewSettings();
                        break;
                }
            }
        }
    }
}