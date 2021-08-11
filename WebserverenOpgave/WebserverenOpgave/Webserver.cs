using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebserverenOpgave
{
    public class Webserver
    {
        private Dictionary<string, string> extensions = new Dictionary<string, string>()
{ 
    //{ "extension", "content type" }
    { "htm", "text/html" },
    { "html", "text/html" },
    { "xml", "text/xml" },
    { "txt", "text/plain" },
    { "css", "text/css" },
    { "png", "image/png" },
    { "gif", "image/gif" },
    { "jpg", "image/jpg" },
    { "jpeg", "image/jpeg" },
    { "zip", "application/zip"}
};

        public bool isRunning;
        public int Timeout { get; set; }
        public Encoding CharEncoder { get; set; }
        public Socket ServerSocket { get; set; }
        public string ContentPath { get; set; }

        public Webserver()
        {
            this.Timeout = 8;
            this.CharEncoder = Encoding.UTF8;
        }

        public bool Start(IPAddress ipAddress, int port, int maxConnNumber, string contentPath)
        {
            // If it is already running, exit.
            if (this.isRunning)
            {
                return false;
            }

            try
            {
                // A tcp/ip socket (ipv4)
                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                               ProtocolType.Tcp);
                ServerSocket.Bind(new IPEndPoint(ipAddress, port));
                ServerSocket.Listen(maxConnNumber);
                ServerSocket.ReceiveTimeout = Timeout;
                ServerSocket.SendTimeout = Timeout;
                isRunning = true;
                this.ContentPath = contentPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //thread for listening on incoming requests
            Thread requestListenerT = new Thread(() =>
            {
                while (this.isRunning)
                {
                    Socket clientSocket;
                    try
                    {
                        clientSocket = ServerSocket.Accept();
                        // Create new thread to handle the request and continue to listen the socket.
                        Thread requestHandler = new Thread(() =>
                        {
                            clientSocket.ReceiveTimeout = Timeout;
                            clientSocket.SendTimeout = Timeout;
                            try
                            {
                                HandleTheRequest(clientSocket);
                            }
                            catch (Exception exs)
                            {
                                Console.WriteLine(exs.Message);
                                try
                                {
                                    clientSocket.Close();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        });
                        requestHandler.Start();
                    }
                    catch { }
                }
            });
            requestListenerT.Start();

            return true;
        }


        public void Stop()
        {
            if (this.isRunning)
            {
                isRunning = false;
                try { ServerSocket.Close(); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                ServerSocket = null;
            }
        }

        private void HandleTheRequest(Socket clientSocket)
        {


            byte[] buffer = new byte[10240]; // 10 kb, just in case
            int receivedBCount = clientSocket.Receive(buffer); // Receive the request
            string strReceived = CharEncoder.GetString(buffer, 0, receivedBCount);

            // Parse method of the request
            string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

            int start = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1;
            int length = strReceived.LastIndexOf("HTTP") - start - 1;
            string requestedUrl = strReceived.Substring(start, length);

            //Looks for which type the incoming request is
            string requestedFile;
            if (httpMethod.Equals("GET") || httpMethod.Equals("POST"))
            {
                requestedFile = requestedUrl.Split('?')[0];
            }

            else // You can implement other methods...
            {
                notImplemented(clientSocket);
                return;
            }

            requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");
            start = requestedFile.LastIndexOf('.') + 1;

            //If there is a file, which means if there are any requested file after your IPAddress:portnumber(8080)(filename)
            if (start > 0)
            {
                length = requestedFile.Length - start;
                string extension = requestedFile.Substring(start, length);
                if (File.Exists(ContentPath + requestedFile)) //If yes check existence of the file
                                                              // Everything is OK, send requested file with correct content type:
                    sendOkResponse(clientSocket,
                      File.ReadAllBytes(ContentPath + requestedFile), extensions[extension]);
                else
                    notFound(clientSocket); // We don't support this extension.
                                            // We are assuming that it doesn't exist.
            }

            //Tries to find other files with same or similar name
            else
            {
                // If file is not specified try to send index.htm or index.html
                // You can add more (default.htm, default.html)
                if (requestedFile.Substring(length - 1, 1) != @"\")
                    requestedFile += @"\";
                if (File.Exists(ContentPath + requestedFile + "index.htm"))
                    sendOkResponse(clientSocket,
                      File.ReadAllBytes(ContentPath + requestedFile + "\\index.htm"), "text/html");
                else if (File.Exists(ContentPath + requestedFile + "index.html"))
                    sendOkResponse(clientSocket,
                      File.ReadAllBytes(ContentPath + requestedFile + "\\index.html"), "text/html");
                else
                    notFound(clientSocket);
            }
        }

        private void notImplemented(Socket clientSocket)
        {

            sendResponse(clientSocket, "<html><head><meta " +

                "http - equiv =\"Content-Type\" content=\"text/html; " +

                "charset = utf - 8\">" +
                "</ head >< body >< h2 > Atasoy Simple Web" +

                "Server </ h2 >< div > 501 - Method Not" +

                "Implemented </ div ></ body ></ html > ",

                "501 Not Implemented", "text/html");
        }

        private void notFound(Socket clientSocket)
        {

            sendResponse(clientSocket, "<html><head><meta" +

                "http - equiv =\"Content-Type\" content=\"text/html;" +

                "charset = utf - 8\"></head><body><h2>Atasoy Simple Web " +

                "Server </ h2 >< div > 404 - Not" +

                "Found </ div ></ body ></ html > ",

                "404 Not Found", "text/html");
        }

        private void sendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
        {
            sendResponse(clientSocket, bContent, "200 OK", contentType);
        }

        // For strings
        private void sendResponse(Socket clientSocket, string strContent, string responseCode, string contentType)
        {
            byte[] bContent = CharEncoder.GetBytes(strContent);
            sendResponse(clientSocket, bContent, responseCode, contentType);
        }

        // For byte arrays
        private void sendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)
        {
            try
            {
                byte[] bHeader = CharEncoder.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Atasoy Simple Web Server\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
