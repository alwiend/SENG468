using System;

namespace Constants
{
    public class Server
    {
        public readonly static ServiceConstant QUOTE_SERVER = new ServiceConstant("quoteserve.seng.uvic.ca", 4448, "QSVR");
        public readonly static ServiceConstant AUDIT_SERVER = new ServiceConstant("audit_server", 44439, "ASVR");
        public readonly static ServiceConstant WEB_SERVER = new ServiceConstant("web_server", 80, "WSVR");
    }

    public class Service
    {
        public readonly static ServiceConstant QUOTE_SERVICE = new ServiceConstant("quote_service", 44440, "QSVC");
        public readonly static ServiceConstant ADD_SERVICE = new ServiceConstant("add_service", 44441, "ASVC");
        public readonly static ServiceConstant BUY_SERVICE = new ServiceConstant("buy_service", 44442, "BSVC");
        public readonly static ServiceConstant BUY_CANCEL_SERVICE = new ServiceConstant("buy_service", 44443, "BSVC");
        public readonly static ServiceConstant BUY_COMMIT_SERVICE = new ServiceConstant("buy_service", 44444, "BSVC");
        public readonly static ServiceConstant DISPLAY_SUMMARY_SERVICE = new ServiceConstant("display_summary_service", 44445, "DSVC");
        public readonly static ServiceConstant SELL_SERVICE = new ServiceConstant("sell_service", 44446, "SSVC");
        public readonly static ServiceConstant SELL_CANCEL_SERVICE = new ServiceConstant("sell_service", 44447, "SSVC");
        public readonly static ServiceConstant SELL_COMMIT_SERVICE = new ServiceConstant("sell_service", 44448, "SSVC");
        public readonly static ServiceConstant BUY_TRIGGER_AMOUNT_SERVICE = new ServiceConstant("buy_trigger_service", 44449, "BTSVC");
        public readonly static ServiceConstant BUY_TRIGGER_CANCEL_SERVICE = new ServiceConstant("buy_trigger_service", 44450, "BTSVC");
        public readonly static ServiceConstant BUY_TRIGGER_SET_SERVICE = new ServiceConstant("buy_trigger_service", 44451, "BTSVC");
        public readonly static ServiceConstant SELL_TRIGGER_AMOUNT_SERVICE = new ServiceConstant("sell_trigger_service", 44452, "STSVC");
        public readonly static ServiceConstant SELL_TRIGGER_CANCEL_SERVICE = new ServiceConstant("sell_trigger_service", 44453, "STSVC");
        public readonly static ServiceConstant SELL_TRIGGER_SET_SERVICE = new ServiceConstant("sell_trigger_service", 44454, "STSVC");
    }

    public class ServiceConstant
    {
        public string ServiceName { get; }
        public int Port { get; }

        public string Abbr { get; }

        public ServiceConstant(string sn, int port, string abbr)
        {
            ServiceName = sn;
            Port = port;
            Abbr = abbr;
        }
    }
}
