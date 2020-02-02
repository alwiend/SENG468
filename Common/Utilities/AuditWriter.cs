using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Utilities
{
    public class AuditWriter : IAuditWriter
    {
        /*
         * @param record The event to pass to the Audit server
         */
        public string WriteRecord(object record)
        {
            string result = "";

            try
            {
                // Creation TCP/IP Client to Audit Server
                
                IPAddress ipAddr = Dns.GetHostAddresses(Constants.Server.AUDIT_SERVER.Name).FirstOrDefault();
                //var ipAddr = IPAddress.Loopback; Local Testing
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, Constants.Server.AUDIT_SERVER.Port);

                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                client.Connect(localEndPoint);

                try
                {
                    StreamWriter client_out = new StreamWriter(client.GetStream())
                    {
                        AutoFlush = false
                    };
                    StreamReader client_in = new StreamReader(client.GetStream());

                    LogType log = new LogType
                    {
                        Items = new object[] { record }
                    };
                    XmlSerializer serializer = new XmlSerializer(typeof(LogType));

                    // XML Serialize the log
                    serializer.Serialize(client_out, log);
                    client_out.Flush();
                    // Shutdown Clientside sending to signal end of stream
                    // Get result
                    client.Client.Shutdown(SocketShutdown.Send);
                    result = client_in.ReadToEnd();

                    client_out.Close();
                    client_in.Close();
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
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect: {0}", ex.Message);
            }

            return result;
        }
    }
}
