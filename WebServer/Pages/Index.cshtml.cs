using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace WebServer.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private static string _transactionServerIPString = Environment.GetEnvironmentVariable("TransactionServerIP");
        
        //private static string _auditServerIP = Environment.GetEnvironmentVariable("AuditServerIP");

        [BindProperty]
        public string Command { get; set; }

        [BindProperty]
        public string Result { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            Console.WriteLine($"TransactionServerIP: {_transactionServerIPString}");
        }

        public void OnGet()
        {

        }

        public void OnPost()
        {
            Console.WriteLine($"Command: {Command}");
            string[] args = Command.Split(",");
            if (args.Length == 0)
            {
                Result = "Please enter a command";
                return;
            }
            
            switch (args[0].ToUpper())
            {
                case "QUOTE":
                    if (args.Length == 3)
                    {
                        Result = GetServiceResult("quote_service", 44440, $"{args[1]},{args[2]}");
                    } else
                    {
                        Result = "Usage: QUOTE,userid,stock";
                    }
                    break;
                case "ADD":
                    if (args.Length == 3)
                    {
                        Result = GetServiceResult("add_service", 44441, $"{args[1]},{args[2]}");
                    }
                    else
                    {
                        Result = "Usage: ADD,userid,money";
                    }
                    break;
                case "BUY":
                case "COMMIT_BUY":
                case "CANCEL_BUY":
                case "SELL":
                case "COMMIT_SELL":
                case "CANCEL_SELL":
                case "SET_BUY_AMOUNT":
                case "CANCEL_SET_BUY":
                case "SET_BUY_TRIGGER":
                case "SET_SELL_AMOUNT":
                case "SET_SELL_TRIGGER":
                case "CANCEL_SET_SELL":
                case "DUMPLOG":
                case "DISPLAY_SUMMARY":
                    Result = "Not Yet Implemented";
                    break;
                default:
                    Result = "Invalid Command";
                    break;
            }
        }

        /*
         * @Param service The port for the service to connect to
         */
        static string GetServiceResult(string service, int port, string command)
        {
            IPAddress[] addresslist = Dns.GetHostAddresses(service);
            var ipAddr = addresslist.FirstOrDefault();
            string result = "";
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);

                // Creation TCP/IP Socket using  
                // Socket Class Costructor 
                Socket sender = new Socket(ipAddr.AddressFamily,
                           SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // Connect Socket to the remote  
                    // endpoint using method Connect() 
                    sender.Connect(localEndPoint);

                    // We print EndPoint information  
                    // that we are connected 
                    Console.WriteLine("Socket connected to -> {0} ",
                                  sender.RemoteEndPoint.ToString());

                    // Creation of message that 
                    // we will send to Server 
                    int byteSent = sender.Send(Encoding.ASCII.GetBytes(command));

                    // Data buffer 
                    byte[] quoteReceived = new byte[1024];

                    // We receive the messagge using  
                    // the method Receive(). This  
                    // method returns number of bytes 
                    // received, that we'll use to  
                    // convert them to string 
                    int byteRecv = sender.Receive(quoteReceived);
                    result = Encoding.ASCII.GetString(quoteReceived,
                                                     0, byteRecv);
                    Console.WriteLine($"Command: {command}\nResult: ${result}");

                    // Close Socket using  
                    // the method Close() 
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
            return result;
        }
    }
}
