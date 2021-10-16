using System.Windows;
using System.Windows.Media;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            if (Global.TableSettings.Rows[0]["SMTPPort"].ToString().Length != 0)
            {
                TextboxSMTPPort.Text = Global.TableSettings.Rows[0]["SMTPPort"].ToString();
            }
            TextboxSMTPServer.Text = Global.TableSettings.Rows[0]["SMTPServer"].ToString(); ;
            TextboxMailFrom.Text = Global.TableSettings.Rows[0]["MailFrom"].ToString();
            TextboxMailTo.Text = Global.TableSettings.Rows[0]["MailTo"].ToString();
            TextboxVirtualAccount.Text = Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString();
            TextboxPSWUCommands.Text = Global.TableSettings.Rows[0]["PSWUCommands"].ToString();
            bool bVerboseLog = bool.TryParse(Global.TableSettings.Rows[0]["VerboseLog"].ToString(), out bool bParse);
            if (bParse)
            {
                CheckboxVerboseLog.IsChecked = bVerboseLog;
            }
        }
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            Global.TableSettings.Rows[0]["SMTPPort"] = TextboxSMTPPort.Text;
            Global.TableSettings.Rows[0]["SMTPServer"] = TextboxSMTPServer.Text;
            Global.TableSettings.Rows[0]["MailFrom"] = TextboxMailFrom.Text;
            Global.TableSettings.Rows[0]["MailTo"] = TextboxMailTo.Text;
            Global.TableSettings.Rows[0]["PSVirtualAccountName"] = TextboxVirtualAccount.Text;
            Global.TableSettings.Rows[0]["PSWUCommands"] = TextboxPSWUCommands.Text;
            Global.TableSettings.Rows[0]["VerboseLog"] = CheckboxVerboseLog.IsChecked.ToString();
            this.Close();
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ButtonSendMail_Click(object sender, RoutedEventArgs e)
        {
            if ((TextboxSMTPServer.Text.Length > 0) && (TextboxSMTPPort.Text.Length > 0) && (TextboxMailFrom.Text.Length > 0) && (TextboxMailTo.Text.Length > 0))
            {
                if (Tasks.SendTestMail(TextboxSMTPServer.Text, TextboxSMTPPort.Text, TextboxMailFrom.Text, TextboxMailTo.Text))
                {
                    ButtonSendMail.Background = new SolidColorBrush(Color.FromRgb(121, 255, 164));
                }
                else
                {
                    ButtonSendMail.Background = new SolidColorBrush(Color.FromRgb(240, 139, 139));
                }
            }
        }
    }
}
