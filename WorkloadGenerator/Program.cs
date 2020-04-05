using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WorkloadGenerator
{
    class Program
    {
        public static string URI { get; private set; }

        static async Task Main(string[] args)
        {
            if (args.Length > 2)
            {
                Console.WriteLine("Usage: WorkloadGenerator.exe <hostip> <filename>");
                return;
            }
            try
            {
                URI = $"http://{args[0]}/api/command";
                HttpClient httpClient = new HttpClient();
                Console.WriteLine($"Attempting Connectiong to {URI}");
                var result = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{URI}?cmd=QUOTE,user1,abc"));

                result.Dispose();
                Console.WriteLine("Connection Success");
            } catch(Exception)
            {
                Console.WriteLine("Server Not Reachable");
                return;
            }

            WorkloadGenerator gen = new WorkloadGenerator();
            bool success = gen.RunAsync(args[1]).Result;

            if (success)
            {
                Console.WriteLine($"Processng Successful");
            } else
            {
                Console.WriteLine($"Processing failed");
            }
        }
    }
}
