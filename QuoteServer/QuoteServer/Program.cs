// A C# Program for Server 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Utilities;

namespace QuoteServer
{
    class Program
    {

        static Random rnd = new Random(DateTime.UtcNow.Second);
        // Main Method 
        static async Task Main(string[] args)
        {
            await ExecuteServer().ConfigureAwait(false);
        }

        static async Task ProcessClient(TcpClient client)
        {
            try
            {
                using StreamReader client_in = new StreamReader(client.GetStream());
                var request = await client_in.ReadToEndAsync().ConfigureAwait(false);

                var args = request.Split(",");


                //Cost,StockSymbol,UserId,Timestamp,CryptoKey
                var cost = (rnd.Next(100, 10000))/100.00;
                string retData = $"{cost},{args[0]},{args[1]},{Unix.TimeStamp},CryptoKey{args[0]}{args[1]}{cost}";

                using StreamWriter client_out = new StreamWriter(client.GetStream());
                await client_out.WriteAsync(retData).ConfigureAwait(false);
                await client_out.FlushAsync().ConfigureAwait(false);
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

        static async Task ExecuteServer()
        {
            IPAddress ipAddr = IPAddress.Any;
            IPEndPoint _localEndPoint = new IPEndPoint(ipAddr, 4448);

            TcpListener _listener = new TcpListener(_localEndPoint);
            _listener.Start();
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                ProcessClient(client);
            }
        }
    }
}
