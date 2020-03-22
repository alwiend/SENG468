using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace WorkloadGenerator
{

    class WorkloadExecutor
    {
        readonly string URI = "http://localhost:8080/api/command";
        public string User { get; }
        public int Remaining 
        {
            get
            {
                return commands.Count;
            } 
        }

        readonly int total;

        readonly Queue<string> commands;
        readonly HttpClient httpClient = new HttpClient();

        public WorkloadExecutor(string user, Queue<string> cmds)
        {
            User = user;
            commands = cmds;
            total = commands.Count;
        }

        public async Task ExecuteWorkload()
        {
            while (commands.Count > 0)
            {
                // Run user commands as long as there is no errors
                await SendToWebServer(commands.Dequeue()).ConfigureAwait(false);
            }
        }

        async Task SendToWebServer(string command)
        {
            var start = DateTime.Now;
            try
            {
                var result = await httpClient.GetAsync($"{URI}?cmd={command}");
                result.Dispose();
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"-------------------Command: {command}-------------------");
                Console.WriteLine($"Cancel requested: {ex.CancellationToken.IsCancellationRequested}");
                Console.WriteLine($"Took {(DateTime.Now - start).TotalSeconds} seconds to cancel");
                Console.WriteLine(ex);
                Console.WriteLine("--------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-------------------Command: {command}-------------------");
                Console.WriteLine($"Took {(DateTime.Now - start).TotalSeconds} seconds to throw exception");
                Console.WriteLine(ex);
                Console.WriteLine("--------------------------------------------------------");
            }
        }


        async Task PostToWebServer(string command)
        {
            var start = DateTime.Now;
            try
            {
                var content = new StringContent(command, Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(URI, content);
                result.Dispose();
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"-------------------Command: {command}-------------------");
                Console.WriteLine($"Cancel requested: {ex.CancellationToken.IsCancellationRequested}");
                Console.WriteLine($"Took {(DateTime.Now - start).TotalSeconds} seconds to cancel");
                Console.WriteLine(ex);
                Console.WriteLine("--------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-------------------Command: {command}-------------------");
                Console.WriteLine($"Took {(DateTime.Now - start).TotalSeconds} seconds to throw exception");
                Console.WriteLine(ex);
                Console.WriteLine("--------------------------------------------------------");
            }
        }
    }
}
