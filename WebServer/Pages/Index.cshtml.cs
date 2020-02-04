using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Utilities;

namespace WebServer.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class IndexModel : PageModel
    {
        private readonly GlobalTransaction _globalTransaction;
        private readonly IAuditWriter _writer;

        [BindProperty]
        public string Command { get; set; }

        [BindProperty]
        public string Result { get; set; }

        public IndexModel(GlobalTransaction gt, IAuditWriter aw)
        {
            _globalTransaction = gt;
            _writer = aw;
        }

        public void OnGet()
        {

        }

        public void OnPost()
        {
            string[] args = Array.ConvertAll(Command.Split(','), p => p.Trim()); ;
            if (args.Length == 0)
            {
                Result = "Please enter a command";
                return;
            }
            if (!Enum.TryParse(typeof(commandType), args[0].ToUpper(), out object ct))
            {
                Result = "Invalid command";
                return;
            }

            UserCommandType userCommand = new UserCommandType
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = Server.WEB_SERVER.Abbr,
                command = (commandType)ct,
                transactionNum = _globalTransaction.Count.ToString()
            };

            switch (userCommand.command)
            {
                case commandType.QUOTE:
                    if (args.Length == 3)
                    {
                        userCommand.stockSymbol = args[2];
                        userCommand.username = args[1];
                        Result = GetServiceResult(Service.QUOTE_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: QUOTE,userid,stock";
                    }
                    break;
                case commandType.ADD:
                    if (args.Length == 3)
                    {
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[2]);
                        userCommand.username = args[1];
                        Result = GetServiceResult(Service.ADD_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: ADD,userid,money";
                    }
                    break;
                case commandType.DUMPLOG:
                    if (args.Length == 2)
                    {
                        userCommand.filename = args[1];
                        _writer.WriteRecord(userCommand);
                    }
                    else if (args.Length == 3)
                    {
                        userCommand.username = args[1];
                        userCommand.filename = args[2];
                        _writer.WriteRecord(userCommand);
                    }
                    else
                    {
                        Result = "Usage: DUMPLOG, userid, filename\nDUMPLOG filename";
                    }
                    break;
                case commandType.BUY:
                    if (args.Length == 4)
                    {
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        Result = GetServiceResult(Service.BUY_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: BUY,userid,stock,amount";
                    }
                    break;
                case commandType.COMMIT_BUY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = GetServiceResult(Service.BUY_COMMIT_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: COMMIT_BUY,userid";
                    }
                    break;
                case commandType.CANCEL_BUY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = GetServiceResult(Service.BUY_CANCEL_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: CANCEL_BUY,userid";
                    }
                    break;
                case commandType.SELL:
                case commandType.COMMIT_SELL:
                case commandType.CANCEL_SELL:
                case commandType.SET_BUY_AMOUNT:
                case commandType.CANCEL_SET_BUY:
                case commandType.SET_BUY_TRIGGER:
                case commandType.SET_SELL_AMOUNT:
                case commandType.SET_SELL_TRIGGER:
                case commandType.CANCEL_SET_SELL:
                case commandType.DISPLAY_SUMMARY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = GetServiceResult(Service.DISPLAY_SUMMARY_SERVICE, userCommand);
                        
                    } else
                    {
                        Result = "Usage: DISPLAY_SUMMARY,userid";
                    }
                    
                    break;
                default:
                    Result = "Invalid Command";
                    break;
            }
        }

        /*
         * @Param service The port for the service to connect to
         */
        string GetServiceResult(ServiceConstant sc, UserCommandType userCommand)
        {
            _writer.WriteRecord(userCommand);
            string result = "";
            try
            {
                var ipAddr = Dns.GetHostAddresses(sc.ServiceName).FirstOrDefault();
                //var ipAddr = IPAddress.Loopback;
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, sc.Port);
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                // Connect Socket to the remote  
                // endpoint using method Connect() 
                client.Connect(localEndPoint);
                StreamWriter client_out = null;
                StreamReader client_in = null;

                try
                {
                    client_out = new StreamWriter(client.GetStream());
                    client_in = new StreamReader(client.GetStream());

                    XmlSerializer serializer = new XmlSerializer(typeof(UserCommandType));
                    serializer.Serialize(client_out, userCommand);
                    // Shutdown Clientside sending to signal end of stream
                    client.Client.Shutdown(SocketShutdown.Send);
                    result = client_in.ReadToEnd();
                    client.Client.Shutdown(SocketShutdown.Receive);
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine($"ArgumentNullException : {ane.ToString()}");
                }

                catch (SocketException se)
                {

                    Console.WriteLine($"SocketException : {se.ToString()}");
                }

                catch (Exception e)
                {
                    Console.WriteLine($"Unexpected exception : {e.ToString()}");
                }
                finally
                {
                    client_out.Close();
                    client_in.Close();
                }
                client.Close();
                client.Dispose();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
            return result;
        }
    }
}