using System.Windows;

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
        private void WinRMTrustedHosts_Checked(object sender, RoutedEventArgs e)
        {
            WinRMServiceStart.IsChecked = true;
            WinRMServiceStart.IsEnabled = false;
        }
        private void WinRMTrustedHosts_Unchecked(object sender, RoutedEventArgs e)
        {
            WinRMServiceStart.IsEnabled = true;
        }
    }
}
