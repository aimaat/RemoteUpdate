using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for Fixit.xaml
    /// </summary>
    public partial class Fixit : Window
    {
        public Fixit()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            if (Tasks.IsAdministrator())
            {
                ImageUAC.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
        private void ButtonFixitCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void ButtonFixitOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
