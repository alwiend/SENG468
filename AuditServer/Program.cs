// A C# Program for Server 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;

namespace AuditServer
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
			List<Record> records = new List<Record>();

			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 44439);

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
					Record record = new Record() { Message = Encoding.ASCII.GetString(bytes, 0, numByte) };

					if (record.Message == "DUMPLOG")
					{
						XmlSerializer xmlSerializer = new XmlSerializer(typeof(Record[]));
						using StringWriter textWriter = new StringWriter();
						xmlSerializer.Serialize(textWriter, records.ToArray());
						clientSocket.Send(Encoding.ASCII.GetBytes(textWriter.ToString()));
					} else
					{
						Console.WriteLine($"{record.RecordTime}: {record.Message}");
						records.Add(record);
					}


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
