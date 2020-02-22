using System.Diagnostics;
using System.Windows;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        string strVirtualAccountName = "VirtualAccount";
        public About(string strVATmp)
        {
            InitializeComponent();
            strVirtualAccountName = strVATmp;
            LabelVersion.Content = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
        private void ButtonDonate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=6JQTFNSEJZD9J&source=url");
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        /*
        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            string fileName = System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateClientScript.ps1";
            // Check if file already exists. If yes, delete it.     
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            // Create a new file     
            using (StreamWriter sw = File.CreateText(fileName))
            {
                sw.WriteLine("Install-Module PSWindowsUpdate -Force");
                sw.WriteLine(@"New-PSSessionConfigurationFile -RunAsVirtualAccount -Path .\" + strVirtualAccountName + ".pssc");
                sw.WriteLine(@"Register-PSSessionConfiguration -Name '" + strVirtualAccountName + "' -Path .\\" + strVirtualAccountName + ".pssc -Force");
            }
            Process.Start(fileName);
        }
        */
    }
}
