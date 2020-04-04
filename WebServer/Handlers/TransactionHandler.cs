using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace WebServer.Handlers
{
    public class TransactionHandler
    {
        readonly IAuditWriter _writer;
        readonly GlobalTransaction _globalTransaction;
        readonly ServicePool _pool;

        public TransactionHandler(GlobalTransaction gt, IAuditWriter aw, ServicePool p)
        {
            _globalTransaction = gt;
            _pool = p;
            _writer = aw;
        }

        public async Task<(HttpStatusCode,string)> HandleTransaction(string Command)
        {
            if (Command == null || Command.Length == 0)
            {
                return (HttpStatusCode.BadRequest,"Please enter a command");
            }

            string[] args = Array.ConvertAll(Command.Split(','), p => p.Trim());
            if (!Enum.TryParse(typeof(commandType), args[0].ToUpper(), out object ct))
            {
                return (HttpStatusCode.BadRequest, "Invalid command");
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
                        return (HttpStatusCode.BadRequest, "Usage:\tDUMPLOG, userid, filename\n\tDUMPLOG filename");
                    }
                    return (HttpStatusCode.OK, "");
                default:
                    return (HttpStatusCode.BadRequest, "Command not found");
            }
            string Result = null;
            if (con != null)
            {
                if (con.Validate(args, ref userCommand, out string error))
                {
                    Result = await GetServiceResult(con, userCommand).ConfigureAwait(false);
                }
                else
                {
                    return (HttpStatusCode.BadRequest, error);
                }
            }

            if (Result == null)
            {
                return (HttpStatusCode.ServiceUnavailable, "Service unavailable");
            }

            return (HttpStatusCode.OK, Result);
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
                CancellationTokenSource cts = new CancellationTokenSource(10000);
                while (!cts.IsCancellationRequested)
                {
                    //ConnectedServices cs = clientConnections.GetOrAdd(userCommand.username, new ConnectedServices());
                    //ServiceConnection conn = await cs.GetServiceConnectionAsync(sc).ConfigureAwait(false);
                    LeasedService ls = await _pool.Lease(sc, cts.Token).ConfigureAwait(false);
                    if (cts.IsCancellationRequested)
                    {
                        throw new Exception("Failed to connect to service");
                    }
                    result = await ls.Service.SendAsync(userCommand, true).ConfigureAwait(false);
                    if (result != null)
                        break;
                    await Task.Delay(delay).ConfigureAwait(false); // Short delay before tring again
                }

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
