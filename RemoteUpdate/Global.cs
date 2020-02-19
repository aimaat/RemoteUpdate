using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteUpdate
{
    static class Global
    {
        public static System.Data.DataTable TableSettings = new System.Data.DataTable("Settings");
        public static System.Data.DataTable TableRuntime = new System.Data.DataTable("Runtime");
        public static string Teststring = "TEST";
    }
}
