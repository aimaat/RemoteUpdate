using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Net.NetworkInformation;
using System.Text;

namespace RemoteUpdate
{
    class Worker
    {
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
    }
}
