using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Security.Cryptography;
using System.Text;

namespace RemoteUpdate
{
    class Tasks
    {
        public static string Encrypt(string clearText)
        {
            if (clearText == "") { return clearText; }
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
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
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
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        public static bool CheckPSConnection(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                pipeline.Commands.AddScript("New-PSSession -ComputerName " + Global.TableRuntime.Rows[line]["Servername"].ToString() + " -ConfigurationName VirtualAccount;");
                try
                {
                    var exResults = pipeline.Invoke();
                    if(pipeline.HadErrors == false)
                    {
                        psRunspace.Close();
                        return true;
                    } else
                    {
                        psRunspace.Close();
                        return false;
                    }
                } catch
                {
                    psRunspace.Close();
                    return false;
                }
            }
        }
        public static bool CreatePSVirtualAccount(int line)
        {
            var sessionState = InitialSessionState.CreateDefault();
            using (var psRunspace = RunspaceFactory.CreateRunspace(sessionState))
            {
                string tmpServername = Global.TableRuntime.Rows[line]["Servername"].ToString();
                string tmpPSVirtualAccountName = Global.TableSettings.Rows[0]["PSVirtualAccountName"].ToString();
                string tmpUsername = Global.TableRuntime.Rows[line]["Username"].ToString();
                string tmpPassword = Global.TableRuntime.Rows[line]["Password"].ToString();

                psRunspace.Open();
                Pipeline pipeline = psRunspace.CreatePipeline();
                if (tmpUsername != "" && tmpPassword != "")
                {
                    pipeline.Commands.AddScript("$pass = ConvertTo-SecureString -AsPlainText '" + Global.TableRuntime.Rows[line]["Password"].ToString() + "' -Force;");
                    pipeline.Commands.AddScript("$Cred = New-Object System.Management.Automation.PSCredential -ArgumentList '" + Global.TableRuntime.Rows[line]["Username"].ToString() + "',$pass;");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + tmpServername + "' { Install-Module PSWindowsUpdate -Force };");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + tmpServername + "' { New-PSSessionConfigurationFile -RunAsVirtualAccount -Path .\\" + tmpPSVirtualAccountName + ".pssc };");
                    pipeline.Commands.AddScript("Invoke-Command -Credential $Cred -ComputerName '" + tmpServername + "' { Register-PSSessionConfiguration -Name '" + tmpPSVirtualAccountName + "' -Path .\\" + tmpPSVirtualAccountName + ".pssc -Force }");
                }
                else
                {
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + tmpServername + "' { Install-Module PSWindowsUpdate -Force };");
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + tmpServername + "' { New-PSSessionConfigurationFile -RunAsVirtualAccount -Path .\\" + tmpPSVirtualAccountName + ".pssc };");
                    pipeline.Commands.AddScript("Invoke-Command -ComputerName '" + tmpServername + "' { Register-PSSessionConfiguration -Name '" + tmpPSVirtualAccountName + "' -Path .\\" + tmpPSVirtualAccountName + ".pssc -Force }");
                }
                try
                {
                    var exResults = pipeline.Invoke();
                    if(pipeline.Error.Count > 1)
                    {
                        return false;
                    } else { 
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
