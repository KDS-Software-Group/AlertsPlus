using System.Windows;

namespace AlertPlus
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var monitor = new MonitorLogic();
            monitor.Initialize();
        }
    }
}