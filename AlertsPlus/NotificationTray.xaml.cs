using System.Windows;
using System.Runtime.InteropServices;

namespace AlertsPlus
{
    public partial class NotificationTray : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private static NotificationTray? _instance;

        public static NotificationTray Instance
        {
            // ensure the one and only instance of the notification tray exists
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                    _instance = new NotificationTray();
                return _instance;
            }
        }

        // constructor that sets up the notification tray window and positions it
        public NotificationTray()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;

            this.Loaded += (s, e) =>
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                helper.EnsureHandle();
                SetWindowLong(helper.Handle, GWL_EXSTYLE,
                GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
                var area = SystemParameters.WorkArea;
                this.Width = 350;
                this.Height = area.Height;
                this.Left = area.Left + area.Width - this.Width;
                this.Top = area.Top;
            };
        }

        // method to add a notification to the tray and show the tray if it's not already visible
        public void AddNotification(string title, string message)
        {
            var item = new NotificationItem(title, message);
            NotificationList.Items.Add(item);

            if (!this.IsVisible)
                this.Show();
        }
    }
}