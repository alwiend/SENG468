using System;

namespace Constants
{
    public class Server
    {
        public readonly static ServiceConstant QUOTE_SERVER = new ServiceConstant("quoteserve.seng.uvic.ca", 4448, "QSVR1");
        public readonly static ServiceConstant AUDIT_SERVER = new ServiceConstant("audit_server", 44439, "ASVR1");
        public readonly static ServiceConstant WEB_SERVER = new ServiceConstant("web_server", 80, "WSVR1");
    }

    public class Service
    {
        public readonly static ServiceConstant QUOTE_SERVICE = new ServiceConstant("quote_service", 44440, "QSVC1");
        public readonly static ServiceConstant ADD_SERVICE = new ServiceConstant("add_service", 44441, "ASVC1");
        public readonly static ServiceConstant BUY_SERVICE = new ServiceConstant("buy_service", 44442, "BSVC1");
        public readonly static ServiceConstant BUY_CANCEL_SERVICE = new ServiceConstant("buy_service", 44443, "BSVC2");
        public readonly static ServiceConstant BUY_COMMIT_SERVICE = new ServiceConstant("buy_service", 44444, "BSVC3");
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
