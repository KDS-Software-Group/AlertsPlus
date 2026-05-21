using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

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
            // clean up fully closed notifications only
            _activeNotifications.RemoveAll(n => !n.IsLoaded);

            // cap at 3 notifications max to prevent flooding
            if (_activeNotifications.Count >= 3) return;

            var area = SystemParameters.WorkArea;
            string position = new SettingsRepository().GetSetting("NotificationPosition", "BottomRight");

            var notification = new NotificationWindow(title, body, critical);
            notification.IsImportant = critical;

            // position based on how many are already showing
            double offset = _activeNotifications.Count * 110;
            notification.Top = position.Contains("Top")
                ? area.Top + 10 + offset
                : area.Bottom - 110 - offset;

            notification.Closed += (s, e) =>
            {
                _activeNotifications.Remove(notification);
                // small delay before restacking so the slide out animation finishes first
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                timer.Tick += (ts, te) => { timer.Stop(); ReStackNotifications(); };
                timer.Start();
            };

            _activeNotifications.Add(notification);
            notification.ShowAndSlide();
        }

        private void ReStackNotifications()
        {
            // remove any that closed during the delay
            _activeNotifications.RemoveAll(n => !n.IsLoaded);

            var area = SystemParameters.WorkArea;
            string position = new SettingsRepository().GetSetting("NotificationPosition", "BottomRight");

            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                double offset = i * 110;
                _activeNotifications[i].Top = position.Contains("Top")
                    ? area.Top + 10 + offset
                    : area.Bottom - 110 - offset;
                _activeNotifications[i].Left = position.Contains("Left")
                    ? area.Left
                    : area.Left + area.Width - 320;
            }
        }
    }
}