using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace Noticore
{
    public partial class ViewTempWatch : UserControl
    {
        private Computer _computer;
        private DispatcherTimer _timer;

        // for the graph
        private readonly List<float> _gpuHistory = new List<float>();
        private const int MaxPoints = 40; // the amount of points we want to show on the graph at once
        private bool _isInitializing = true;

        public ViewTempWatch()
        {
            InitializeComponent();

            var repo = new SettingsRepository();

            MasterToggle.IsChecked = repo.Get<bool>("MasterToggle", true);
            MonitorGpuToggle.IsChecked = repo.Get<bool>("MonitorGpuToggle", true);
            HotspotToggle.IsChecked = repo.Get<bool>("HotspotToggle", false);
            ThresholdInput.Text = repo.GetSetting("GpuThreshold", "80");

            _isInitializing = false;

            _computer = new Computer
            {
                IsCpuEnabled = false,
                IsGpuEnabled = true,
            };
            _computer.Open();

            // runs every second
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            this.Loaded += ViewTempWatch_Loaded;

            this.Unloaded += (s, e) => {
                _timer.Stop();
                _computer.Close();
            };
        }

        // saves to db settings
        private void ViewTempWatch_Loaded(object sender, RoutedEventArgs e)
        {
            var repo = new SettingsRepository();

            // unhook events so they dont save while we trying to load shouldnt happen but you dont know
            MasterToggle.Checked -= SettingChanged; MasterToggle.Unchecked -= SettingChanged;
            MonitorGpuToggle.Checked -= SettingChanged; MonitorGpuToggle.Unchecked -= SettingChanged;
            HotspotToggle.Checked -= SettingChanged; HotspotToggle.Unchecked -= SettingChanged;

            MasterToggle.IsChecked = repo.Get<bool>("MasterToggle", true);
            MonitorGpuToggle.IsChecked = repo.Get<bool>("MonitorGpuToggle", true);
            HotspotToggle.IsChecked = repo.Get<bool>("HotspotToggle", false);

            if (ThresholdInput != null)
            {
                ThresholdInput.Text = repo.GetSetting("GpuThreshold", "80");
            }

            // rehook 
            MasterToggle.Checked += SettingChanged; MasterToggle.Unchecked += SettingChanged;
            MonitorGpuToggle.Checked += SettingChanged; MonitorGpuToggle.Unchecked += SettingChanged;
            HotspotToggle.Checked += SettingChanged; HotspotToggle.Unchecked += SettingChanged;
        }

        // saves to db settings
        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            if (sender is CheckBox cb)
            {
                var repo = new SettingsRepository();
                repo.SaveSetting(cb.Name, cb.IsChecked.ToString()!);

                if (cb.Name == "MasterToggle") MonitorLogic.IsEnabled = cb.IsChecked ?? false;
                if (cb.Name == "MonitorGpuToggle") MonitorLogic.MonitorGpu = cb.IsChecked ?? false;
                if (cb.Name == "HotspotToggle") MonitorLogic.UseHotspot = cb.IsChecked ?? false;
            }
        }

        private void ThresholdInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (ThresholdInput != null && float.TryParse(ThresholdInput.Text, out float newLimit))
            {
                MonitorLogic.Threshold = newLimit;

                var repo = new SettingsRepository();
                repo.SaveSetting("GpuThreshold", newLimit.ToString());
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {

            float currentGpu = MonitorLogic.CurrentGpuTemp;

            MonitorLogic.UseHotspot = HotspotToggle.IsChecked ?? false;
            GpuTempText.Text = $"{currentGpu}°C";

            _gpuHistory.Add(currentGpu);
            if (_gpuHistory.Count > MaxPoints) _gpuHistory.RemoveAt(0);

            UpdateGraph();

            GpuTempText.Text = $"{currentGpu}°C";

            if (currentGpu >= 60)
            {
                GpuTempText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                GpuTempText.Foreground = System.Windows.Media.Brushes.White;
            }

            if (float.TryParse(ThresholdInput.Text, out float limit))
            {
                MonitorLogic.Threshold = limit;
            }

            MonitorLogic.IsEnabled = MasterToggle.IsChecked ?? false;
            MonitorLogic.MonitorGpu = MonitorGpuToggle.IsChecked ?? false;
        }

        private void UpdateGraph()
        {
            if (_gpuHistory.Count < 2) return;

            PointCollection points = new PointCollection();
            double width = GpuGraphLine.ActualWidth > 0 ? GpuGraphLine.ActualWidth : 240;
            double height = 80;
            double xStep = width / (MaxPoints - 1);

            double minTemp = 60;
            double maxTemp = 100;
            double range = maxTemp - minTemp;

            for (int i = 0; i < _gpuHistory.Count; i++)
            {
                double x = i * xStep;
                double y = height - ((_gpuHistory[i] - minTemp) * (height / range));
                y = Math.Clamp(y, 0, height);

                points.Add(new System.Windows.Point(x, y));
            }

            GpuGraphLine.Points = points;
        }

        private void GpuThresholdInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (float.TryParse(ThresholdInput.Text, out float newLimit))
            {
                // 1. Update the LIVE logic immediately
                MonitorLogic.Threshold = newLimit;

                // 2. Save it to the database so it's remembered
                var repo = new SettingsRepository();
                repo.SaveSetting("GpuThreshold", newLimit.ToString());
            }
        }

        ~ViewTempWatch() { _computer.Close(); }
    }
}
