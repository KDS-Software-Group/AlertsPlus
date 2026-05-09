using System.Windows;
using System.Windows.Controls;

namespace AlertPlus
{
    public partial class NotificationTray : Window
    {
        private static NotificationTray? _instance;

        public static NotificationTray Instance
        {
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                    _instance = new NotificationTray();
                return _instance;
            }
        }

        public NotificationTray()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                var area = SystemParameters.WorkArea;
                this.Width = 350;
                this.Height = area.Height;
                this.Left = area.Left + area.Width - this.Width;
                this.Top = area.Top;
            };
        }

        public void AddNotification(string title, string message)
        {
            var item = new NotificationItem(title, message);
            NotificationList.Items.Add(item);

            if (!this.IsVisible)
                this.Show();
        }
    }
}