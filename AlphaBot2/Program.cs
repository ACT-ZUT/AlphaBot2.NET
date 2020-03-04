using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace AlphaBot2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("AlphaBot2.NET");
            Console.WriteLine("ver 0.1.0, ACT Science Club");
            Console.WriteLine("SaDiablo");

            var Robot = new AlphaBot2();
            Robot.ImuTest();

            Console.CancelKeyPress += (o, e) =>
            {
                GC.Collect();
            };
        }
    }
}