using System.Threading;
using System.Windows;

namespace AlertPlus
{
    public partial class App : Application
    {
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, "AlertPlus_SingleInstance", out bool isNewInstance);

            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("AlertPlus is already running.", "AlertPlus", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                var monitor = new MonitorLogic();
                monitor.Initialize();

                var mainWindow = new MainContentArea();
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

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}