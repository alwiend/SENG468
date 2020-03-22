using Constants;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AuditServer
{
    public class AuditServer : IDisposable
	{
		private static readonly ConcurrentStack<object> Log = new ConcurrentStack<object>();
		private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

		private readonly TcpListener listener;

		public AuditServer()
		{
			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Server.AUDIT_SERVER.Port);

			listener = new TcpListener(localEndPoint);
		}


		public void Dispose()
		{
			_tokenSource.Cancel();
			listener.Stop();
		}

		public async Task Run()
		{
			listener.Start();

			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
				_ = new AuditClient(client).Process();
			}
		}

		public static void AddRecord(object record)
		{
			Log.Push(record);
			if (Log.Count % 10000 == 0)
			{
				Console.WriteLine($"Logs Received: {Log.Count}");
			}
		}

		public static void DumpLog(string filename)
		{
			LogType logs = new LogType
			{
				Items = Log.ToArray()
			};

			var path = Path.Combine(Directory.GetCurrentDirectory(), filename);

			FileStream file = File.Create(path);

			XmlSerializer serializer = new XmlSerializer(typeof(LogType));
			serializer.Serialize(file, logs);
			file.Close();
		}
	}
}
