using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Net.NetworkInformation;
using System.Text;

namespace RemoteUpdate
{
    class Worker
    {
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
            if (reply.Status == IPStatus.Success)
            {
                Global.TableRuntime.Rows[line]["Ping"] = "true";
            }
            else
            {
                Global.TableRuntime.Rows[line]["Ping"] = "false";
            }
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

                if (exResults.Count != 0)
                {
                    TimeSpan tstmp = TimeSpan.Parse(exResults[0].BaseObject.ToString());
                    Global.TableRuntime.Rows[line]["Uptime"] = tstmp.Days + "d " + tstmp.Hours + "h " + tstmp.Minutes + "m";
                }
                else
                {
                    Global.TableRuntime.Rows[line]["Uptime"] = "no connection";
                }
                psRunspace.Close();
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
    }
}
