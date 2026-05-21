using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlertPlus
{
    public class ScheduledNotification
    {
        public int Id { get; set; }
        public string Title { get; set; } = "Reminder";
        public string Message { get; set; } = "";
        public string Description { get; set; } = "";
        public string ExePath { get; set; } = "";
        public DateTime TargetTime { get; set; }
        public bool IsEnabled { get; set; } = true;

        public string TimeDisplay => TargetTime.ToString("hh:mm tt");
        public string DateDisplay => TargetTime.ToString("MMM d, yyyy");
        public Visibility HasDescription => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasExe => string.IsNullOrWhiteSpace(ExePath) ? Visibility.Collapsed : Visibility.Visible;
    }

    public partial class ViewScheduler : UserControl
    {
        private readonly SettingsRepository _repo = new SettingsRepository();

        public ViewScheduler()
        {
            InitializeComponent();
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            NotificationsControl.ItemsSource = null;
            NotificationsControl.ItemsSource = _repo.GetAllNotifications();
        }

        private void OpenForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void CloseForm_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void FormCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { }

        private void BrowseExe_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Application",
                Filter = "Executables & Scripts|*.exe;*.bat;*.ps1;*.cmd|All Files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
                ExePathInput.Text = dialog.FileName;
        }

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                TitleInput.BorderBrush = System.Windows.Media.Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionInput.Text))
            {
                DescriptionInput.BorderBrush = System.Windows.Media.Brushes.Red;
                return;
            }

            if (!DatePickerHidden.SelectedDate.HasValue)
                return;

            string timePart = $"{HourInput.Text}:{MinuteInput.Text} {(AmPmInput.SelectedItem as ComboBoxItem)?.Content}";
            string fullDateTime = $"{DatePickerHidden.SelectedDate.Value.ToShortDateString()} {timePart}";

            if (!DateTime.TryParse(fullDateTime, out DateTime finalTime))
                return;

            var note = new ScheduledNotification
            {
                Title = TitleInput.Text.Trim(),
                Message = DescriptionInput.Text.Trim(),
                Description = DescriptionInput.Text.Trim(),
                ExePath = ExePathInput.Text.Trim(),
                TargetTime = finalTime,
                IsEnabled = true
            };

            _repo.AddScheduledNotification(note);
            LoadNotifications();
            ModalOverlay.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        private void DeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                _repo.DeleteNotification(id);
                LoadNotifications();
            }
        }

        private void ClearForm()
        {
            TitleInput.Text = "";
            DescriptionInput.Text = "";
            ExePathInput.Text = "";
            HourInput.Text = "12";
            MinuteInput.Text = "00";
            AmPmInput.SelectedIndex = 0;
            DatePickerHidden.SelectedDate = null;

            var defaultBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(45, 45, 48));
            TitleInput.BorderBrush = defaultBrush;
            DescriptionInput.BorderBrush = defaultBrush;
            HourInput.BorderBrush = defaultBrush;
            MinuteInput.BorderBrush = defaultBrush;
        }
    }
}