// A C# Program for Server 
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuditServer
{
	class Program
	{
		// Main Method 
		static async Task Main()
		{
			ThreadPool.SetMinThreads(Environment.ProcessorCount * 15, Environment.ProcessorCount * 10);
			AuditServer server = new AuditServer();
			_ = server.DisplayLogs();
			await server.Run();
			server.Dispose();
		}
	}
}
