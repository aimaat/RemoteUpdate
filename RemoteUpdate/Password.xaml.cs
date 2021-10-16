using System.Windows;
using System.Windows.Controls;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for Password.xaml
    /// </summary>
    public partial class Password : Window
    {
        public Password(bool bEncrypt)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (bEncrypt)
            {
                Owner = Application.Current.MainWindow;
            }
            else
            {
                this.Title = "Decryption Password";
            }
        }
        private void PasswordGotFocus(object sender, RoutedEventArgs e)
        {
            (sender as PasswordBox).SelectAll();
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
