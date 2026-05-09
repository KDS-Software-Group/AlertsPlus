using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AlertPlus
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class NotificationWindow : Window
    {
        private bool _stayUntilExit;
        public float NotificationDuration = 12.0f;
        public bool IsImportant { get; set; } = false;

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // ends teh window instance
        }
        public NotificationWindow(string title, string message, bool stayUntilExit)
        {
            InitializeComponent();
            TitleTxt.Text = title;
            MessageTxt.Text = message;
            _stayUntilExit = stayUntilExit;
        }

        public void ShowAndSlide()
        {
            var area = SystemParameters.WorkArea;
            string position = new SettingsRepository().GetSetting("NotificationPosition", "BottomRight");

            double screenRight = area.Left + area.Width;
            double screenLeft = area.Left;

            // Horizontal starting point and resting point
            double restX = position.Contains("Left") ? screenLeft : screenRight - 320;
            double startX = position.Contains("Left") ? screenLeft - 320 : screenRight;

            // Vertical position
            this.Top = position.Contains("Top")
                ? area.Top + 10
                : area.Bottom - this.Height - 10;

            this.Left = startX;
            this.Show();

            DoubleAnimation slide = new DoubleAnimation
            {
                From = startX,
                To = restX,
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.LeftProperty, slide);

            if (!IsImportant && !_stayUntilExit)
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(NotificationDuration) };
                timer.Tick += (s, e) => { SlideOutAndClose(); timer.Stop(); };
                timer.Start();
            }
        }

        public void SlideOutAndClose()
        {
            var area = SystemParameters.WorkArea;
            string position = new SettingsRepository().GetSetting("NotificationPosition", "BottomRight");

            double endX = position.Contains("Left")
                ? area.Left - 400
                : area.Left + area.Width + 400;

            DoubleAnimation slideOut = new DoubleAnimation
            {
                From = this.Left,
                To = endX,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOut.Completed += (s, e) => this.Close();
            this.BeginAnimation(Window.LeftProperty, slideOut);
        }

        public void UpdateMessage(string title, string message)
        {
            TitleTxt.Text = title;
            MessageTxt.Text = message;
        }
    }
}