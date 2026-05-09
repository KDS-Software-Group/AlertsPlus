using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AlertPlus
{
    public partial class ViewTempWatch : UserControl
    {
        private Computer _computer;
        private DispatcherTimer _timer;
        private readonly List<float> _gpuHistory = new List<float>();
        private readonly List<float> _cpuHistory = new List<float>();
        private const int MaxPoints = 40;
        private bool _isInitializing = true;

        public ViewTempWatch()
        {
            InitializeComponent();
            LoadSettings();
            _isInitializing = false;

            _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
            _computer.Open();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            this.Unloaded += (s, e) => { _timer.Stop(); _computer.Close(); };
        }

        private void LoadSettings()
        {
            var repo = new SettingsRepository();
            MasterToggle.IsChecked = repo.Get<bool>("MasterToggle", true);
            MonitorGpuToggle.IsChecked = repo.Get<bool>("MonitorGpuToggle", true);
            MonitorCpuToggle.IsChecked = repo.Get<bool>("MonitorCpuToggle", true);
            HotspotToggle.IsChecked = repo.Get<bool>("HotspotToggle", false);
            ThresholdInput.Text = repo.GetSetting("GpuThreshold", "80");
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (sender is CheckBox cb)
            {
                new SettingsRepository().SaveSetting(cb.Name, cb.IsChecked.ToString()!);
                if (cb.Name == "MasterToggle") MonitorLogic.IsEnabled = cb.IsChecked ?? false;
                if (cb.Name == "MonitorGpuToggle") MonitorLogic.MonitorGpu = cb.IsChecked ?? false;
                if (cb.Name == "MonitorCpuToggle") MonitorLogic.MonitorCpu = cb.IsChecked ?? false;
                if (cb.Name == "HotspotToggle") MonitorLogic.UseHotspot = cb.IsChecked ?? false;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateUI(GpuTempText, MonitorLogic.CurrentGpuTemp, _gpuHistory, GpuGraphLine);
            UpdateUI(CpuTempText, MonitorLogic.CurrentCpuTemp, _cpuHistory, CpuGraphLine);
        }

        private void UpdateUI(TextBlock textBlock, float temp, List<float> history, Polyline line)
        {
            textBlock.Text = $"{temp}°C";
            textBlock.Foreground = temp >= MonitorLogic.Threshold ? Brushes.Red : Brushes.White;

            history.Add(temp);
            if (history.Count > MaxPoints) history.RemoveAt(0);
            UpdateGraph(history, line);
        }

        private void UpdateGraph(List<float> history, Polyline line)
        {
            if (history.Count < 2) return;
            PointCollection points = new PointCollection();
            double width = line.ActualWidth > 0 ? line.ActualWidth : 160;
            double height = 80;
            double xStep = width / (MaxPoints - 1);
            for (int i = 0; i < history.Count; i++)
                points.Add(new Point(i * xStep, height - ((history[i] - 40) * (height / 60)))); // Range 40-100
            line.Points = points;
        }

        private void ThresholdInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing && float.TryParse(ThresholdInput.Text, out float limit))
            {
                MonitorLogic.Threshold = limit;
                new SettingsRepository().SaveSetting("GpuThreshold", limit.ToString());
            }
        }
    }
}