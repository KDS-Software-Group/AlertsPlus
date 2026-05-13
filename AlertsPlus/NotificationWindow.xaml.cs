using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace AlertPlus
{
    public partial class NotificationWindow : Window
    {
        private bool _stayUntilExit;
        public float NotificationDuration = 12.0f;
        public bool IsImportant { get; set; } = false;
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // ends the window instance
        }

        // IMPORTANT!
        // Call this function in any C# file to create a notifizcation. Input a title, message, and whether it should stay open until exit or close with a timer.
        // Refer to documentation on Github for more information.
        public NotificationWindow(string title, string message, bool stayUntilExit)
        {
            InitializeComponent();
            TitleTxt.Text = title;
            MessageTxt.Text = message;
            _stayUntilExit = stayUntilExit;

            this.ShowInTaskbar = false;
            Loaded += (s, e) =>
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                SetWindowLong(helper.Handle, GWL_EXSTYLE,
                    GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
            };
        }

        // Function that creates the slide in animation for the notification. also sets up a timer to close the notification after a certain amount of time if it's not important and not set to stay until exit.
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

        // Function that slides the notification out and closes it. Called when the close button is clicked, or when the timer runs out.
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
    }
}