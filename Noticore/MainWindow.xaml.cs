using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Noticore
{
    public partial class MainContentArea : Window
    {
        private MonitorLogic _logic;

        public MainContentArea()
        {
            InitializeComponent();
            _logic = new MonitorLogic();
            _logic.Initialize();

            MainContentFrame.Content = new ViewHome();
        }

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
                this.DragMove();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_logic != null)
            {
                // Update the global duration setting
                // You may need to make a 'GlobalSettings' class or update the logic
            }
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
                    case "Customize":
                        MainContentFrame.Content = new ViewCustomize();
                        break;
                }
            }
        }
    }
}