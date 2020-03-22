using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Utilities;

namespace AuditServer
{
    public class AuditServer : IDisposable
	{
		private static readonly ConcurrentStack<object> Log = new ConcurrentStack<object>();

		public async Task DisplayLogs()
		{
			while(!_tokenSource.IsCancellationRequested)
			{
				Console.WriteLine($"Logs Received: {Log.Count}");
				
				await Task.Delay(5000).ConfigureAwait(false);
			}
		}

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
		}

		public static void AddBulkRecords(object[] records)
		{
			Log.PushRange(records);
		}

		public static void DumpLog(string filename)
		{
			LogType logs = new LogType
			{
				Items = Log.Reverse().ToArray()
			};

			var path = Path.Combine(Directory.GetCurrentDirectory(), filename);

			FileStream file = File.Create(path);

			XmlSerializer serializer = new XmlSerializer(typeof(LogType));
			serializer.Serialize(file, logs);
			file.Close();
		}
	}
}
