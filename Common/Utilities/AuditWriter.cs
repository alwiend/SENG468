using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Utilities
{
    public class AuditWriter
    {
        readonly Socket sender;

        public AuditWriter()
        {
            try
            {
                // Creation TCP/IP Socket to Audit Server
                // using Socket Class Costructor 
                IPAddress ipAddr = Dns.GetHostAddresses(Constants.Server.AUDIT_SERVER.Name).FirstOrDefault();
                sender = new Socket(ipAddr.AddressFamily,
                       SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, Constants.Server.AUDIT_SERVER.Port);

                // Connect Socket to the remote  
                // endpoint using method Connect() 
                sender.Connect(localEndPoint);

                // We print EndPoint information  
                // that we are connected 
                Console.WriteLine("Socket connected to -> {0} ",
                              sender.RemoteEndPoint.ToString());
            } catch (Exception ex)
            {

            }
        }

        // Deconstructor to close port connection
        ~AuditWriter()
        {
            try
            {
                // Close Socket using  
                // the method Close() 
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            } catch (Exception ex)
            {

            }
        }

        public string WriteLine(string message)
        {
            string result = "";

                try
                {

                    // Creation of message that 
                    // we will send to Server 
                    int byteSent = sender.Send(Encoding.ASCII.GetBytes(message));

                    // Data buffer 
                    byte[] quoteReceived = new byte[1024];

                    // We receive the messagge using  
                    // the method Receive(). This  
                    // method returns number of bytes 
                    // received, that we'll use to  
                    // convert them to string 
                    int byteRecv = sender.Receive(quoteReceived);
                    result = Encoding.ASCII.GetString(quoteReceived,
                                                     0, byteRecv);

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
            
            return result;
        }
    }
}
