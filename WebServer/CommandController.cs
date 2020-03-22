using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
            string Result = null;
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
            ServiceConstant con = null;
            switch (userCommand.command)
            {
                case commandType.QUOTE:
                    con = Service.QUOTE_SERVICE;
                    break;
                case commandType.ADD:
                    con = Service.ADD_SERVICE;
                    break;
                case commandType.BUY:
                    con = Service.BUY_SERVICE;
                    break;
                case commandType.COMMIT_BUY:
                    con = Service.BUY_COMMIT_SERVICE;
                    break;
                case commandType.CANCEL_BUY:
                    con = Service.BUY_CANCEL_SERVICE;
                    break;
                case commandType.SELL:
                    con = Service.SELL_SERVICE;
                    break;
                case commandType.COMMIT_SELL:
                    con = Service.SELL_COMMIT_SERVICE;
                    break;
                case commandType.CANCEL_SELL:
                    con = Service.SELL_CANCEL_SERVICE;
                    break;
                case commandType.SET_BUY_AMOUNT:
                    con = Service.BUY_TRIGGER_AMOUNT_SERVICE;
                    break;
                case commandType.CANCEL_SET_BUY:
                    con = Service.BUY_TRIGGER_CANCEL_SERVICE;
                    break;
                case commandType.SET_BUY_TRIGGER:
                    con = Service.BUY_TRIGGER_SET_SERVICE;
                    break;
                case commandType.SET_SELL_AMOUNT:
                    con = Service.SELL_TRIGGER_AMOUNT_SERVICE;
                    break;
                case commandType.SET_SELL_TRIGGER:
                    con = Service.SELL_TRIGGER_SET_SERVICE;
                    break;
                case commandType.CANCEL_SET_SELL:
                    con = Service.SELL_TRIGGER_CANCEL_SERVICE;
                    break;
                case commandType.DISPLAY_SUMMARY:
                    con = Service.DISPLAY_SUMMARY_SERVICE;
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
                    return Ok();
                default:
                    return BadRequest("Invalid Command");
            }
            if (con != null)
            {
                if (con.Validate(args, ref userCommand, out string error))
                {
                    Result = await GetServiceResult(con, userCommand).ConfigureAwait(false);
                }
                else
                {
                    Result = error;
                }
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
