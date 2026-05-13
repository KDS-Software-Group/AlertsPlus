using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Windows;

namespace AlertPlus
{
    public class MonitorLogic
    {
        private static bool _isInitialized = false;
        private Computer? _mycomputer;
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
            
            // liberehardwaremonitor loading and settings loading
            _mycomputer = new Computer() { IsGpuEnabled = true, IsCpuEnabled = true };
            _mycomputer.Open();

            GlobalNotificationDuration = float.Parse(_repo.GetSetting("Duration", "12"));
            Threshold = float.Parse(_repo.GetSetting("GpuThreshold", "80"));
            IsEnabled = bool.Parse(_repo.GetSetting("MasterToggle", "true"));
            MonitorGpu = bool.Parse(_repo.GetSetting("MonitorGpuToggle", "true"));
            MonitorCpu = bool.Parse(_repo.GetSetting("MonitorCpuToggle", "true"));
            UseHotspot = bool.Parse(_repo.GetSetting("HotspotToggle", "false"));

            _isInitialized = true;

            // run monitoring on background thread so crashes don't kill the app
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
                        float gpu = 0;

                        try { gpu = GetGpuTemp(); } catch { }
                        // cpu is disabled because i cant get it working without the entire library crashing on this hardware, even if i never read cpu temps. just return 0 for cpu temp and disable all cpu monitoring features until this can be fixed
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

        private void CheckScheduledNotifications()
        {
            // checks for scheduled notifications and triggers them if their time has come 
            try
            {
                var repo = new SettingsRepository();
                foreach (var note in repo.GetAllNotifications())
                {
                    if (note.IsEnabled && DateTime.Now >= note.TargetTime)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TriggerNotification(note.Title, note.Description);

                            if (!string.IsNullOrWhiteSpace(note.ExePath))
                            {
                                try { Process.Start(new ProcessStartInfo(note.ExePath) { UseShellExecute = true }); }
                                catch { }
                            }
                        });

                        note.IsEnabled = false;
                        repo.UpdateNotification(note);
                    }
                }
            }
            catch { }
        }

        private float GetCpuTemp()
        {
            return 0; // cpu temp is broken so this shall work for now as explained in previous comments
        }

        private float GetGpuTemp()
        {
            float max = 0, coreTemp = 0;

            // gets gpu temp with liberhardwaremonitor
            try
            {
                foreach (var hardware in _mycomputer!.Hardware)
                {
                    if (hardware == null) continue;
                    if (hardware.HardwareType != HardwareType.GpuNvidia &&
                        hardware.HardwareType != HardwareType.GpuAmd && hardware.HardwareType != HardwareType.GpuIntel) continue;

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

        // my old runcheck! Its probably useless but will leave here for redundancy until im sure the new system is stable. It runs on the same timer as the old one did so it should be fine to just call it instead of the new code if i need to revert for some reason
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
                if (_schedulerCheckCounter >= 1) { CheckScheduledNotifications(); _schedulerCheckCounter = 0; }
            }
            catch (Exception) { }
        }

        // checks if the current temp exceeds the limit and if enough time has passed since the last notification before triggering a new one. also resets the warning if the temp drops significantly below the limit
        private void CheckThreshold(string label, float currentTemp, ref float lastWarnedTemperature, ref DateTime lastNotificationTime, float limit)
        {
            if (currentTemp > limit)
            {
                bool cooledDown = (DateTime.Now - lastNotificationTime) >= NotificationCooldown;

                if (cooledDown)
                {
                    lastWarnedTemperature = currentTemp;
                    lastNotificationTime = DateTime.Now;
                    TriggerNotification($"{label} Heat Warning", $"{label} is at {currentTemp}°C.", true);
                }
            }
            else if (currentTemp < limit - 5)
            {
                lastWarnedTemperature = 0;
            }
        }

        // creates and shows a new notification, stacking it above existing ones and removing it from the list when closed

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

        // repositions all active notifications should be called after one is closed to fill gaps and keep them stacked nicely
        private void ReStackNotifications()
        {
            var area = System.Windows.SystemParameters.WorkArea;
            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                _activeNotifications[i].Top = area.Bottom - 110 - (i * 110);
                _activeNotifications[i].Left = area.Left + area.Width - 320;
            }
        }
    }
}