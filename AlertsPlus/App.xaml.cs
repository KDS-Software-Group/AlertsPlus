using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace AlertPlus
{
    public partial class App : Application
    {
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // creates mutex to ensure single instance
            _mutex = new Mutex(true, "AlertPlus_SingleInstance", out bool isNewInstance);

            // sends message if instance already running then exits
            if (!isNewInstance)
            {
                NativeMethods.PostMessage(
                    NativeMethods.FindWindow(null, "AlertPlus"),
                    NativeMethods.WM_SHOWINSTANCE, IntPtr.Zero, IntPtr.Zero);
                Application.Current.Shutdown();
                return;
            }

            // wpf stuff
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // splash screen stuff
            var splash = new SplashScreen(() => LaunchApp());
            splash.Show();
        }

        private void LaunchApp()
        {
            // initializes monitor logic and main window, applies title bar style based on settings, and shows the main window
            try
            {
                var monitor = new MonitorLogic();
                monitor.Initialize();

                string style = new SettingsRepository().GetSetting("TitleBarStyle", "MacOS");
                var mainWindow = new MainContentArea();

                if (style == "Windows")
                {
                    mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                    mainWindow.AllowsTransparency = false;
                    mainWindow.Background = new SolidColorBrush(Color.FromRgb(18, 18, 20));
                    mainWindow.BorderThickness = new Thickness(0);
                    mainWindow.ResizeMode = ResizeMode.CanResize;
                }

                mainWindow.Show();
            }
            // error throwing incase of this garbage code breaking or something
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error:\n\n{ex.GetType().Name}\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "AlertsPlus Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}