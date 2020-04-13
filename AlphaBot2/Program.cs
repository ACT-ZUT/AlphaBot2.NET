using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace AlphaBot2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            Debug debug = new Debug();
            debug.WriteInfo();
            AlphaBot2 Robot = new AlphaBot2(argsList);

            Console.CancelKeyPress += (o, e) =>
            {
                GC.Collect();
            };
        }
    }
}