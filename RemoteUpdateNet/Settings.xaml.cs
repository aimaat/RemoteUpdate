using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RemoteUpdateNet
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings(string strSMTPServer, string strSMTPPort, string strMailFrom, string strMailTo, string strVirtualAccount)
        {
            InitializeComponent();
            if(strSMTPPort != "")
            {
                TextboxSMTPPort.Text = strSMTPPort;
            }
            TextboxSMTPServer.Text = strSMTPServer;
            TextboxMailFrom.Text = strMailFrom;
            TextboxMailTo.Text = strMailTo;
            TextboxVirtualAccount.Text = strVirtualAccount;
        }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
