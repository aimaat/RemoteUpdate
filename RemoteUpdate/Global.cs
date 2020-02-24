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
    }
}
