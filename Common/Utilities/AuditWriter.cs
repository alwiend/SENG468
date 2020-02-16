using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Utilities
{
    public class AuditWriter : IAuditWriter
    {
        /*
         * @param record The event to pass to the Audit server
         */
        public async Task<string> WriteRecord(object record)
        {
            string result = "";

            try
            {
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                await client.ConnectAsync(Constants.Server.AUDIT_SERVER.ServiceName, Constants.Server.AUDIT_SERVER.Port);

                try
                {
                    LogType log = new LogType
                    {
                        Items = new object[] { record }
                    };



                    await Task.Run(() =>
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(LogType));
                        using (StreamWriter client_out = new StreamWriter(client.GetStream()))
                        {
                            serializer.Serialize(client_out, log);
                            // Shutdown Clientside sending to signal end of stream
                            client.Client.Shutdown(SocketShutdown.Both);
                        }
                    });
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
                client.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect: {0}", ex.Message);
            }

            return result;
        }
    }
}
