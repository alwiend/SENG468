// A C# Program for Server 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace QuoteServer
{
	class Program
	{

		// Main Method 
		static void Main(string[] args)
		{
			ExecuteServer();
		}

		public static void ExecuteServer()
		{
			// A list of quotes
			Dictionary<string, Quote> quotes = new Dictionary<string, Quote>();

			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 4448);

			// Creation TCP/IP Socket using 
			// Socket Class Costructor 
			Socket listener = new Socket(IPAddress.Any.AddressFamily,
						SocketType.Stream, ProtocolType.Tcp);

			try
			{

				// Using Bind() method we associate a 
				// network address to the Server Socket 
				// All client that will connect to this 
				// Server Socket must know this network 
				// Address 
				listener.Bind(localEndPoint);

				// Using Listen() method we create 
				// the Client list that will want 
				// to connect to Server 
				listener.Listen(10);

				while (true)
				{

					Console.WriteLine($"Waiting connection on {localEndPoint.Address}:{localEndPoint.Port} address family {listener.AddressFamily}... ");

					// Suspend while waiting for 
					// incoming connection Using 
					// Accept() method the server 
					// will accept connection of client 
					Socket clientSocket = listener.Accept();

					Console.WriteLine("Client connected");

					// Data buffer 
					byte[] bytes = new Byte[1024];

					// Message expected to just receive quote name
					// Quote names are case sensitive for simplicity
					int numByte = clientSocket.Receive(bytes);
					string name = Encoding.ASCII.GetString(bytes, 0, numByte);

					// Returns the quote cost
					if (!quotes.ContainsKey(name))
					{
						quotes.Add(name, new Quote(name));
					}
					double cost = quotes[name].Cost;
					Console.WriteLine($"Quoting {name}: ${cost}");

					// Send a message to Client 
					// using Send() method 
					clientSocket.Send(Encoding.ASCII.GetBytes(cost.ToString()));

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
	}
}
