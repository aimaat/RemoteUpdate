using System.Windows;

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
            if(Global.TableSettings.Rows[0]["SMTPPort"].ToString().Length != 0)
            {
                TextboxSMTPPort.Text = Global.TableSettings.Rows[0]["SMTPPort"].ToString();
            }
            TextboxSMTPServer.Text = Global.TableSettings.Rows[0]["SMTPServer"].ToString(); ;
            TextboxMailFrom.Text = Global.TableSettings.Rows[0]["MailFrom"].ToString();
            TextboxMailTo.Text = Global.TableSettings.Rows[0]["MailTo"].ToString();
            TextboxVirtualAccount.Text = Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString();
            TextboxPSWUCommands.Text = Global.TableSettings.Rows[0]["PSWUCommands"].ToString();
        }
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            Global.TableSettings.Rows[0]["SMTPPort"] = TextboxSMTPPort.Text;
            Global.TableSettings.Rows[0]["SMTPServer"] = TextboxSMTPServer.Text;
            Global.TableSettings.Rows[0]["MailFrom"] = TextboxMailFrom.Text;
            Global.TableSettings.Rows[0]["MailTo"] = TextboxMailTo.Text;
            Global.TableSettings.Rows[0]["PSVirtualAccountName"] = TextboxVirtualAccount.Text;
            Global.TableSettings.Rows[0]["PSWUCommands"] = TextboxPSWUCommands.Text;
            this.Close();
        }
        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
