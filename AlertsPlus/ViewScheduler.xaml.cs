using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AlertPlus
{
    public partial class ViewScheduler : UserControl
    {
        private readonly SettingsRepository _repo = new SettingsRepository();

        // ── Data model ───────────────────────────────────────────────────────────
        // NOTE: SettingsRepository uses `using static AlertPlus.ViewScheduler`
        // so this class must stay public and nested here.

        public class ScheduledNotification
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Description { get; set; } = "";
            public string ExePath { get; set; } = "";
            public DateTime TargetTime { get; set; }
            public bool IsEnabled { get; set; } = true;

            public string DateDisplay => TargetTime.ToString("ddd, MMM d");
            public string TimeDisplay => TargetTime.ToString("h:mm tt");
            public Visibility HasDescription => string.IsNullOrWhiteSpace(Description)
                ? Visibility.Collapsed : Visibility.Visible;
            public Visibility HasExe => string.IsNullOrWhiteSpace(ExePath)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        // ── Constructor ──────────────────────────────────────────────────────────

        public ViewScheduler()
        {
            InitializeComponent();
            LoadNotifications();
        }

        // ── Load list ────────────────────────────────────────────────────────────

        private void LoadNotifications()
        {
            NotificationsControl.ItemsSource = _repo.GetAllNotifications();
        }

        // ── Schedule button ──────────────────────────────────────────────────────

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                MessageBox.Show("Please enter a title.", "AlertPlus",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a date.", "AlertPlus",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(HourInput.Text, out int hour) || hour < 1 || hour > 12)
            {
                MessageBox.Show("Please enter a valid hour (1–12).", "AlertPlus",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MinuteInput.Text, out int minute) || minute < 0 || minute > 59)
            {
                MessageBox.Show("Please enter a valid minute (00–59).", "AlertPlus",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isPm = (AmPmInput.SelectedIndex == 1);
            if (isPm && hour != 12) hour += 12;
            if (!isPm && hour == 12) hour = 0;

            DateTime target = DatePicker.SelectedDate.Value.Date
                .AddHours(hour)
                .AddMinutes(minute);

            _repo.AddScheduledNotification(new ScheduledNotification
            {
                Title       = TitleInput.Text.Trim(),
                Message     = DescriptionInput.Text.Trim(),
                Description = DescriptionInput.Text.Trim(),
                ExePath     = ExePathInput.Text.Trim(),
                TargetTime  = target,
                IsEnabled   = true
            });

            TitleInput.Text         = "";
            DescriptionInput.Text   = "";
            ExePathInput.Text       = "";
            DatePicker.SelectedDate = null;
            HourInput.Text          = "12";
            MinuteInput.Text        = "00";
            AmPmInput.SelectedIndex = 0;

            LoadNotifications();
        }

        // ── Delete button ────────────────────────────────────────────────────────

        private void DeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int id))
            {
                _repo.DeleteNotification(id);
                LoadNotifications();
            }
        }

        // ── Browse exe ───────────────────────────────────────────────────────────

        private void BrowseExe_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title  = "Select an application",
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
                ExePathInput.Text = dialog.FileName;
        }
    }
}