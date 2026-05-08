using System.Configuration;
using System.Data;
using System.Windows;

namespace Noticore
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }


    public class MonitorEngine
    {

        public void StartMonitoring()
        {

        }
    }

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // keeps app alive when all windows are closed
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // logic
            var monitor = new MonitorLogic();
            monitor.Initialize();
        }
    } 
}

