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

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for Credentials.xaml
    /// </summary>
    public partial class Credentials : Window
    {
        string strLabelID;
        Grid MainGrid;
        public Credentials(object sender, string tmpUserName, string tmpPassword)
        {
            InitializeComponent();
            this.
            MainGrid = ((FrameworkElement)sender).Parent as Grid;
            strLabelID = (sender as Button).Name.Split('_')[1];
            if (tmpPassword != "") {
                PasswordBoxPassword.Password = tmpPassword;
            }
            if (tmpUserName != "")
            {
                TextboxUsername.Text = tmpUserName;
            }
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
        private void UsernameGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender.GetType().Name == "TextBox")
            {
                (sender as TextBox).SelectAll();
            } else
            {
                (sender as PasswordBox).SelectAll();
            }
        }
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
