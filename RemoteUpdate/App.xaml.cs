using System.Windows;

namespace RemoteUpdate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string[] Args;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                bool bExit = false;
                Args = e.Args;
                for (int ii = 0; ii < Args.Length; ii++)
                {
                    switch (Args[ii].ToString(System.Globalization.CultureInfo.CurrentCulture))
                    {
                        case "WinRMService":
                            Tasks.SetServiceStartup("winrm", "auto");
                            bExit = true;
                            continue;
                        case "WinRMStart":
                            Tasks.StartService("winrm");
                            bExit = true;
                            continue;
                        case "TrustedHosts":
                            Tasks.SetTrustedHosts("*");
                            bExit = true;
                            continue;
                    }
                }
                if (bExit)
                {
                    System.Environment.Exit(0);
                }
            }
        }
    }
}
