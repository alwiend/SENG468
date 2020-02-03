// Connects to Quote Server and returns quote
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Base;
using Constants;
using Utilities;

namespace QuoteService
{

    public class QuoteService : BaseService
    {
        public static void Main(string[] args)
        {
            var quote_service = new QuoteService(Service.QUOTE_SERVICE, new AuditWriter());
            quote_service.StartService();
        }

        public QuoteService(ServiceConstant sc, IAuditWriter aw): base(sc, aw)
        {
            DataReceived = RequestQuote;
        }

        string LogQuoteServerEvent(UserCommandType command, string quote)
        {
            QuoteServerType stockQuote = new QuoteServerType()
            {
                username = command.username,
                server = Server.QUOTE_SERVER.Abbr,
                price = decimal.Parse(quote),
                transactionNum = command.transactionNum,
                stockSymbol = command.stockSymbol,
                timestamp = Unix.TimeStamp.ToString(),
                quoteServerTime = Unix.TimeStamp.ToString(),
                cryptokey = ""
            };
            Auditor.WriteRecord(stockQuote);
            return stockQuote.price.ToString();
        }

        // ExecuteClient() Method 
        object RequestQuote(UserCommandType command)
        {
            string cost = "";
            try
            {
                // Establish the remote endpoint  
                var ipAddr = Dns.GetHostAddresses(Server.QUOTE_SERVER.ServiceName).FirstOrDefault();
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, Server.QUOTE_SERVER.Port);

                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                client.Connect(localEndPoint);
                StreamWriter client_out = null;
                StreamReader client_in = null;
                try
                {
                    client_out = new StreamWriter(client.GetStream());
                    client_in = new StreamReader(client.GetStream());

                    client_out.Write($"{command.stockSymbol},{command.username}");
                    client.Client.Shutdown(SocketShutdown.Send);
                    string quote = client_in.ReadToEnd();
                    client.Client.Shutdown(SocketShutdown.Receive);
                    cost = LogQuoteServerEvent(command, quote);
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
                client.Client.Close();
                client.Dispose();
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
            return cost;
        }
    }
}