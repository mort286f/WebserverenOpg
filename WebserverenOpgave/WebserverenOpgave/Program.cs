using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebserverenOpgave
{
    class Program
    {
        static void Main(string[] args)
        {
            Webserver webserver = new Webserver();

            //Gets hostname from Dns for finding ip address
            string hostName = Dns.GetHostName();
            //string that contains ipaddress found from the dns.  
            System.Net.IPAddress myIP = Dns.GetHostEntry(hostName).AddressList[1];
            //filepath
            string filepath = @"C:/Users/mort286f/source/repos/WebserverenOpgave/WebserverenOpgave";

            
            //start webserver
            webserver.Start(IPAddress.Parse("10.108.168.254"), 8080, 1, filepath);
            int serverMsgCount = 0;
            while (webserver.isRunning)
            {
                serverMsgCount++;
                Console.WriteLine($"({serverMsgCount}) Server is running...");
                Thread.Sleep(10000);
            }
            if (!webserver.isRunning)
            {
                Console.WriteLine("Server stopped");
            }
            Console.Read();
        }
    }
}
