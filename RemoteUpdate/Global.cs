using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace RemoteUpdate
{
    static class Global
    {
        public static System.Data.DataTable TableSettings = new System.Data.DataTable("Settings");
        public static System.Data.DataTable TableRuntime = new System.Data.DataTable("Runtime");
        // BackgroundWorker List for Uptime Checks
        public static List<BackgroundWorker> ListBackgroundWorkerUptime = new List<BackgroundWorker>();
        // BackgroundWorker List for Ping Checks
        public static List<BackgroundWorker> ListBackgroundWorkerPing = new List<BackgroundWorker>();
    }
}
