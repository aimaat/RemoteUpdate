using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Xml;

namespace RemoteUpdate
{
    class Tasks
    {
        public static string Encrypt(string clearText, string strServername, string EncryptionKey)
        {
            try
            {
                string strSalt = strServername;
                if (clearText.Length == 0) { return clearText; }
                //EncryptionKey = System.Net.Dns.GetHostEntry("localhost").HostName + "RemoteUpdateByAIMA" + System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString();
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (Aes encryptor = Aes.Create())
                {
                    while (strSalt.Length < 16)
                    {
                        strSalt += strSalt;
                    }
                    byte[] salt = Encoding.ASCII.GetBytes(strSalt);
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt, 1000, HashAlgorithmName.SHA512);
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
            catch (Exception ee)
            {
                WriteLogFile(2, "Encrypt error for server " + strServername.ToUpper(Global.cultures) + ": " + ee.Message);
                return "";
            }
        }
        public static string Decrypt(string cipherText, string strServername)
        {
            try
            {
                string EncryptionKey = Global.strDecryptionPassword;
                string strSalt = strServername;
                //EncryptionKey = System.Net.Dns.GetHostEntry("localhost").HostName + "RemoteUpdateByAIMA" + System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString();
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    while (strSalt.Length < 16)
                    {
                        strSalt += strSalt;
                    }
                    byte[] salt = Encoding.ASCII.GetBytes(strSalt);
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, salt, 1000, HashAlgorithmName.SHA512);
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
            catch (Exception ee)
            {
                WriteLogFile(2, "Decrypt error for server " + strServername.ToUpper(Global.cultures) + ": " + ee.Message);
                return "";
            }

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
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName " + tmpServername + " -ConfigurationName VirtualAccount { Get-WUApiVersion | Select-Object PSWindowsUpdate };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (!pipeline.HadErrors)
                    {
                        WriteLogFile(0, "Connection to " + tmpServername.ToUpper(Global.cultures) + " works");
                        returnValue = true;
                    }
                    else
                    {
                        WriteLogFile(1, "Connection to " + tmpServername.ToUpper(Global.cultures) + " is not working");
                        returnValue = false;
                    }
                }
                catch (PSSnapInException ee)
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
            if (!CheckPSConnectionPrerequisiteCredentials(line))
            {
                strFailureMessage = "Credentials";
                return false;
            }
            // Check if Package Provider NuGet is installed
            if (!CheckPSConnectionPrerequisiteProvider(line))
            {
                // Install Package Provider NuGet if not installed
                if (!CreatePSConnectionPrerequisiteProvider(line))
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
                string tmpCommand = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force";
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
                string tmpCommand = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Install-Module PSWindowsUpdate -MinimumVersion 2.1.1.2 -Force";
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
        public static void OpenPowerShellScript(int line, Grid GridMainWindow, string strScriptBlock)
        {
            string strTmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString().ToUpper(Global.cultures);
            string strTmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
            string strTmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
            string strTmpCredentials = "";
            ProcessStartInfo startInfo = new ProcessStartInfo(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");
            startInfo.UseShellExecute = false;
            startInfo.EnvironmentVariables.Add("RedirectStandardOutput", "true");
            startInfo.EnvironmentVariables.Add("RedirectStandardError", "true");
            startInfo.EnvironmentVariables.Add("UseShellExecute", "false");
            startInfo.EnvironmentVariables.Add("CreateNoWindow", "false");
            if ((bool)GridMainWindow.Children.OfType<CheckBox>().Where(cb => cb.Name.Equals("CheckboxGUI_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault().IsChecked)
            {
                startInfo.Arguments = "-noexit ";
            }
            else
            {
                startInfo.Arguments = "-WindowStyle Hidden ";
            }
            startInfo.Arguments += "$pshost = get-host; $pswindow = $pshost.ui.rawui; $newsize = $pswindow.buffersize; $newsize.height = 10; $pswindow.windowsize = $newsize; "; //$pswindow.buffersize = $newsize;
            startInfo.Arguments += "$host.ui.RawUI.WindowTitle = '" + strTmpServername + "';";
            if (strTmpUsername.Length != 0 && strTmpPassword.Length != 0)
            {
                startInfo.Arguments += "$pass = ConvertTo-SecureString -AsPlainText '" + strTmpPassword + "' -Force;";
                startInfo.Arguments += "$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + strTmpUsername + "',$pass;";
                strTmpCredentials = "-Credential $Cred ";
            }
            //string strScriptBlock = "$env:computername; Get-DnsClient; ipconfig";
            startInfo.Arguments += "Invoke-Command " + strTmpCredentials + " -ComputerName " + strTmpServername + " { " + strScriptBlock + " }";
            //startInfo.Arguments += "Invoke-Command " + strTmpCredentials + "-ConfigurationName '" + strTmpVirtualAccount + "' -ComputerName " + strTmpServername + " { Install-WindowsUpdate -Verbose " + WUArguments + Global.TableSettings.Rows[0]["PSWUCommands"].ToString() + " }";
            Process tmpProcess = Process.Start(startInfo);
            if (tmpProcess.Id.ToString(Global.cultures).Length > 0)
            {
                WriteLogFile(0, "Powershell started on Server " + strTmpServername.ToUpper(Global.cultures));
            }
            else
            {
                WriteLogFile(2, "Powershell for Server " + strTmpServername.ToUpper(Global.cultures) + " could not be opened");
            }
        }
        public static void OpenPowerShellUpdate(int line, Grid GridMainWindow)
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
                //startInfo.Arguments = "-noexit ";
            }
            else
            {
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
            //startInfo.Arguments += "Invoke-Command " + strTmpCredentials + "-ConfigurationName '" + strTmpVirtualAccount + "' -ComputerName " + strTmpServername + " { Install-WindowsUpdate -Verbose " + WUArguments + Global.TableSettings.Rows[0]["PSWUCommands"].ToString() + " }";

            string strWUArgument = "Install-WindowsUpdate -Verbose " + WUArguments + Global.TableSettings.Rows[0]["PSWUCommands"].ToString();
            startInfo.Arguments += "Invoke-Command " + strTmpCredentials + "-ConfigurationName '" + strTmpVirtualAccount + "' -ComputerName " + strTmpServername + " { " + strWUArgument + " }";

            Process tmpProcess = Process.Start(startInfo);
            if (tmpProcess.Id.ToString(Global.cultures).Length > 0)
            {
                LockAndWriteDataTable(Global.TableRuntime, line, "PID", tmpProcess.Id.ToString(Global.cultures), 100);
                WriteLogFile(0, "Update started on Server " + strTmpServername.ToUpper(Global.cultures) + " with the command: " + strWUArgument);
                UpdateStatusGUI(line, "progress", GridMainWindow);
            }
            else
            {
                LockAndWriteDataTable(Global.TableRuntime, line, "PID", "error", 100);
                WriteLogFile(2, "Process for Server " + strTmpServername.ToUpper(Global.cultures) + " could not be opened");
                UpdateStatusGUI(line, "error", GridMainWindow);
            }
        }
        public static void AskPendingStatus(int line, Grid GridMainWindow)
        {
            string strScriptBlock = @"
            $scriptBlock = {
function Test-RegistryKey {
    param
    (
        [string]$Key
    )
    $ErrorActionPreference = 'Stop'
    if (Get-Item -Path $Key -ErrorAction Ignore) {
        $true
    }
}
function Test-RegistryValue {
    param
    (
        [string]$Key,
        [string]$Value
    )
    $ErrorActionPreference = 'Stop'
    if (Get-ItemProperty -Path $Key -Name $Value -ErrorAction Ignore) {
        $true
    }
}
function Test-RegistryValueNotNull {
    param
    (
        [string]$Key,
        [string]$Value
    )
    $ErrorActionPreference = 'Stop'
    if (($regVal = Get-ItemProperty -Path $Key -Name $Value -ErrorAction Ignore) -and $regVal.($Value)) {
        $true
    }
}
$tests = @(
    { Test-RegistryKey -Key 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending' }
    { Test-RegistryKey -Key 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootInProgress' }
    { Test-RegistryKey -Key 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' }
    { Test-RegistryKey -Key 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Component Based Servicing\PackagesPending' }
    { Test-RegistryKey -Key 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\PostRebootReporting' }
    { Test-RegistryValueNotNull -Key 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations' }
    { Test-RegistryValueNotNull -Key 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations2' }
    { 
        'HKLM:\SOFTWARE\Microsoft\Updates' | ?{ test-path $_ -PathType Container } | %{            
            (Get-ItemProperty -Path $_ -Name 'UpdateExeVolatile' | Select-Object -ExpandProperty UpdateExeVolatile) -ne 0 
        }
    }
    { Test-RegistryValue -Key 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Value 'DVDRebootSignal' }
    { Test-RegistryKey -Key 'HKLM:\SOFTWARE\Microsoft\ServerManager\CurrentRebootAttemps' }
    { Test-RegistryValue -Key 'HKLM:\SYSTEM\CurrentControlSet\Services\Netlogon' -Value 'JoinDomain' }
    { Test-RegistryValue -Key 'HKLM:\SYSTEM\CurrentControlSet\Services\Netlogon' -Value 'AvoidSpnSet' }
    {
        ( 'HKLM:\SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName' | ?{ test-path $_ } | %{ (Get-ItemProperty -Path $_ ).ComputerName } ) -ne 
        ( 'HKLM:\SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName' | ?{ test-path $_ } | %{ (Get-ItemProperty -Path $_ ).ComputerName } )
    }
    {
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Services\Pending' | Where-Object { 
            (Test-Path $_) -and (Get-ChildItem -Path $_) } | ForEach-Object { $true }
    }
)
foreach ($test in $tests) {
    if (& $test) {
        return 'REBOOT'
    }
}
return 'OK'
}
            ";

            if (Global.TableRuntime.Rows[line]["IP"].ToString().Length == 0) { return; }
            string strTmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
            string strTmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
            string strTmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                // strScriptBlock = "$scriptBlock = { $env:COMPUTERNAME }";
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                pipeline.Commands.AddScript(strScriptBlock);
                if (strTmpUsername.Length != 0 && strTmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + strTmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + strTmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + strTmpServername + "' -ScriptBlock $ScriptBlock;");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + strTmpServername + "' -ScriptBlock $ScriptBlock;");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if (exResults.Count == 1)
                    {
                        if (exResults[0].ToString().ToUpper(Global.cultures) == "REBOOT")
                        {
                            UpdateStatusGUI(line, "pending", GridMainWindow);
                            WriteLogFile(0, "Server " + strTmpServername.ToUpper(Global.cultures) + " has a reboot pending");
                        }
                        else
                        {
                            UpdateStatusGUI(line, "clear", GridMainWindow);
                            WriteLogFile(0, "Server " + strTmpServername.ToUpper(Global.cultures) + " has no reboot pending", true);
                        }
                    }
                    else
                    {
                        UpdateStatusGUI(line, "error", GridMainWindow);
                        WriteLogFile(2, "Too many results at reboot pending status for server " + strTmpServername.ToUpper(Global.cultures));
                    }
                }
                catch (InvalidPipelineStateException ee)
                {
                    UpdateStatusGUI(line, "error", GridMainWindow);
                    Tasks.WriteLogFile(2, "Reboot Pending check error with " + strTmpServername + ": " + ee.Message, true);
                    return;
                }
            }

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
            if (File.Exists(xmlFilename))
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
                    WriteLogFile(2, xmlFilename + " not imported:" + ee.Message.ToString(Global.cultures));
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
                    Global.streamLogFile.WriteLine(System.DateTime.Now.ToString("yyyy.MM.dd_HHmmss", Global.cultures) + Global.stringTab + "INFO" + Global.stringTab + "Logfile created");
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
            if (!Global.bVerboseLog && bVerbose) { return; }
            if (Global.bDirectoryWritable)
            {
                // Create Classification string
                string strClassification;
                if (iClassification == 0)
                {
                    strClassification = "INFO";
                }
                else if (iClassification == 1)
                {
                    strClassification = "WARNING";
                }
                else if (iClassification == 2)
                {
                    strClassification = "ERROR";
                }
                else
                {
                    strClassification = "UNKNOWN";
                }
                // Write Log Entry
                using (Global.streamLogFile = File.AppendText(Global.strLogFile))
                {
                    Global.streamLogFile.WriteLine(System.DateTime.Now.ToString("yyyy.MM.dd_HHmmss", Global.cultures) + Global.stringTab + strClassification + Global.stringTab + strArgument);
                }
            }
        }
        public static string CheckServiceStatus(string strServiceName)
        {
            using (ServiceController sc = new ServiceController(strServiceName))
            {
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
            if (CheckServiceStatus("WinRM") != "Running")
            {
                strMessage = "WinRM Service is not running!";
                return false;
            }
            string strTrustedHostsLine = "";
            try
            {
                // Start the child process.>
                using (Process p = new Process())
                {
                    // Redirect the output stream of the child process.
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = "/C winrm get winrm/config/client";
                    p.Start();
                    p.WaitForExit();
                    // Get Output of cmd and create string array
                    string[] strOutput = p.StandardOutput.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    // Get line where 'TrustedHosts' exists and split it with = and than trim it
                    strTrustedHostsLine = Array.Find(strOutput, str => str.Contains("TrustedHosts")).Split(new[] { '=' }).Last().Trim();
                }
                //if (strTrustedHostsLine.Length == 0)
                if (strTrustedHostsLine == "TrustedHosts")
                {
                    strMessage = "No hosts are in the TrustedHosts list.";
                    Tasks.WriteLogFile(0, strMessage, true);
                    return false;
                }
                if (!strTrustedHostsLine.Contains("*"))
                {
                    strMessage = "The following hosts are in the TrustedHosts list: " + strTrustedHostsLine.Replace(",", ", ").ToUpper(Global.cultures);
                    Tasks.WriteLogFile(0, strMessage, true);
                    return false;
                }
            }
            catch (Exception ee)
            {
                strMessage = "An error occured while retrieving the WinRM TrustedHosts list. ";
                WriteLogFile(2, strMessage + ee.Message, true);
                return false;
            }
            strMessage = "WinRM service is running and * is in the TrustedHosts list.";
            Tasks.WriteLogFile(0, strMessage, true);
            return true;
        }
        public static void SetServiceStartup(string strServicename, string strStartType)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.Arguments = "/c sc config \"" + strServicename + "\" start=" + strStartType;
                p.StartInfo.UseShellExecute = true;
                p.Start();
                p.WaitForExit(5000);
            }
        }
        public static bool GetServiceStartup(string strServicename)
        {
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/f2fed453-edca-44bb-8f70-197d9bcddc41/get-startup-type-of-a-windows-service?forum=netfxbcl

            RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\" + strServicename);
            int startupTypeValue = (int)reg.GetValue("Start");
            switch (startupTypeValue)
            {
                case 0:
                    // startupType = "BOOT";
                    return false;
                case 1:
                    // startupType = "SYSTEM";
                    return false;
                case 2:
                    //startupType = "AUTOMATIC";
                    return true;
                case 3:
                    //startupType = "MANUAL";
                    return true;
                case 4:
                    //startupType = "DISABLED";
                    return false;
                default:
                    //startupType = "UNKNOWN";
                    return false;
            }
        }
        public static void StartService(string strServicename)
        {

            if (!GetServiceStartup(strServicename))
            {
                SetServiceStartup(strServicename, "demand");
            }
            using (ServiceController service = new ServiceController(strServicename))
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(5000));
                }
            }
        }
        public static void SetTrustedHosts(string strTrustedHosts)
        {
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                pipeline.Commands.AddScript(@"set-item wsman:\localhost\Client\TrustedHosts -Value '" + strTrustedHosts + "' -Force");
                pipeline.Invoke();
            }
        }
        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static void Elevate(string strArguments)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.Verb = "runas";
                    p.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                    p.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                    p.StartInfo.Arguments = strArguments;
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                    p.WaitForExit(5000);
                }
                WriteLogFile(0, "UAC Elevation successfull");
            }
            catch (Exception ee)
            {
                WriteLogFile(2, "UAC Elevation error: " + ee.Message);
            }
        }
        public static bool SendTestMail(string strSMTPServer, string strSMTPPort, string strMailFrom, string strMailTo)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(strMailFrom);
                    mail.To.Add(strMailTo);
                    mail.Subject = "Test Mail from RemoteUpdate";
                    mail.Body = "This is a Test Mail from RemoteUpdate to check if the SMTP Settings are OK.";
                    using (SmtpClient SmtpServer = new SmtpClient(strSMTPServer))
                    {
                        SmtpServer.Port = int.Parse(strSMTPPort, Global.cultures);
                        SmtpServer.Timeout = 1000;
                        SmtpServer.Send(mail);
                        Tasks.WriteLogFile(0, "Test Mail was sent to " + strMailTo);
                    }
                }
            }
            catch (SmtpException ee)
            {
                Tasks.WriteLogFile(2, "Test Mail had an error while sending: " + ee.Message);
                return false;
            }
            return true;
        }
        public static void LockAndWriteDataTable(System.Data.DataTable tmpTable, int line, string strColumn, string strValue, int iTimeout)
        {
            if (Monitor.TryEnter(tmpTable.Rows.SyncRoot, iTimeout))
            {
                try
                {
                    tmpTable.Rows[line][strColumn] = strValue;
                }
                finally
                {
                    Monitor.Exit(tmpTable.Rows.SyncRoot);
                }
            }
            else
            {
                WriteLogFile(1, "Locking of Table " + tmpTable.TableName.ToString(Global.cultures) + " in Row " + line + " and Column " + strColumn + " with value " + strValue + " failed.", true);
            }
        }
        public static string GetFinalRedirect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            int maxRedirCount = 8;  // prevent infinite loops
            string newUrl = url;
            do
            {
                HttpWebRequest req = null;
                HttpWebResponse resp = null;
                try
                {
                    Uri uurl = new Uri(url);
                    req = (HttpWebRequest)WebRequest.Create(new Uri(url));
                    req.Method = "HEAD";
                    req.AllowAutoRedirect = false;
                    resp = (HttpWebResponse)req.GetResponse();
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return newUrl;
                        case HttpStatusCode.Redirect:
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            newUrl = resp.Headers["Location"];
                            if (newUrl == null)
                                return url;

                            if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                            {
                                // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                                Uri u = new Uri(new Uri(url), newUrl);
                                newUrl = u.ToString();
                            }
                            break;
                        default:
                            return newUrl;
                    }
                    url = newUrl;
                }
                catch (WebException)
                {
                    // Return the last known good URL
                    return newUrl;
                }
                catch (Exception ee)
                {
                    return null;
                }
                finally
                {
                    if (resp != null)
                        resp.Close();
                }
            } while (maxRedirCount-- > 0);

            return newUrl;
        }
        public static void CheckLatestVersion()
        {
            try
            {
                // Check Directory if there are write rights
                string strCurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(strCurrentDirectory);
                string strLatestVersionUrl = Tasks.GetFinalRedirect("https://github.com/aimaat/RemoteUpdate/releases/latest");
                string strLatestVersion = strLatestVersionUrl.Substring(strLatestVersionUrl.Length - 7);
                Version ActualVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
                Version LatestVersion = new Version(strLatestVersion);
                //Version LatestVersion = new Version("0.3.2.3");
                if (LatestVersion > ActualVersion)
                {
                    Tasks.WriteLogFile(0, "New version is available; Installed: " + ActualVersion.ToString() + "; New Version: " + strLatestVersion);
                    Global.bNewVersionOnline = true;
                }
                else
                {
                    Tasks.WriteLogFile(0, "No new version was found");
                    Global.bNewVersionOnline = false;
                }
            }
            catch (Exception ee)
            {
                Tasks.WriteLogFile(2, "An error occured while searching for a new version: " + ee.Message);
                Global.bNewVersionOnline = false;
            }
        }
        public static void UpdateRemoteUpdate()
        {
            try
            {
                // Download latest version from github
                using (var client = new WebClient())
                {
                    client.DownloadFile("https://github.com/aimaat/RemoteUpdate/releases/latest/download/RemoteUpdate.exe", "RemoteUpdateLatest.exe");
                }
                WriteLogFile(1, "Newest version downloaded", true);
                int PID = Process.GetCurrentProcess().Id;
                string strApplicationName = Process.GetCurrentProcess().MainModule.ModuleName;
                string strCurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                // Create cmd for update mechanics (kill remoteupdate, delete exe, move downloaded exe to original place, start it and delete the script)
                using (System.IO.StreamWriter swBat = File.AppendText(Path.Combine(strCurrentDirectory, "UpdateRemoteUpdate.cmd")))
                {
                    swBat.WriteLine("taskkill /PID " + PID + " /F");
                    swBat.WriteLine("timeout /T 1");
                    swBat.WriteLine("cd \"" + strCurrentDirectory + "\"");
                    swBat.WriteLine("del \"" + strApplicationName + "\"");
                    swBat.WriteLine("copy \"RemoteUpdateLatest.exe\" \"" + strApplicationName + "\"");
                    swBat.WriteLine("del \"RemoteUpdateLatest.exe\"");
                    swBat.WriteLine("start \"" + strApplicationName + "\" \"" + strApplicationName + "\"");
                    swBat.WriteLine("DEL \"%~f0\"");
                }
                WriteLogFile(1, "Selfupdate script created, waiting for execution", true);
                // start created cmd
                using (Process p = new Process())
                {
                    // Redirect the output stream of the child process.
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = false;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = "/C " + Path.Combine(strCurrentDirectory, "UpdateRemoteUpdate.cmd");
                    p.Start();
                }
            }
            catch (Exception ee)
            {
                WriteLogFile(2, "An error occured while the self updating process: " + ee.Message);
            }
        }
        public static void UpdateStatusGUI(int line, string strStatus, Grid GridMainWindow)
        {
            if (strStatus == "progress")
            {
                Uri UriImage;
                if ((line + 1) % 2 == 0)
                {
                    UriImage = new Uri(@"pack://application:,,,/Pictures/loading_lightgray.gif", UriKind.Absolute);
                }
                else
                {
                    UriImage = new Uri(@"pack://application:,,,/Pictures/loading_gray.gif", UriKind.Absolute);
                }
                GifImage tmpGifImage = GridMainWindow.Children.OfType<GifImage>().Where(gif => gif.Name.Equals("gifImage_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault();
                tmpGifImage.Source = new System.Windows.Media.Imaging.BitmapImage(UriImage);
                tmpGifImage.Visibility = System.Windows.Visibility.Visible;
                tmpGifImage.StartAnimation();
            }
            else if (strStatus == "error")
            {
                Uri UriImage = new Uri(@"pack://application:,,,/Pictures/error.gif", UriKind.Absolute);
                GifImage tmpGifImage = GridMainWindow.Children.OfType<GifImage>().Where(gif => gif.Name.Equals("gifImage_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault();
                tmpGifImage.Source = new System.Windows.Media.Imaging.BitmapImage(UriImage);
                tmpGifImage.Visibility = System.Windows.Visibility.Visible;
            }
            else if (strStatus == "finished")
            {
                Uri UriImage = new Uri(@"pack://application:,,,/Pictures/checkmark.gif", UriKind.Absolute);
                GifImage tmpGifImage = GridMainWindow.Children.OfType<GifImage>().Where(gif => gif.Name.Equals("gifImage_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault();
                tmpGifImage.StopAnimation();
                tmpGifImage.Source = new System.Windows.Media.Imaging.BitmapImage(UriImage);
                tmpGifImage.Visibility = System.Windows.Visibility.Visible;
                //tmpGifImage.UpdateLayout();
            }
            else if (strStatus == "pending")
            {
                Uri UriImage = new Uri(@"pack://application:,,,/Pictures/restart.gif", UriKind.Absolute);
                GifImage tmpGifImage = GridMainWindow.Children.OfType<GifImage>().Where(gif => gif.Name.Equals("gifImage_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault();
                tmpGifImage.Source = new System.Windows.Media.Imaging.BitmapImage(UriImage);
                tmpGifImage.Visibility = System.Windows.Visibility.Visible;
            }
            else if (strStatus == "clear")
            {
                GifImage tmpGifImage = GridMainWindow.Children.OfType<GifImage>().Where(gif => gif.Name.Equals("gifImage_" + line.ToString(Global.cultures), StringComparison.Ordinal)).FirstOrDefault();
                tmpGifImage.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {

            }
        }
        public static void StartReboot(int line)
        {
            if (Global.TableRuntime.Rows[line]["IP"].ToString().Length == 0) { return; }
            string strTmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
            string strTmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
            string strTmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                if (strTmpUsername.Length != 0 && strTmpPassword.Length != 0)
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + strTmpPassword + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + strTmpUsername + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + strTmpServername + "' { Restart-Computer -Force };");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + strTmpServername + "' { Restart-Computer -Force };");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    Tasks.WriteLogFile(2, "Server " + strTmpServername + " was rebooted", true);
                }
                catch (InvalidPipelineStateException ee)
                {
                    Tasks.WriteLogFile(2, "Reboot error with " + strTmpServername + ": " + ee.Message, true);
                    return;
                }
            }
        }
    }
}
