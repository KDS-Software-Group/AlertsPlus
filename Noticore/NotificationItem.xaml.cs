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
    public partial class NotificationItem : UserControl 
    {
        public NotificationItem(string title, string message)
        {
            InitializeComponent();
            TitleTxt.Text = title;
            MessageTxt.Text = message;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is ListBox))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is ListBox listBox)
            {
                listBox.Items.Remove(this);
            }
        }
    }
}
