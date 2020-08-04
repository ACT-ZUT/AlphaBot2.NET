using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;
using System.Text;

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
            //launchListener();
            AlphaBot2 Robot = new AlphaBot2(argsList);

            Console.CancelKeyPress += (o, e) =>
            {
                GC.Collect();
            };
        }

        public static void launchListener()
        {
            try
            {
                // use local m/c IP address, and 
                // use the same in the client

                /* Initializes the Listener */
                TcpListener myList = new TcpListener(IPAddress.Any, 8001);

                /* Start Listeneting at the specified port */
                myList.Start();

                Console.WriteLine("The server is running at port 8001...");
                Console.WriteLine("The local End point is  :" +
                                  myList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                Socket s = myList.AcceptSocket();
                Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);

                byte[] b = new byte[100];
                int k = s.Receive(b);
                Console.WriteLine("Recieved...");
                for (int i = 0; i < k; i++)
                    Console.Write(Convert.ToChar(b[i]));

                ASCIIEncoding asen = new ASCIIEncoding();
                s.Send(asen.GetBytes("The string was recieved by the server."));
                Console.WriteLine("\nSent Acknowledgement");
                /* clean up */
                s.Close();
                myList.Stop();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error... " + e.Message);
                Console.WriteLine("Error... " + e.StackTrace);
            }
            while (true) ;
        }
    }
}