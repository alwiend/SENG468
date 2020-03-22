using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Constants;
using Microsoft.AspNetCore.Mvc;
using Utilities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebServer
{
    [Produces("application/json")]
    [Route("api/command")]
    public class CommandController : Controller
    {
        readonly IAuditWriter _writer;
        readonly GlobalTransaction _globalTransaction;
        readonly static ConcurrentDictionary<string, ConnectedServices> clientConnections = new ConcurrentDictionary<string, ConnectedServices>();

        public class TransactionCommand
        {
            public string Command;
        }

        public CommandController(GlobalTransaction gt, IAuditWriter aw)
        {
            _globalTransaction = gt;
            _writer = aw;
        }

        [HttpHead]
        public async Task<IActionResult> HeadAsync([FromQuery(Name = "cmd")] string Command)
        {
            return await HandleTransaction(Command);
        }


        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery(Name = "cmd")] string Command)
        {
            return await HandleTransaction(Command);
        }


        // POST api/<controller>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody]TransactionCommand transaction)
        {
            return await HandleTransaction(transaction.Command);
        }

        private async Task<IActionResult> HandleTransaction(string Command)
        { 
            string Result = "";
            if (Command == null || Command.Length == 0)
            {
                Result = "Please enter a command";
                return BadRequest(Result);
            }

            string[] args = Array.ConvertAll(Command.Split(','), p => p.Trim());
            if (!Enum.TryParse(typeof(commandType), args[0].ToUpper(), out object ct))
            {
                Result = "Invalid command";
                return BadRequest(Result);
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
                        var quote = await GetServiceResult(Service.QUOTE_SERVICE, userCommand);
                        Result = $"{Convert.ToDecimal(quote) / 100m}";
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
                        userCommand.funds = Convert.ToDecimal(args[2]) * 100;
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
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]) * 100;
                        Result = await GetServiceResult(Service.BUY_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: BUY,userid,stock,amount";
                    }
                    break;
                case commandType.COMMIT_BUY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.BUY_COMMIT_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: COMMIT_BUY,userid";
                    }
                    break;
                case commandType.CANCEL_BUY:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.BUY_CANCEL_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: CANCEL_BUY,userid";
                    }
                    break;
                case commandType.SELL:
                    if (args.Length == 4)
                    {
                        userCommand.fundsSpecified = true;
                        userCommand.funds = Convert.ToDecimal(args[3]) * 100;
                        userCommand.username = args[1];
                        userCommand.stockSymbol = args[2];
                        Result = await GetServiceResult(Service.SELL_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: SELL,userid,StockSymbol,amount";
                    }
                    break;
                case commandType.COMMIT_SELL:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.SELL_COMMIT_SERVICE, userCommand);
                    }
                    else
                    {
                        Result = "Usage: COMMIT_SELL,userid";
                    }
                    break;
                case commandType.CANCEL_SELL:
                    if (args.Length == 2)
                    {
                        userCommand.username = args[1];
                        Result = await GetServiceResult(Service.SELL_CANCEL_SERVICE, userCommand);
                    }
                    else
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
                        userCommand.funds = Convert.ToDecimal(args[3]) * 100;
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
                        userCommand.funds = Convert.ToDecimal(args[3]) * 100;
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
                        userCommand.funds = Convert.ToDecimal(args[3]) * 100;
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
                        userCommand.funds = Convert.ToDecimal(args[3]) * 100;
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
                    }
                    else
                    {
                        Result = "Usage: DISPLAY_SUMMARY,userid";
                    }
                    break;
                default:
                    Result = "Invalid Command";
                    break;
            }
            if (Result == null)
            {
                return StatusCode(503, "Service unavailable");
            }

            return Ok(Result);
        }

        /*
         * @Param service The port for the service to connect to
         */
        async Task<string> GetServiceResult(ServiceConstant sc, UserCommandType userCommand)
        {
            _writer.WriteRecord(userCommand);
            var delay = 500;

            try
            {
                string result = null;
                do
                {
                    ConnectedServices cs = clientConnections.GetOrAdd(userCommand.username, new ConnectedServices());
                    ServiceConnection conn = await cs.GetServiceConnectionAsync(sc).ConfigureAwait(false);
                    if (conn == null)
                    {
                        throw new Exception("Failed to connect to service");
                    }
                    result = await conn.SendAsync(userCommand, true).ConfigureAwait(false);
                    if (result == null)
                        await Task.Delay(delay).ConfigureAwait(false); // Short delay before tring again
                } while (result == null);

                return result;
            }
            catch (Exception ex)
            {
                LogDebugEvent(userCommand, ex.Message);
                return null;
            }
        }

        void LogDebugEvent(UserCommandType command, string err)
        {
            DebugType bug = new DebugType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = Server.WEB_SERVER.Abbr,
                transactionNum = command.transactionNum,
                command = command.command,
                debugMessage = err
            };
            _writer.WriteRecord(bug);
        }
    }
}
