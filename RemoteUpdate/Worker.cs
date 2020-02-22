using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace RemoteUpdate
{
    class Worker
    {
        public static void CreateBackgroundWorker(int line)
        {
            CreateBackgroundWorkerUptime(line);
            CreateBackgroundWorkerPing(line);
        }
        /// <summary>
        /// Create a new BackgroundWorker for Uptime of the host xyz
        /// </summary>
        /// <param name="trline"></param>
        public static void CreateBackgroundWorkerUptime(int line)
        {
            BackgroundWorker BgWUptimeTmp = new BackgroundWorker();
            BgWUptimeTmp.DoWork += new DoWorkEventHandler((sender, e) => { BackgroundWorkerUptime(line); });
            BgWUptimeTmp.RunWorkerAsync();
            Global.ListBackgroundWorkerUptime.Add(BgWUptimeTmp);

        }
        /// <summary>
        /// Create a new BackgroundWorker for Ping of the host xyz
        /// </summary>
        /// <param name="trline"></param>
        public static void CreateBackgroundWorkerPing(int line)
        {
            BackgroundWorker BgWUptimeTmp = new BackgroundWorker();
            BgWUptimeTmp.DoWork += new DoWorkEventHandler((sender, e) => { BackgroundWorkerPing(line); });
            BgWUptimeTmp.RunWorkerAsync();
            Global.ListBackgroundWorkerPing.Add(BgWUptimeTmp);

        }

        public static void Ping_Tick(int line)
        {
            if (Global.TableRuntime.Rows[line]["IP"].ToString() == "" && Global.TableRuntime.Rows[line]["Servername"].ToString() == "")
            {
                return;
            }
            if (Global.TableRuntime.Rows[line]["IP"].ToString() == "")
            {
                Global.TableRuntime.Rows[line]["Ping"] = "noIP";
                return;
            }
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            PingReply reply = pingSender.Send(Global.TableRuntime.Rows[line]["IP"].ToString(), 1000, buffer, options);
            try
            {
                if (reply.Status == IPStatus.Success)
                {
                    Global.TableRuntime.Rows[line]["Ping"] = "true";
                }
                else
                {
                    Global.TableRuntime.Rows[line]["Ping"] = "false";
                }
            } catch { return;  }
        }
        public static void Uptime_Tick(int line)
        {
            if (Global.TableRuntime.Rows[line]["IP"].ToString() == "") { return; }
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                if (Global.TableRuntime.Rows[line]["Username"].ToString() != "" && Global.TableRuntime.Rows[line]["Username"].ToString() != "")
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + Global.TableRuntime.Rows[line]["Password"].ToString() + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + Global.TableRuntime.Rows[line]["Username"].ToString() + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + Global.TableRuntime.Rows[line]["Servername"].ToString() + "' { (get-date) - (gcim Win32_OperatingSystem).LastBootUpTime };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + Global.TableRuntime.Rows[line]["Servername"].ToString() + "' { (get-date) - (gcim Win32_OperatingSystem).LastBootUpTime };");
                }
                var exResults = pipeline.Invoke();
                try
                {
                    if (exResults.Count != 0)
                    {
                        TimeSpan tstmp = TimeSpan.Parse(exResults[0].BaseObject.ToString());
                        Global.TableRuntime.Rows[line]["Uptime"] = tstmp.Days + "d " + tstmp.Hours + "h " + tstmp.Minutes + "m";
                    }
                    else
                    {
                        Global.TableRuntime.Rows[line]["Uptime"] = "no connection";
                    }
                } catch { }
            }
        }
        public static void BackgroundWorkerUptime(int line)
        {
            System.Threading.Thread.Sleep(10 * 1000);
            while (true)
            {
                Uptime_Tick(line);
                System.Threading.Thread.Sleep(30 * 1000);  // Wait 30 seconds
            }
        }
        public static void BackgroundWorkerPing(int line)
        {
            System.Threading.Thread.Sleep(1 * 1000);
            while (true)
            {
                Ping_Tick(line);
                System.Threading.Thread.Sleep(5 * 1000);  // Wait 5 seconds
            }
        }
        public static void TimerUpdateGrid_Tick(Grid GridMainWindow)
        {
            Global.TimerUpdateGrid.Stop();
            try
            {
                for (int ii = 0; ii < Global.TableRuntime.Rows.Count; ii++)
                {
                    if (Global.TableRuntime.Rows[ii]["Servername"].ToString() == "")
                    {
                        GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
                        GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = "";
                        continue;
                    }
                    if (Global.TableRuntime.Rows[ii]["IP"].ToString() == "")
                    {
                        GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(218, 97, 230)); // 
                        GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = "no IP";
                        continue;
                    }
                    if (Global.TableRuntime.Rows[ii]["Ping"].ToString() == "true")
                    {
                        GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(121, 255, 164)); // #FF79FFA4
                        GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = Global.TableRuntime.Rows[ii]["Uptime"].ToString();
                    }
                    else
                    {
                        GridMainWindow.Children.OfType<TextBox>().Where(tb => tb.Name == "TextBoxServer_" + ii).FirstOrDefault().Background = new SolidColorBrush(Color.FromRgb(240, 139, 139)); // #FFF08B8B
                        GridMainWindow.Children.OfType<Label>().Where(lbl => lbl.Name == "LabelUptime_" + ii).FirstOrDefault().Content = Global.TableRuntime.Rows[ii]["Uptime"].ToString();
                    }
                }
            } catch { }
            Global.TimerUpdateGrid.Start();
        }
    }
}
