using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace AlphaBot2
{
    class Debug
    {
        /// <summary>
        /// Class used to record, display and export debug data
        /// </summary>
        string line_temp;
        int i = 0;
        public ProcessorArchitecture Architecture;
        public string AssemblyName;
        public DateTime buildDate;
        public Version version;


        public Debug()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Architecture = typeof(AlphaBot2).Assembly.GetName().ProcessorArchitecture;
            AssemblyName = typeof(AlphaBot2).Assembly.GetName().Name;
            version = Assembly.GetExecutingAssembly().GetName().Version;
            buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
        }

        public void WriteInfo()
        {
            Console.WriteLine($"{AssemblyName}");
            Console.WriteLine($"ACT Science Club");
            Console.WriteLine($"{Architecture}");
            Console.WriteLine($"Version: {version}");
            Console.WriteLine($"Build Date: {buildDate}");

#if DEBUG
            Console.WriteLine("Configuration: Debug");
            Console.WriteLine("Waiting for remote debugger...");
            while (true)
            {
                if (Debugger.IsAttached) break;
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
#else
            Console.WriteLine("Configuration: Release");
#endif
        }

        public void CSV_Write(string filename, string line)
        {
            i++;
            line_temp += line;
            if(i % 1000 == 0)
            {
                File.AppendAllText($@"/home/pi/{filename}.csv", line_temp);
                line_temp = "";
                i = 0;
            }
        }
    }
}
