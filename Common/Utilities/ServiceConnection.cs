using Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Utilities
{
    public class ServiceConnection
    {
        readonly IPEndPoint localEndPoint;

        public ServiceConnection(ServiceConstant sc)
            : this(Dns.GetHostAddresses(sc.ServiceName).FirstOrDefault(), sc.Port)
        {
        }

        public ServiceConnection(IPAddress addr, int port)
        {
            localEndPoint = new IPEndPoint(addr, port);
        }

        public async Task<string> Send(UserCommandType userCommand, bool receive = false)
        {
            string result = "";
            try
            {
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                await client.ConnectAsync(localEndPoint.Address, localEndPoint.Port).ConfigureAwait(false);

                await Task.Run(async () =>
                {
                    StreamReader client_in = null;
                    try
                    {
                        client_in = new StreamReader(client.GetStream());

                        XmlSerializer serializer = new XmlSerializer(typeof(UserCommandType));
                        using StreamWriter client_out = new StreamWriter(client.GetStream());
                        serializer.Serialize(client_out, userCommand);
                        await client_out.FlushAsync().ConfigureAwait(false);
                        client.Client.Shutdown(SocketShutdown.Send);

                        if (receive)
                        {
                            result = await client_in.ReadToEndAsync().ConfigureAwait(false);
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
                }).ConfigureAwait(false);
            }

            catch (Exception e)
            {
                return null;
            }
            return result;
        }

        public async Task<string> Send(string data, bool receive = false)
        {
            string result = "";
            try
            {
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                await client.ConnectAsync(localEndPoint.Address, localEndPoint.Port).ConfigureAwait(false);
                StreamReader client_in = null;
                StreamWriter client_out = null;
                try
                {
                    client_in = new StreamReader(client.GetStream());
                    client_out = new StreamWriter(client.GetStream());

                    await client_out.WriteAsync(data).ConfigureAwait(false);
                    await client_out.FlushAsync().ConfigureAwait(false);
                    client.Client.Shutdown(SocketShutdown.Send);

                    if (receive)
                    {
                        result = await client_in.ReadToEndAsync().ConfigureAwait(false);
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
