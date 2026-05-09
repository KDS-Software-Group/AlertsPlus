using LibreHardwareMonitor.Hardware;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Threading;

namespace AlertPlus
{
    public class MonitorLogic
    {
        private static bool _isInitialized = false;
        private Computer? _mycomputer;
        private DispatcherTimer? _timer;
        private float _lastCpuWarned = 0;
        private float _lastGpuWarned = 0;
        private DateTime _lastGpuNotificationTime = DateTime.MinValue;
        private DateTime _lastCpuNotificationTime = DateTime.MinValue;
        private static readonly TimeSpan NotificationCooldown = TimeSpan.FromMinutes(5);
        private List<NotificationWindow> _activeNotifications = new List<NotificationWindow>();
        private NotificationTray _tray = new NotificationTray();
        private SettingsRepository _repo = new SettingsRepository();

        public static float Threshold { get; set; } = 85;
        private int _schedulerCheckCounter = 0;
        public float GlobalNotificationDuration { get; set; }
        public static float CurrentGpuTemp { get; set; }
        public static float CurrentCpuTemp { get; set; }
        public static bool IsEnabled { get; set; } = true;
        public static bool MonitorGpu { get; set; } = true;
        public static bool MonitorCpu { get; set; } = true;
        public static bool UseHotspot { get; set; } = false;

        public void Initialize()
        {
            if (_isInitialized) return;

            Application.Current.Dispatcher.Invoke(() => _tray.Show());

            _mycomputer = new Computer() { IsGpuEnabled = true, IsCpuEnabled = true };
            _mycomputer.Open();

            GlobalNotificationDuration = float.Parse(_repo.GetSetting("Duration", "12"));
            Threshold = float.Parse(_repo.GetSetting("GpuThreshold", "80"));
            IsEnabled = bool.Parse(_repo.GetSetting("MasterToggle", "true"));
            MonitorGpu = bool.Parse(_repo.GetSetting("MonitorGpuToggle", "true"));
            MonitorCpu = bool.Parse(_repo.GetSetting("MonitorCpuToggle", "true"));
            UseHotspot = bool.Parse(_repo.GetSetting("HotspotToggle", "false"));

            _isInitialized = true;

            // Run monitoring on background thread so crashes don't kill the app
            Task.Run(() => MonitorLoop());
        }

        private async Task MonitorLoop()
        {
            while (true)
            {
                try
                {
                    if (IsEnabled && _mycomputer != null)
                    {
                        float gpu = 0, cpu = 0;

                        try { gpu = GetGpuTemp(); } catch { }
                        // CPU disabled - LibreHardwareMonitor throws internally on this hardware
                        CurrentGpuTemp = gpu;
                        CurrentCpuTemp = 0;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                if (MonitorGpu) CheckThreshold("GPU", CurrentGpuTemp, ref _lastGpuWarned, ref _lastGpuNotificationTime, Threshold);
                                if (MonitorCpu) CheckThreshold("CPU", CurrentCpuTemp, ref _lastCpuWarned, ref _lastCpuNotificationTime, Threshold);

                                _schedulerCheckCounter++;
                                if (_schedulerCheckCounter >= 15)
                                {
                                    CheckScheduledNotifications();
                                    _schedulerCheckCounter = 0;
                                }
                            }
                            catch { }
                        });
                    }
                }
                catch { }

                await Task.Delay(2000);
            }
        }

        private float GetCpuTemp()
        {
            return 0;
        }

        private float GetGpuTemp()
        {
            float max = 0, coreTemp = 0;
            try
            {
                foreach (var hardware in _mycomputer!.Hardware)
                {
                    if (hardware == null) continue;
                    if (hardware.HardwareType != HardwareType.GpuNvidia &&
                        hardware.HardwareType != HardwareType.GpuAmd) continue;

                    hardware.Update();

                    if (hardware.Sensors == null) continue;

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor?.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            float currentVal = (float)Math.Round(sensor.Value.Value);
                            if (currentVal > max) max = currentVal;
                            if (sensor.Name != null && sensor.Name.ToLower().Contains("core"))
                                coreTemp = currentVal;
                        }
                    }
                }
            }
            catch { return 0; }
            return UseHotspot ? max : (coreTemp > 0 ? coreTemp : max);
        }

        private void RunCheck()
        {
            if (!IsEnabled || _mycomputer == null) return;

            try
            {
                CurrentGpuTemp = GetGpuTemp();
                CurrentCpuTemp = GetCpuTemp();

                if (MonitorGpu) CheckThreshold("GPU", CurrentGpuTemp, ref _lastGpuWarned, ref _lastGpuNotificationTime, Threshold);
                if (MonitorCpu) CheckThreshold("CPU", CurrentCpuTemp, ref _lastCpuWarned, ref _lastCpuNotificationTime, Threshold);

                _schedulerCheckCounter++;
                if (_schedulerCheckCounter >= 15) { CheckScheduledNotifications(); _schedulerCheckCounter = 0; }
            }
            catch (Exception) { }
        }

        private void CheckThreshold(string label, float currentTemp, ref float lastWarnedTemperature, ref DateTime lastNotificationTime, float limit)
        {
            if (currentTemp > limit)
            {
                bool cooledDown = (DateTime.Now - lastNotificationTime) >= NotificationCooldown;

                if (currentTemp != lastWarnedTemperature && cooledDown)
                {
                    lastWarnedTemperature = currentTemp;
                    lastNotificationTime = DateTime.Now;
                    TriggerNotification($"{label} Heat Warning", $"{label} is at {currentTemp}°C.", true);
                }
            }
            else if (currentTemp < limit - 5)
            {
                lastWarnedTemperature = 0;
                lastNotificationTime = DateTime.MinValue; // reset cooldown once temp is safe
            }
        }

        private void TriggerNotification(string title, string body, bool critical = false)
        {
            _activeNotifications.RemoveAll(n => !n.IsVisible);

            var area = System.Windows.SystemParameters.WorkArea;
            double offset = _activeNotifications.Count * 110;

            var notification = new NotificationWindow(title, body, critical);
            notification.IsImportant = critical;
            notification.Top = area.Bottom - 110 - offset;

            notification.Closed += (s, e) =>
            {
                _activeNotifications.Remove(notification);
                ReStackNotifications();
            };

            _activeNotifications.Add(notification);
            notification.ShowAndSlide();
        }

        private void ReStackNotifications()
        {
            var area = System.Windows.SystemParameters.WorkArea;
            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                _activeNotifications[i].Top = area.Bottom - 110 - (i * 110);
                _activeNotifications[i].Left = area.Left + area.Width - 320;
            }
        }

        private void CheckScheduledNotifications()
        {
            try
            {
                var repo = new SettingsRepository();
                foreach (var note in repo.GetAllNotifications())
                {
                    if (note.IsEnabled && DateTime.Now >= note.TargetTime)
                    {
                        TriggerNotification(note.Title, note.Message);
                        note.IsEnabled = false;
                        repo.UpdateNotification(note);
                    }
                }
            }
            catch { }
        }
    }
}