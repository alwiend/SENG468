using Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Utilities
{
    public class ServiceConnection
    {
        IPEndPoint localEndPoint;

        public ServiceConnection(ServiceConstant sc)
        {
            var ipAddr = Dns.GetHostAddresses(sc.ServiceName).FirstOrDefault();
            localEndPoint = new IPEndPoint(ipAddr, sc.Port);
        }

        public string Send(string data, bool receive = false)
        {
            string result = "";
            try
            {
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                client.Connect(localEndPoint);
                StreamReader client_in = null;
                StreamWriter client_out = null;
                try
                {
                    client_in = new StreamReader(client.GetStream());
                    client_out = new StreamWriter(client.GetStream());

                    client_out.Write(data);
                    client_out.Flush();
                    client.Client.Shutdown(SocketShutdown.Send);

                    if (receive)
                    {
                        result = client_in.ReadToEnd();
                    }
                    client.Client.Shutdown(SocketShutdown.Receive);
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
                client.Client.Close();
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
