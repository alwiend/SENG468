using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Utilities
{
    public class ConnectedServices : IDisposable
    {
        readonly Dictionary<string, ServiceConnection> connections = new Dictionary<string, ServiceConnection>();

        public void Dispose()
        {
            connections.Values.ToList().ForEach(conn => conn.Dispose());
        }

        public async Task<ServiceConnection> GetServiceConnectionAsync(ServiceConstant sc)
        {
            if (connections.TryGetValue(sc.UniqueName, out ServiceConnection conn))
            {
                if (conn.Connected)
                    return conn;
                Console.WriteLine($"{sc.UniqueName} connection dead");
                conn.Dispose(); // Service connection was lost, dispose the abandoned connection and restart
                connections.Remove(sc.UniqueName);
            }

            for (int i = 0; i < 3; i++)
            {
                connections.Add(sc.UniqueName, new ServiceConnection(sc));
                if (await connections[sc.UniqueName].ConnectAsync()) return connections[sc.UniqueName];
                Console.WriteLine($"{sc.UniqueName} connection failed");
                await Task.Delay(1000 * (i+1)*(i+1));
                conn.Dispose(); // Service connection was lost, dispose the abandoned connection and restart
                connections.Remove(sc.UniqueName);
            }

            return null;
        }
    }
}
