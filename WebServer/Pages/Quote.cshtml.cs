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

namespace WebServer
{
    public class QuoteModel : PageModel
    {
        private readonly ILogger<QuoteModel> _logger;

        [BindProperty]
        public string Quote { get; set; }

        [BindProperty]
        public string Cost { get; set; }

        public QuoteModel(ILogger<QuoteModel> logger)
        {
            _logger = logger;
        }
        public void OnGet()
        {

        }

        public void OnPost()
        {

            Cost = $"{Quote} is ${RequestQuote(Quote)}";
        }


        // ExecuteClient() Method 
        static string RequestQuote(string quote)
        {
            string cost = "";
            try
            {

                // Establish the remote endpoint  
                // for the socket. This example  
                // uses port 4444 on the local  
                // computer. 
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 4444);

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
                    int byteSent = sender.Send(Encoding.ASCII.GetBytes(quote));

                    // Data buffer 
                    byte[] quoteReceived = new byte[1024];

                    // We receive the messagge using  
                    // the method Receive(). This  
                    // method returns number of bytes 
                    // received, that we'll use to  
                    // convert them to string 
                    int byteRecv = sender.Receive(quoteReceived);
                    cost = Encoding.ASCII.GetString(quoteReceived,
                                                     0, byteRecv);
                    Console.WriteLine($"Quote: {quote}\nCost: ${cost}");

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
            return cost;
        }
    }
}