using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace AlertsPlus
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        public static System.Windows.Forms.NotifyIcon? TrayIcon { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // creates mutex to ensure single instance
            _mutex = new Mutex(true, "AlertsPlus_SingleInstance", out bool isNewInstance);

            // sends message if instance already running then exits
            if (!isNewInstance)
            {
                NativeMethods.PostMessage(
                    NativeMethods.FindWindow(null, "AlertsPlus"),
                    NativeMethods.WM_SHOWINSTANCE, IntPtr.Zero, IntPtr.Zero);
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashScreen(() => LaunchApp());
            splash.Show();
        }

        private void LaunchApp()
        {
            try
            {
                var monitor = new MonitorLogic();
                monitor.Initialize();

                // system tray icon setup
                TrayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName),
                    Visible = true,
                    Text = "AlertsPlus"
                };

                TrayIcon.DoubleClick += (s, e) => ShowMainWindow();

                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("Open AlertsPlus", null, (s, e) => ShowMainWindow());
                menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                menu.Items.Add("Exit", null, (s, e) =>
                {
                    TrayIcon.Visible = false;
                    Application.Current.Shutdown();
                });
                TrayIcon.ContextMenuStrip = menu;

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
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error:\n\n{ex.GetType().Name}\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "AlertsPlus Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is MainContentArea main)
                {
                    main.Show();
                    main.Activate();
                    main.WindowState = WindowState.Normal;
                    return;
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}