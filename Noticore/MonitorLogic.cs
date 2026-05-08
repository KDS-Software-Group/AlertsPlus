using LibreHardwareMonitor.Hardware;
using System;
using System.Diagnostics;
using System.Management;
using System.Windows.Threading;



namespace Noticore
{
    public class MonitorLogic
    {
        private Computer? _mycomputer;
        private DispatcherTimer? _timer;
        // private float _lastCpuWarned = 0; for cpu monitoring later on  
        private float _lastGpuWarned = 0; 
        private NotificationWindow? _currentCpuNotification;
        private NotificationTray _tray = new NotificationTray();
        private SettingsRepository _repo = new SettingsRepository();
        public static float Threshold { get; set; } = 85; // default for gpu temp threshold
        private int _schedulerCheckCounter = 0;

        public float GlobalNotificationDuration { get; set; }
        public float GpuLimit { get; set; }
        public static float CurrentGpuTemp { get; set; }
        public static bool IsEnabled { get; set; } = true;
        public static bool MonitorGpu { get; set; } = true;
        public static bool UseHotspot { get; set; } = false;

        public void Initialize()
        {
            _tray.Show();
            _mycomputer = new Computer() { IsGpuEnabled = true };
            _mycomputer!.Open();

            GlobalNotificationDuration = float.Parse(_repo.GetSetting("Duration", "12"));

            string saved = _repo.GetSetting("GpuThreshold", "80");
            Threshold = float.Parse(saved);

            IsEnabled = bool.Parse(_repo.GetSetting("MasterToggle", "true"));
            MonitorGpu = bool.Parse(_repo.GetSetting("MonitorGpuToggle", "true"));
            UseHotspot = bool.Parse(_repo.GetSetting("HotspotToggle", "false"));

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) => RunCheck();
            _timer.Start();
        }

        public void UpdateDuration(float newValue)
        {
            GlobalNotificationDuration = newValue;
            _repo.SaveSetting("Duration", newValue.ToString());
        }

        public void UpdateGpuThreshold(float newLimit)
        {
            Threshold = newLimit;
            _repo.SaveSetting("GpuLimit", newLimit.ToString());
        }

        private float GetHardwareTemp()
        {
            float max = 0;
            float coreTemp = 0;

            foreach (var hardware in _mycomputer!.Hardware)
            {
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            float currentVal = (float)Math.Round(sensor.Value.Value);

                            if (currentVal > max) max = currentVal;

                            if (sensor.Name.ToLower().Contains("core")) coreTemp = currentVal;
                        }
                    }
                }
            }

            if (UseHotspot) return max;
            return coreTemp > 0 ? coreTemp : max;
        }

        private void TriggerNotification(string title, string body, bool critical = false)
        {
            if (_currentCpuNotification != null && _currentCpuNotification.IsLoaded)
            {
                _currentCpuNotification.UpdateMessage(title, body);
            }
            else
            {
                _currentCpuNotification = new NotificationWindow(title, body, true);

                if (critical)
                {
                    _currentCpuNotification.IsImportant = true;
                }

                _currentCpuNotification.ShowAndSlide();
            }
        }

        private void RunCheck()
        {
            if (!IsEnabled) return;

            float gpu = GetHardwareTemp();
            CurrentGpuTemp = gpu;

            if (MonitorGpu)
            {
                CheckThreshold("GPU", gpu, ref _lastGpuWarned, Threshold);
            }

            _schedulerCheckCounter++;
            if (_schedulerCheckCounter >= 15) // 15 ticks * 2 seconds es 30 seconds si
            {
                CheckScheduledNotifications();
                _schedulerCheckCounter = 0;
            }
        }

        private void CheckScheduledNotifications()
        {
            var repo = new SettingsRepository();
            var allNotifications = repo.GetAllNotifications();

            // see if the thing sees the notes
            Debug.WriteLine($"[Scheduler] Checking {allNotifications.Count} notifications...");

            foreach (var note in allNotifications)
            {
                if (note.IsEnabled && DateTime.Now >= note.TargetTime)
                {
                    Debug.WriteLine($"[Scheduler] TRIGGERING: {note.Title}");
                    TriggerNotification(note.Title, note.Message);

                    note.IsEnabled = false;
                    repo.UpdateNotification(note);
                }
            }
        }

        private void CheckThreshold(string label, float currentTemp, ref float lastWarnedTemperature, float limit)
        {
            if (currentTemp > limit)
            {
                if (currentTemp != lastWarnedTemperature)
                {
                    lastWarnedTemperature = currentTemp;
                    TriggerNotification($"{label} Heat Warning", $"{label} is at {currentTemp}°C.", true);
                }
            }
            else if (currentTemp < limit - 5)
            {
                lastWarnedTemperature = 0;

                if (_currentCpuNotification != null && _currentCpuNotification.IsImportant)
                {
                    _currentCpuNotification.SlideOutAndClose();
                    _currentCpuNotification = null;
                }
            }
        }
    }   
}