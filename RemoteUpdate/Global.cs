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
        // BackgroundWorker for Process Lookup
        public static BackgroundWorker BackgroundWorkerProcess = new BackgroundWorker();
        // Timer for Grid Update
        public static DispatcherTimer TimerUpdateGrid = new DispatcherTimer();
        // Culture variable, for future changes to dynamic adaption
        public static System.Globalization.CultureInfo cultures = System.Globalization.CultureInfo.CurrentCulture;
        // Variable for Logfile Destination
        public static string strLogFile = System.AppDomain.CurrentDomain.BaseDirectory + "RemoteUpdateLog-" + System.DateTime.Now.ToString("yyyy.MM.dd_HHmmss", cultures) + ".txt";
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
        // Variable for threaded Update Search
        public static bool bNewVersionOnline = false;
        // TableScripts
        public static System.Data.DataTable TableScripts = new System.Data.DataTable("Scripts");
        // Password String
        public static string strDecryptionPassword = "";

    }
}
