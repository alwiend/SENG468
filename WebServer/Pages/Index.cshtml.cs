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

        public async Task<IActionResult> OnPostAsync()
        {
            if (Command == null || Command.Length == 0)
            {
                Result = "Please enter a command";
                return Page();
            }

            if (Command == "AsyncTest")
            {
                UserCommandType cmd = new UserCommandType
                {
                    timestamp = Unix.TimeStamp.ToString(),
                    server = Server.WEB_SERVER.Abbr,
                    command = commandType.QUOTE,
                    username = "AsyncTest",
                    transactionNum = _globalTransaction.Count.ToString()
                };
                await _writer.WriteRecord(cmd);
                return Page();
            }

            string[] args = Array.ConvertAll(Command.Split(','), p => p.Trim());
            if (!Enum.TryParse(typeof(commandType), args[0].ToUpper(), out object ct))
            {
                Result = "Invalid command";
                return Page();
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
                        Result = await GetServiceResult(Service.QUOTE_SERVICE, userCommand);
                    } else
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
                        Result = await GetServiceResult(Service.ADD_SERVICE, userCommand);
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
                        await _writer.WriteRecord(userCommand);
                    } 
                    else if (args.Length == 3)
                    {
                        userCommand.username = args[1];
                        userCommand.filename = args[2];
                        await _writer.WriteRecord(userCommand);
                    }
                    else
                    {
                        Result = "Usage: DUMPLOG, userid, filename\nDUMPLOG filename";
                    }
                    break;
                case commandType.BUY:
                    if (args.Length == 4)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        Result = await GetServiceResult(Service.BUY_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: BUY,userid,stock,amount";
                    }
                    break;
                case commandType.COMMIT_BUY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.BUY_COMMIT_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: COMMIT_BUY,userid";
                    }
                    break;
                case commandType.CANCEL_BUY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.BUY_CANCEL_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: CANCEL_BUY,userid";
                    }
                    break;
                case commandType.SELL:
                    if (args.Length == 4)
                    {
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        Result = await GetServiceResult(Service.SELL_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: SELL,userid,StockSymbol,amount";
                    }
                    break;
                case commandType.COMMIT_SELL:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.SELL_COMMIT_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: COMMIT_SELL,userid";
                    }
                    break;
                case commandType.CANCEL_SELL:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.SELL_CANCEL_SERVICE, userCommand);
                    } else
                    {
                        Result = "Usage: CANCEL_SELL,userid";
                    }
                    break;
                case commandType.SET_BUY_AMOUNT:
                    if (args.Length == 4)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        Result = await GetServiceResult(Service.BUY_TRIGGER_AMOUNT_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: SET_BUY_AMOUNT,jsmith,ABC,50.00";
                    }
                    break;
                case commandType.CANCEL_SET_BUY:
                    if (args.Length == 3)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        Result = await GetServiceResult(Service.BUY_TRIGGER_CANCEL_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: CANCEL_SET_BUY,jsmith,ABC";
                    }
                    break;
                case commandType.SET_BUY_TRIGGER:
                    if (args.Length == 4)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        Result = await GetServiceResult(Service.BUY_TRIGGER_SET_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: SET_BUY_TRIGGER,jsmith,ABC,20.00";
                    }
                    break;
                case commandType.SET_SELL_AMOUNT:
                    if (args.Length == 4)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        Result = await GetServiceResult(Service.SELL_TRIGGER_AMOUNT_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: SET_SELL_AMOUNT,jsmith,ABC,50.00";
                    }
                    break;
                case commandType.SET_SELL_TRIGGER:
                    if (args.Length == 4)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]);
                        Result = await GetServiceResult(Service.SELL_TRIGGER_SET_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: SET_SELL_TRIGGER,jsmith,ABC,20.00";
                    }
                    break;
                case commandType.CANCEL_SET_SELL:
                    if (args.Length == 3)
                    {
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        Result = await GetServiceResult(Service.SELL_TRIGGER_CANCEL_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: CANCEL_SET_SELL,jsmith,ABC";
                    }
                    break;
                case commandType.DISPLAY_SUMMARY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.DISPLAY_SUMMARY_SERVICE, userCommand);
                        
                    } else
                    {
                        Result = "Usage: DISPLAY_SUMMARY,userid";
                    }                    
                    break;
                default:
                    Result = "Invalid Command";
                    break;
            }

            return Page();
        }

        /*
         * @Param service The port for the service to connect to
         */
        async Task<string> GetServiceResult(ServiceConstant sc, UserCommandType userCommand)
        {
            await _writer.WriteRecord(userCommand).ConfigureAwait(false);
            //ServiceConnection conn = new ServiceConnection(IPAddress.Loopback, sc.Port);
            ServiceConnection conn = new ServiceConnection(sc);
            return await conn.Send(userCommand, true).ConfigureAwait(false);
        }
    }
}
