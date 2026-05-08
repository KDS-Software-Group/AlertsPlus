using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Noticore
{
    public partial class NotificationTray : Window
    {
        public NotificationTray()
        {
            InitializeComponent();

            var area = SystemParameters.WorkArea;

            this.Width = 350;
            this.Height = area.Height;
            this.Left = area.Width - this.Width;
            this.Top = area.Y;
        }

        public void AddNotifcation(string title, string message)
        {
            var item = new NotificationItem(title, message);
            NotificationList.Items.Add(item);
        }


    }
}