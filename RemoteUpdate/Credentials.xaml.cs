using System.Windows;
using System.Windows.Controls;

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
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            iLabelID = iTmpID;
            if (Global.TableRuntime.Rows[iLabelID]["Password"].ToString().Length != 0)
            {
                PasswordBoxPassword.Password = Global.TableRuntime.Rows[iLabelID]["Password"].ToString();
            }
            if (Global.TableRuntime.Rows[iLabelID]["Username"].ToString().Length != 0)
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
            }
            else
            {
                (sender as PasswordBox).SelectAll();
            }
        }
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            if (TextboxUsername.Text != "Username")
            {
                Tasks.LockAndWriteDataTable(Global.TableRuntime, iLabelID, "Username", TextboxUsername.Text, 100);
                //Global.TableRuntime.Rows[iLabelID]["Username"] = TextboxUsername.Text;
            }
            if (PasswordBoxPassword.Password != "ABC")
            {
                Tasks.LockAndWriteDataTable(Global.TableRuntime, iLabelID, "Password", PasswordBoxPassword.Password, 100);
                //Global.TableRuntime.Rows[iLabelID]["Password"] = PasswordBoxPassword.Password;
            }
            this.Close();
        }
    }
}
