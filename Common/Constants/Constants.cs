using System;

namespace Constants
{
    public class Server
    {
        public static ServiceConstant QUOTE_SERVER = new ServiceConstant("quoteserve.seng.uvic.ca", 4448);
        public static ServiceConstant AUDIT_SERVER = new ServiceConstant("audit_server", 44439);
    }

    public class Service
    {
        public static ServiceConstant QUOTE_SERVICE = new ServiceConstant("quote_service", 44440);
        public static ServiceConstant ADD_SERVICE = new ServiceConstant("add_service", 44441);
        public static ServiceConstant BUY_SERVICE = new ServiceConstant("buy_service", 44442);
        public static ServiceConstant BUY_COMMIT_SERVICE = new ServiceConstant("buy_service", 44443);
        public static ServiceConstant BUY_CANCEL_SERVICE = new ServiceConstant("buy_service", 44444);

    }
    public class ServiceConstant
    {
        public string Name { get; }
        public int Port { get; }

        public ServiceConstant(string name, int port)
        {
            Name = name;
            Port = port;
        }
    }
}
