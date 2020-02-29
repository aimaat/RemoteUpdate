using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Windows.Controls;
using System.Xml;

namespace RemoteUpdate
{
    class Tasks
    {
        public static string Encrypt(string clearText)
        {
            if (clearText.Length == 0) { return clearText; }
            string EncryptionKey = System.Net.Dns.GetHostEntry("localhost").HostName + "RemoteUpdateByAIMA" + System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString();
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
                pdb.Dispose();
            }
            return clearText;
        }
        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = System.Net.Dns.GetHostEntry("localhost").HostName + "RemoteUpdateByAIMA" + System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString();
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
                pdb.Dispose();
            }
            return cipherText;
        }
        public static bool CheckPSConnection(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();

                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " -ConfigurationName VirtualAccount { Get-WUApiVersion | Select-Object PSWindowsUpdate };");
                } else {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " -ConfigurationName VirtualAccount { Get-WUApiVersion | Select-Object PSWindowsUpdate };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if(!pipeline.HadErrors)
                    {
                        WriteLogFile(0, "Connection to " + tmpServername.ToUpper(Global.cultures) + " works");
                        returnValue = true;
                    } else
                    {
                        WriteLogFile(1, "Connection to " + tmpServername.ToUpper(Global.cultures) + " is not working");
                        returnValue = false;
                    }
                } catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Connection to " + tmpServername.ToUpper(Global.cultures) + " failed: " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CreatePSConnectionPrerequisites(int line, out string strFailureMessage)
        {
            // Check Credentials with a single Connection first
            if(!CheckPSConnectionPrerequisiteCredentials(line))
            {
                strFailureMessage = "Credentials";
                return false;
            }
            // Check if Package Provider NuGet is installed
            if(!CheckPSConnectionPrerequisiteProvider(line))
            {
                // Install Package Provider NuGet if not installed
                if(!CreatePSConnectionPrerequisiteProvider(line))
                {
                    strFailureMessage = "PackageProvider";
                    return false;
                }
            }
            // Check if Powershell Module PSWindowsUpdate is installed
            if (!CheckPSConnectionPrerequisiteModule(line))
            {
                // Install Powershell Module PSWindowsUpdate
                if (!CreatePSConnectionPrerequisiteModule(line))
                {
                    strFailureMessage = "PSWindowsUpdate";
                    return false;
                }
            }
            // Check if Powershell Module PSWindowsUpdate is installed
            if (!CheckPSConnectionPrerequisiteVirtualAccount(line))
            {
                // Install Powershell Module PSWindowsUpdate
                if (!CreatePSConnectionPrerequisiteVirtualAccount(line))
                {
                    strFailureMessage = "VirtualAccount";
                    return false;
                }
            }
            strFailureMessage = "OK";
            return true;
        }
        public static bool CheckPSConnectionPrerequisiteCredentials(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (!pipeline.HadErrors)
                    {
                        WriteLogFile(0, "Credentials respectively access rights are ok for " + tmpServername.ToUpper(Global.cultures));
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Wrong credentials or access rights for " + tmpServername.ToUpper(Global.cultures));
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Wrong credentials or access rights for " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CheckPSConnectionPrerequisiteProvider(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpCommand = "Get-PackageProvider | Where {  $_.Name -eq 'NuGet' -and $_.Version -ge [version]('2.8.5.201') }";
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (exResults.Count > 0)
                    {
                        WriteLogFile(0, "Package Provider NuGet is installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Package Provider NuGet is not installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Package Provider NuGet is not installed on " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CreatePSConnectionPrerequisiteProvider(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpCommand = "Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force";
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { " + tmpCommand + " }");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (!pipeline.HadErrors)
                    {
                        WriteLogFile(0, "Package Provider NuGet got installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Package Provider NuGet could not be installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Package Provider NuGet could not be installed on " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CheckPSConnectionPrerequisiteModule(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpCommand = "Get-Module -ListAvailable -Name PSWindowsUpdate  | Where { $_.Version -ge [version]('2.1.1.2') }";
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (exResults.Count > 0)
                    {
                        WriteLogFile(0, "Module PSWindowsUpdate is installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Module PSWindowsUpdate is not installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Module PSWindowsUpdate is not installed on " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CreatePSConnectionPrerequisiteModule(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpCommand = "Install-Module PSWindowsUpdate -MinimumVersion 2.1.1.2 -Force";
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { " + tmpCommand + " }");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (!pipeline.HadErrors)
                    {
                        WriteLogFile(0, "Module PSWindowsUpdate got successfully installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Module PSWindowsUpdate could not be installed on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Module PSWindowsUpdate could not be installed on " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CheckPSConnectionPrerequisiteVirtualAccount(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpVirtualAccount = Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString();
                string tmpCommand = "";
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (exResults.Count > 0)
                    {
                        WriteLogFile(0, "Powershell VirtualAccount \"" + tmpVirtualAccount + "\" exists on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Powershell VirtualAccount \"" + tmpVirtualAccount + "\" does not exist on " + tmpServername.ToUpper(Global.cultures));
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Powershell VirtualAccount \"" + tmpVirtualAccount + "\" does not exist on " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static bool CreatePSConnectionPrerequisiteVirtualAccount(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            bool returnValue = false;
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpVirtualAccount = Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString();
                string tmpCommand = "New-PSSessionConfigurationFile -RunAsVirtualAccount -Path .\\" + tmpVirtualAccount + ".pssc; Register-PSSessionConfiguration -Name '" + tmpVirtualAccount + "' -Path .\\" + tmpVirtualAccount + ".pssc -Force;";
                if (tmpUsername.Length != 0 && tmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + tmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + tmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName " + tmpServername + " { " + tmpCommand + " };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " { " + tmpCommand + " }");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    WriteLogFile(0, "Powershell VirtualAccount \"" + tmpVirtualAccount + "\" was created on " + tmpServername.ToUpper(Global.cultures));
                    returnValue = true;
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "Powershell VirtualAccount \"" + tmpVirtualAccount + "\" could not be created on " + tmpServername.ToUpper(Global.cultures) + ": " + ee.Message);
                    returnValue = false;
                }
            }
            return returnValue;
        }
        public static void OpenPowerShell(int line, Grid GridMainWindow)
        {
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Visible;
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonTime_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Content = DateTime.Now.ToString("HH:mm:ss", Global.cultures);
            GridMainWindow.Children.OfType<Button>().Where(btn => btn.Name.Equals("ButtonStart_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().Visibility = System.Windows.Visibility.Hidden;
            string strTmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper(Global.cultures);
            string strTmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
            string strTmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
            string strTmpCredentials = "";
            string strTmpVirtualAccount = Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString();
                ProcessStartInfo startInfo = new ProcessStartInfo(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");
                startInfo.UseShellExecute = false;
                startInfo.EnvironmentVariables.Add("RedirectStandardOutput", "true");
                startInfo.EnvironmentVariables.Add("RedirectStandardError", "true");
                startInfo.EnvironmentVariables.Add("UseShellExecute", "false");
                startInfo.EnvironmentVariables.Add("CreateNoWindow", "false");
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name.Equals("CheckboxGUI_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().IsChecked)
                {
                    startInfo.Arguments = "-noexit ";
                } else {
                    startInfo.Arguments = "-WindowStyle Hidden ";
                }
                // https://devblogs.microsoft.com/scripting/how-can-i-expand-the-width-of-the-windows-powershell-console/ entfern: $newsize.width = 120; 
                startInfo.Arguments += "$pshost = get-host; $pswindow = $pshost.ui.rawui; $newsize = $pswindow.buffersize; $newsize.height = 10; $pswindow.windowsize = $newsize; $pswindow.buffersize = $newsize;";
                startInfo.Arguments += "$host.ui.RawUI.WindowTitle = '" + strTmpServername + "';";
                if (strTmpUsername.Length != 0 && strTmpPassword.Length != 0)
                {
                    startInfo.Arguments += "$pass = ConvertTo-SecureString -AsPlainText '" + strTmpPassword + "' -Force;";
                    startInfo.Arguments += "$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + strTmpUsername + "',$pass;";
                    strTmpCredentials = "-Credential $Cred ";
                }
                string WUArguments = "";
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxAccept_" + line.ToString(Global.cultures)).FirstOrDefault().IsChecked)
                {
                    WUArguments += "-AcceptAll ";
                }
                if (!(bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxDrivers_" + line.ToString(Global.cultures)).FirstOrDefault().IsChecked)
                {
                    WUArguments += "-NotCategory Drivers ";
                }
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxReboot_" + line.ToString(Global.cultures)).FirstOrDefault().IsChecked)
                {
                    WUArguments += "-AutoReboot ";
                }
                if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name == "CheckboxMail_" + line.ToString(Global.cultures)).FirstOrDefault().IsChecked)
                {
                string strTmpSMTPServer = Global.TableSettings.Rows[0]["SMTPServer"].ToString();
                string strTmpSMTPPort = Global.TableSettings.Rows[0]["SMTPPort"].ToString();
                string strTmpMailFrom = Global.TableSettings.Rows[0]["MailFrom"].ToString();
                string strTmpMailTo = Global.TableSettings.Rows[0]["MailTo"].ToString();
                    if ((strTmpSMTPServer.Length != 0) && (strTmpSMTPPort.Length != 0) && (strTmpMailFrom.Length != 0) && (strTmpMailTo.Length != 0))
                    {
                        WUArguments += "-SendReport –PSWUSettings @{ SmtpServer = '" + strTmpSMTPServer + "'; Port = " + strTmpSMTPPort + "; From = '" + strTmpMailFrom + "'; To = '" + strTmpMailTo + "' }";
                    }
                }
                startInfo.Arguments += "Invoke-Command " + strTmpCredentials + "-ConfigurationName '" + strTmpVirtualAccount + "' -ComputerName " + strTmpServername + " { Install-WindowsUpdate -Verbose " + WUArguments + Global.TableSettings.Rows[0]["PSWUCommands"].ToString() + "}";
                Process test = Process.Start(startInfo);
                WriteLogFile(0, "Update startet on Server " + strTmpServername.ToUpper(Global.cultures));
        }
        public static string GetIPfromHostname(string Servername)
        {
            if (Servername.Length == 0) { return ""; }
            try
            {

                IPHostEntry hostEntry = Dns.GetHostEntry(Servername);
                if (IsLocalHost(hostEntry)) 
                {
                    WriteLogFile(0, "Returned IP 127.0.0.1 for " + Servername.ToUpper(Global.cultures) + " (localhost)", true);
                    return "127.0.0.1"; 
                }
                WriteLogFile(0, "Returned IP " + hostEntry.AddressList.FirstOrDefault().ToString() + " for " + Servername.ToUpper(Global.cultures), true); 
                return hostEntry.AddressList.FirstOrDefault().ToString();
            }
            catch (Exception ee)
            {
                WriteLogFile(1, "Could not get IP for Servername: " + Servername + ": " + ee.Message);
                return "";
            }
        }
        public static bool IsLocalHost(IPHostEntry ipAddress)
        {
            return Global.localHost.AddressList.Any(x => ipAddress.AddressList.Any(y => x.Equals(y)));
        }
        public static bool ReadXMLToTable(string xmlFilename, System.Data.DataTable loadTable)
        {
            if(File.Exists(xmlFilename))
            {
                try
                {
                    loadTable.ReadXmlSchema(xmlFilename);
                    loadTable.ReadXml(xmlFilename);
                    WriteLogFile(0, xmlFilename + " successfully loaded");
                    return true;
                }
                catch (XmlException ee)
                {
                    WriteLogFile(2, xmlFilename + " not imported:" + ee.Message.ToString(Global.cultures)) ;
                    return false;
                }
            }
            WriteLogFile(1, xmlFilename + " not found");
            return false;
        }
        public static bool WriteTableToXML(System.Data.DataTable saveTable, string xmlFilename)
        {
            saveTable.WriteXml(xmlFilename, System.Data.XmlWriteMode.WriteSchema);
            WriteLogFile(0, xmlFilename + " successfully saved");
            return true;
        }
        public static bool CreateLogFile()
        {
            try
            {
                // Check Directory if there are write rights
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(System.AppDomain.CurrentDomain.BaseDirectory);
                // Create Logfile
                using (Global.streamLogFile = File.AppendText(Global.strLogFile))
                {
                    Global.streamLogFile.WriteLine(System.DateTime.Now.ToString("yyyy.MM.dd_hhmmss", Global.cultures) + Global.stringTab + "INFO" + Global.stringTab + "Logfile created");
                }
                // Set Global variable that 
                Global.bDirectoryWritable = true;
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Global.bDirectoryWritable = false;
                return false;
            }
        }
        public static void WriteLogFile(int iClassification, string strArgument)
        {
            WriteLogFile(iClassification, strArgument, false);
        }
        public static void WriteLogFile(int iClassification, string strArgument, bool bVerbose)
        {
            // Return if Global Verbose is false and the LogMessage is indicated as Verbose Log
            if(!Global.bVerboseLog && bVerbose) { return; }
            if(Global.bDirectoryWritable)
            {
                // Create Classification string
                string strClassification;
                if(iClassification == 0) {
                    strClassification = "INFO";
                } else if(iClassification == 1) {
                    strClassification = "WARNING";
                } else if(iClassification == 2) {
                    strClassification = "ERROR";
                } else {
                    strClassification = "UNKNOWN";
                }
                // Write Log Entry
                using (Global.streamLogFile = File.AppendText(Global.strLogFile))
                {
                    Global.streamLogFile.WriteLine(System.DateTime.Now.ToString("yyyy.MM.dd_hhmmss", Global.cultures) + Global.stringTab + strClassification + Global.stringTab + strArgument);
                }
            }
        }
        public static string CheckServiceStatus (string strServiceName) {
            using (ServiceController sc = new ServiceController(strServiceName)) { 
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        return "Running";
                    case ServiceControllerStatus.Stopped:
                        return "Stopped";
                    case ServiceControllerStatus.Paused:
                        return "Paused";
                    case ServiceControllerStatus.StopPending:
                        return "Stopping";
                    case ServiceControllerStatus.StartPending:
                        return "Starting";
                    default:
                        return "Status Changing";
                }
            }
        }
        public static bool CheckWinRMStatus(out string strMessage)
        {
            strMessage = "";
            if(CheckServiceStatus("WinRM") != "Running")
            {
                strMessage = "WinRM Service is not running!";
                return false;
            }
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                pipeline.Commands.AddScript(@"(get-item wsman:\localhost\Client\TrustedHosts).value");
                try
                {
                    var exResults = pipeline.Invoke();
                    if (exResults.Count > 0)
                    {
                        if(!exResults[0].ToString().Contains("*"))
                        {
                            strMessage = "The following hosts are in the TrustedHosts list: " + exResults[0].ToString();
                            return false;
                        }
                    } else
                    {
                        strMessage = "No hosts are in the TrustedHosts list.";
                        return false;
                    }
                }
                catch (PSSnapInException ee)
                {
                    WriteLogFile(2, "An error occured while retrieving the WinRM TrustedHosts list.");
                    strMessage = "An error occured while retrieving the WinRM TrustedHosts list.";
                    return false;
                }
            }
            strMessage = "WinRM service is running and * is in the TrustedHosts list.";
            return true;
        }
    }
}