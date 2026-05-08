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

namespace Noticore
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
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = screenWidth;
            this.Show();

            DoubleAnimation slide = new DoubleAnimation
            {
                From = screenWidth,
                To = screenWidth - 320,
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.LeftProperty, slide);

            if (!IsImportant && !_stayUntilExit)
            {
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(NotificationDuration)
                };

                timer.Tick += (s, e) => {
                    SlideOutAndClose();
                    timer.Stop();
                };
                timer.Start();
            }
        }

        public void SlideOutAndClose()
        {
            // Get the current position
            double startPos = this.Left;
            double endPos = startPos + 400;

            DoubleAnimation slideAnimation = new DoubleAnimation
            {
                From = startPos,
                To = endPos,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideAnimation.Completed += (s, e) => this.Close();

            this.BeginAnimation(Window.LeftProperty, slideAnimation);
        }

        public void UpdateMessage(string title, string message)
        {
            TitleTxt.Text = title;
            MessageTxt.Text = message;
        }
    }
}