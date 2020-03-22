using System;

namespace Utilities
{
    public class Server
    {
        public readonly static ServiceConstant QUOTE_SERVER = new ServiceConstant("quoteserve.seng.uvic.ca", 4448, "QSVR");
        public readonly static ServiceConstant AUDIT_SERVER = new ServiceConstant("audit_server", 44439, "ASVR");
        public readonly static ServiceConstant WEB_SERVER = new ServiceConstant("web_server", 80, "WSVR");
    }

    public class Service
    {
        public readonly static ServiceConstant QUOTE_SERVICE = new ServiceConstant("quote_service", 44440, "QSVC", al: 3, u: "Usage: QUOTE,userid,stock", ui: 1, si: 2);
        public readonly static ServiceConstant ADD_SERVICE = new ServiceConstant("add_service", 44441, "ASVC", al: 3, u: "Usage: ADD,userid,money", ui: 1, fi: 2);
        public readonly static ServiceConstant BUY_SERVICE = new ServiceConstant("buy_service", 44442, "BSVC", al: 4, u: "Usage: BUY,userid,stock,amount", ui: 1, si: 2, fi: 3);
        public readonly static ServiceConstant BUY_CANCEL_SERVICE = new ServiceConstant("buy_service", 44443, "BSVC", al: 2, u: "Usage: CANCEL_BUY,userid", ui: 1);
        public readonly static ServiceConstant BUY_COMMIT_SERVICE = new ServiceConstant("buy_service", 44444, "BSVC", al: 2, u: "Usage: COMMIT_BUY,userid", ui: 1);
        public readonly static ServiceConstant DISPLAY_SUMMARY_SERVICE = new ServiceConstant("display_summary_service", 44445, "DSVC", al: 2, u: "Usage: DISPLAY_SUMMARY,userid", ui: 1);
        public readonly static ServiceConstant SELL_SERVICE = new ServiceConstant("sell_service", 44446, "SSVC", al: 4, u: "Usage: SELL,userid,StockSymbol,amount", ui: 1, si: 2, fi: 3);
        public readonly static ServiceConstant SELL_CANCEL_SERVICE = new ServiceConstant("sell_service", 44447, "SSVC", al: 2, u: "Usage: CANCEL_SELL,userid", ui: 1);
        public readonly static ServiceConstant SELL_COMMIT_SERVICE = new ServiceConstant("sell_service", 44448, "SSVC", al: 2, u: "Usage: COMMIT_SELL,userid", ui: 1);
        public readonly static ServiceConstant BUY_TRIGGER_AMOUNT_SERVICE = new ServiceConstant("buy_trigger_service", 44449, "BTSVC", al: 4, u: "Usage: SET_BUY_AMOUNT,jsmith,ABC,50.00", ui: 1, si: 2, fi: 3);
        public readonly static ServiceConstant BUY_TRIGGER_CANCEL_SERVICE = new ServiceConstant("buy_trigger_service", 44450, "BTSVC", al: 3, u: "Usage: CANCEL_SET_BUY,jsmith,ABC", ui: 1, si: 2);
        public readonly static ServiceConstant BUY_TRIGGER_SET_SERVICE = new ServiceConstant("buy_trigger_service", 44451, "BTSVC", al: 4, u: "Usage: SET_BUY_TRIGGER,jsmith,ABC,20.00", ui: 1, si: 2, fi: 3);
        public readonly static ServiceConstant SELL_TRIGGER_AMOUNT_SERVICE = new ServiceConstant("sell_trigger_service", 44452, "STSVC", al: 4, u: "Usage: SET_SELL_AMOUNT,jsmith,ABC,50.00", ui: 1, si: 2, fi: 3);
        public readonly static ServiceConstant SELL_TRIGGER_CANCEL_SERVICE = new ServiceConstant("sell_trigger_service", 44453, "STSVC", al: 3, u: "Usage: CANCEL_SET_SELL,jsmith,ABC", ui: 1, si: 2);
        public readonly static ServiceConstant SELL_TRIGGER_SET_SERVICE = new ServiceConstant("sell_trigger_service", 44454, "STSVC", al: 4, u: "Usage: SET_SELL_TRIGGER,jsmith,ABC,20.00", ui: 1, si: 2, fi: 3);

    }

    public class ServiceConstant
    {
        public string ServiceName { get; }
        public int Port { get; }

        public string Abbr { get; }

        public string UniqueName { get; }

        private int _usernameIndex = -1;
        private int _stockIndex = -1;
        private int _fundsIndex = -1;
        private int _filenameIndex = -1;
        private int _argLength;
        private string _usage;

        public ServiceConstant(string sn, int port, string abbr, int al = 0, string u = "", int ui = -1, int si = -1, int fi = -1, int fni = -1)
        {
            ServiceName = sn;
            Port = port;
            Abbr = abbr;
            UniqueName = $"{sn}:{port}";
            _usernameIndex = ui;
            _stockIndex = si;
            _fundsIndex = fi;
            _filenameIndex = fni;
            _argLength = al;
            _usage = u;
        }

        public bool Validate(string[] args, ref UserCommandType command, out string error)
        {
            if (args.Length != _argLength)
            {
                error = _usage;
                return false;
            }
            if (_usernameIndex > 0)
            {
                command.username = args[_usernameIndex].Trim();
                if (string.IsNullOrEmpty(command.username))
                {
                    error = "Invalid username";
                    return false;
                }
            }
            if (_filenameIndex > 0)
            {
                command.filename = args[_filenameIndex].Trim();
                if (string.IsNullOrEmpty(command.filename))
                {
                    error = "Invalid username";
                    return false;
                }
            }
            if (_fundsIndex > 0)
            {
                if (!Decimal.TryParse(args[_fundsIndex], out decimal funds) || funds < 0)
                {
                    error = "Invalid amount specified";
                    return false;
                }
                command.fundsSpecified = true;
                command.funds = funds * 100;
            }
            if (_stockIndex > 0)
            {
                command.stockSymbol = args[_stockIndex];
                if (command.stockSymbol.Length > 3 || command.stockSymbol.Length < 1
                    || !System.Text.RegularExpressions.Regex.IsMatch(command.stockSymbol, @"^[a-zA-Z]+$"))
                {
                    error = "Stock symbol is invalid";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
