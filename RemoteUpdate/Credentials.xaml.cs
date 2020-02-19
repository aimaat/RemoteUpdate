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
        int iLabelID;
        public Credentials(int iTmpID)
        {
            InitializeComponent();
            iLabelID = iTmpID;
            if (Global.TableRuntime.Rows[iLabelID]["Password"].ToString() != "") {
                PasswordBoxPassword.Password = Global.TableRuntime.Rows[iLabelID]["Password"].ToString();
            }
            if (Global.TableRuntime.Rows[iLabelID]["Username"].ToString() != "")
            {
                TextboxUsername.Text = Global.TableRuntime.Rows[iLabelID]["Username"].ToString();
            }
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
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
            if (TextboxUsername.Text != "Username")
            {
                Global.TableRuntime.Rows[iLabelID]["Username"] = TextboxUsername.Text.ToString();
            }
            if (PasswordBoxPassword.Password != "ABC")
            {
                Global.TableRuntime.Rows[iLabelID]["Password"] = PasswordBoxPassword.Password.ToString();
            }
            this.Close();
        }
    }
}
