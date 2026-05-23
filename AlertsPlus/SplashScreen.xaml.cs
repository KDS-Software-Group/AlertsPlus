using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AlertsPlus
{
    public partial class SplashScreen : Window
    {
        private readonly Action _onComplete;
        private readonly DispatcherTimer _timer = new();

        public SplashScreen(Action onComplete)
        {
            InitializeComponent();
            _onComplete = onComplete;

            Loaded += (s, e) => BeginFadeIn();
        }

        private void BeginFadeIn()
        {
            var fadeIn = (Storyboard)Resources["FadeIn"];
            fadeIn.Completed += (s, e) => StartHoldTimer();
            fadeIn.Begin();
        }

        private void StartHoldTimer()
        {
            // hold for 3 seconds total minus fade times (0.8 + 0.8 = 1.6s)
            _timer.Interval = TimeSpan.FromSeconds(1.4);
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                BeginFadeOut();
            };
            _timer.Start();
        }

        // fade out the splash screen and close it once the animation is complete
        private void BeginFadeOut()
        {
            var fadeOut = (Storyboard)Resources["FadeOut"];
            fadeOut.Completed += (s, e) =>
            {
                _onComplete?.Invoke();
                this.Close();
            };
            fadeOut.Begin();
        }
    }
}