// A C# Program for Server 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using Constants;

namespace Base
{
	public delegate object DataReceivedEvent(UserCommandType command);

	public abstract class BaseService
	{
		protected IAuditWriter Auditor { get; }
		protected DataReceivedEvent DataReceived;
		protected ServiceConstant ServiceDetails { get; }

		public BaseService(ServiceConstant sc, IAuditWriter aw)
		{
			ServiceDetails = sc;
			Auditor = aw;
		}

		/*
		 * Logs interserver communication
		 * @param command The user command that is driving the process
		 */
		void LogServerEvent(UserCommandType command)
		{
			SystemEventType sysEvent = new SystemEventType()
			{
				timestamp = Unix.TimeStamp.ToString(),
				server = ServiceDetails.Abbr,
				transactionNum = command.transactionNum,
				username = command.username,
				fundsSpecified = command.fundsSpecified,
				command = command.command,
				filename = command.filename,
				stockSymbol = command.stockSymbol
			};
			if (command.fundsSpecified)
				sysEvent.funds = command.funds;
			Auditor.WriteRecord(sysEvent);
		}

		private void ProcessIncoming(TcpClient client)
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(UserCommandType));

				using StreamReader client_in = new StreamReader(client.GetStream());
				UserCommandType command = (UserCommandType)serializer.Deserialize(client_in);
				LogServerEvent(command);

				object retData = DataReceived(command).ToString();

				using StreamWriter client_out = new StreamWriter(client.GetStream());
				client_out.Write(retData);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				client.Close();
			}
		}

		public void StartService()
		{
			IPAddress ipAddr = IPAddress.Any;
			IPEndPoint _localEndPoint = new IPEndPoint(ipAddr, ServiceDetails.Port);

			TcpListener _listener = new TcpListener(_localEndPoint);
			_listener.Start(100);
			while (true)
			{
				Console.WriteLine($"Waiting connection on {_localEndPoint.Address}:{_localEndPoint.Port} ");

				TcpClient client = _listener.AcceptTcpClient();
				Thread thr = new Thread(new ThreadStart(() => ProcessIncoming(client)));
				thr.Start();
			}
		}
	}
}
