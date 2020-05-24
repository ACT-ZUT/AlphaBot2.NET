using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;

namespace AlphaBot2
{
    public class Program
    {
        public static TcpListener tcpListener;
        public static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            Debug debug = new Debug();
            debug.WriteInfo();
            AlphaBot2 Robot = new AlphaBot2(argsList);
            TcpListener tcp = new TcpListener(800);

            Console.CancelKeyPress += (o, e) =>
            {
                GC.Collect();
            };
        }

        public static void listenPort()
        {
            IPAddress ipAddress = Dns.Resolve("localhost").AddressList[0];

            try
            {
                tcpListener = new TcpListener(ipAddress, 13);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}