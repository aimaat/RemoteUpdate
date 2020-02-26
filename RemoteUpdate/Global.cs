using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace RemoteUpdate
{
    static class Global
    {
        // Table for App Settings
        public static System.Data.DataTable TableSettings = new System.Data.DataTable("Settings");
        // Table for Runtime values like Server, IP, Status from Ping and Uptime etc.
        public static System.Data.DataTable TableRuntime = new System.Data.DataTable("Runtime");
        // BackgroundWorker List for Uptime Checks
        public static List<BackgroundWorker> ListBackgroundWorkerUptime = new List<BackgroundWorker>();
        // BackgroundWorker List for Ping Checks
        public static List<BackgroundWorker> ListBackgroundWorkerPing = new List<BackgroundWorker>();
        // Timer for Grid Update
        public static DispatcherTimer TimerUpdateGrid = new DispatcherTimer();
        // Culture variable, for future changes to dynamic adaption
        public static System.Globalization.CultureInfo cultures = new System.Globalization.CultureInfo("en-US");
        // Variable for Logfile Destination
        public static string strLogFile = System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateLog-" + System.DateTime.Now.ToString("yyyy.MM.dd_hhmmss", cultures) + ".txt";
        // Bool for Log and Save Write
        public static bool bDirectoryWritable;
        // Streamwriter for LogFile
        public static System.IO.StreamWriter streamLogFile;
        // Logfile divide character
        public static string stringTab = System.Convert.ToChar(9).ToString(cultures);
        // Verbose Logging enabled?
        public static bool bVerboseLog = false;
        // IPHostEntry for localhost to check if servername is localhost
        public static System.Net.IPHostEntry localHost = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
    }
}
