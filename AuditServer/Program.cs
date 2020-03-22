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
using System.Threading.Tasks;
using System.Xml;
using Constants;
using Utf8Json;
using MessagePack;

namespace AuditServer
{
	class Program
	{
		// Main Method 
		static async Task Main(string[] args)
		{
			AuditServer server = new AuditServer();
			await server.Run();
			server.Dispose();
		}
	}
}
