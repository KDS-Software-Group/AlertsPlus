using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AlertPlus
{
    public class ScheduledNotification
    {
        public int Id { get; set; }
        public string Title { get; set; } = "Reminder";
        public string Message { get; set; } = "";
        public DateTime TargetTime { get; set; }
        public bool IsEnabled { get; set; } = true;

        public string TimeDisplay => TargetTime.ToString("hh:mm tt");
        public string DateDisplay => TargetTime.ToShortDateString();
    }

    public partial class ViewScheduler : UserControl
    {
        private readonly SettingsRepository _notificationRepository = new SettingsRepository();

        public ViewScheduler()
        {
            InitializeComponent();
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            List<ScheduledNotification> notifications = _notificationRepository.GetAllNotifications();
            NotificationsControl.ItemsSource = notifications;
        }

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                string timePart = $"{HourInput.Text}:{MinuteInput.Text} {(AmPmInput.SelectedItem as ComboBoxItem)?.Content}";
                string fullDateTime = $"{DatePicker.SelectedDate.Value.ToShortDateString()} {timePart}";

                if (DateTime.TryParse(fullDateTime, out DateTime finalTime))
                {
                    var note = new ScheduledNotification
                    {
                        Title = TitleInput.Text,
                        TargetTime = finalTime,
                        IsEnabled = true
                    };
                    _notificationRepository.AddScheduledNotification(note);
                    LoadNotifications();
                }
            }
        }

        private void DeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                _notificationRepository.DeleteNotification(id);
                LoadNotifications();
            }
        }
    }
}