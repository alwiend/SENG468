// A C# Program for Server 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Base
{
	public delegate string DataReceivedEvent(string data);

	public abstract class BaseService
	{
		IPEndPoint _localEndPoint;
		Socket _listener;
		bool _running;
		public DataReceivedEvent DataReceived;

		public BaseService(DataReceivedEvent dr, int port)
		{
			DataReceived += dr;

			// Establish the local endpoint 
			// for the socket. Dns.GetHostName 
			// returns the name of the host 
			// running the application. 
			IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddr = IPAddress.Any;
			_localEndPoint = new IPEndPoint(ipAddr, port);

			// Creation TCP/IP Socket using 
			// Socket Class Costructor 
			_listener = new Socket(ipAddr.AddressFamily,
						SocketType.Stream, ProtocolType.Tcp);
		}

		public void StartService(int maxClients = 10)
		{
			try
			{
				// Using Bind() method we associate a 
				// network address to the Server Socket 
				// All client that will connect to this 
				// Server Socket must know this network 
				// Address 
				_listener.Bind(_localEndPoint);

				// Using Listen() method we create 
				// the Client list that will want 
				// to connect to Server 
				_listener.Listen(maxClients);
				_running = true;
				while (_running)
				{
					Console.WriteLine($"Waiting connection on {_localEndPoint.Address}:{_localEndPoint.Port} address family {_listener.AddressFamily}... ");

					// Suspend while waiting for 
					// incoming connection Using 
					// Accept() method the server 
					// will accept connection of client 
					Socket clientSocket = _listener.Accept();

					Console.WriteLine("Client connected");

					// Data buffer 
					byte[] bytes = new Byte[1024];

					// Message expected to just receive quote name
					// Quote names are case sensitive for simplicity
					int numByte = clientSocket.Receive(bytes);
					string recData = Encoding.ASCII.GetString(bytes, 0, numByte);

					string retData = DataReceived(recData);
					
					// Send a message to Client 
					// using Send() method 
					clientSocket.Send(Encoding.ASCII.GetBytes(retData.ToString()));

					// Close client Socket using the 
					// Close() method. After closing, 
					// we can use the closed Socket 
					// for a new Client Connection 
					clientSocket.Shutdown(SocketShutdown.Both);
					clientSocket.Close();
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		public void StopService()
		{
			_running = false;
		}
	}
}
