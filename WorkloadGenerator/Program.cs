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
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: WorkloadGenerator.exe <hostip> <filename>");
                return;
            }
            try
            {
                URI = $"http://localhost:8080/api/command";
                HttpClient httpClient = new HttpClient();
                Console.WriteLine($"Attempting Connection to {URI}");
                var result = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{URI}?cmd=HealthCheck"));

                result.Dispose();
                Console.WriteLine("Connection Success");
            } catch(Exception)
            {
                Console.WriteLine("Server Not Reachable");
                return;
            }

            WorkloadGenerator gen = new WorkloadGenerator();
            bool success = gen.RunAsync(args[0]).Result;

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
