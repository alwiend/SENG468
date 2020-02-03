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
using System.Threading;

namespace AuditServer
{
	class Program
	{
		private static List<object> Log { get; set; }
		// Main Method 
		static void Main(string[] args)
		{
			ExecuteServer();
		}

		private static void ProcessIncoming(TcpClient client)
		{
			UserCommandType command = null;
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(LogType));
				using StreamReader server_in = new StreamReader(client.GetStream());
				var log_in = (LogType)serializer.Deserialize(server_in);

				for (int i = 0; i < log_in.Items.Length; i++)
				{
					var record = log_in.Items[i];
					Log.Add(record);
					if (record.GetType() == typeof(UserCommandType))
					{
						command = (UserCommandType)record;
						if (command.command == commandType.DUMPLOG)
						{
							LogType logs = new LogType
							{
								Items = Log.ToArray()
							};

							var path = Path.Combine(Directory.GetCurrentDirectory(), command.filename);

							FileStream file = File.Create(path);

							serializer.Serialize(file, logs);
							file.Close();
						}
					}
				}
				client.Close();
				client.Dispose();
			}

			catch (Exception e)
			{
				DebugType debugEvent = new DebugType
				{
					server = "ASVR1",
					debugMessage = e.Message
				};
				if(command != null)
				{
					debugEvent.command = command.command;
					debugEvent.transactionNum = command.transactionNum;
					debugEvent.username = command.username;
				}
				Log.Add(debugEvent);
			}

		}

		public static void ExecuteServer()
		{
			Program.Log = new List<object>();

			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 44439);

			// Creation TCP/IP Socket using 
			// Socket Class Costructor 
			TcpListener listener = new TcpListener(localEndPoint);
			listener.Start(100);

			while (true)
			{
				TcpClient client = listener.AcceptTcpClient();
				Thread thr = new Thread(new ThreadStart(() => ProcessIncoming(client)));
				thr.Start();
			}
		}
	}
}
