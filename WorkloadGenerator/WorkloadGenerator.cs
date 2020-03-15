using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WorkloadGenerator
{
    public class WorkloadGenerator
    {
        readonly Dictionary<string, Queue<string>> userCommands = new Dictionary<string, Queue<string>>();
        readonly HttpClient httpClient = new HttpClient();

        public async Task<bool> RunAsync(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"{filename} does not exist");
                return false;
            }

            string finalCommand = "";

            Console.WriteLine("Parsing File");
            using (FileStream fs = File.Open(filename, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    // Remove line numbers and leading/trailing whitespace
                    s = s.Substring(s.IndexOf(']') + 1).Trim();
                    string[] args = s.Split(',');
                    if (args[0] == "DUMPLOG" && args.Length == 2)
                    {
                        finalCommand = s;
                    }
                    else
                    {
                        if (!userCommands.ContainsKey(args[1]))
                        {
                            userCommands.Add(args[1], new Queue<string>());
                        }
                        userCommands[args[1]].Enqueue(s);
                    }
                }
            }
            Console.WriteLine($"Parsing Complete");

            Console.WriteLine("Spawn User Tasks");

            DateTime start = DateTime.Now;
            var tasks = userCommands.Select(userCmds => ExecuteUserCommands(userCmds.Value));

            Console.WriteLine("Wait For User Tasks");

            await Task.WhenAll(tasks);

            Console.WriteLine($"Ran for {(DateTime.Now - start).TotalSeconds} seconds");
            await PostToWebServer(finalCommand);

            return true;
        }

        public async Task<bool> RunAsync()
        {
            Console.WriteLine("Spawn AsyncTest Tasks");
            for (int i = 0; i < 1000; i++)
            {
                Queue<string> queue = new Queue<string>();
                for (int j = 0; j < 1; j++)
                {
                    queue.Enqueue("AsyncTest");
                }
                userCommands.Add($"user{i}", queue);
            }

            Console.WriteLine("Spawning Complete");
            Console.WriteLine("Wait For Completion");
            DateTime start = DateTime.Now;
            await Task.WhenAll(userCommands.Select(userCmds => Task.Run(async () =>
            {
                await ExecuteUserCommands(userCmds.Value);
            })));

            Console.WriteLine($"Ran for {(DateTime.Now - start).TotalSeconds} seconds");
            return true;
        }

        private async Task ExecuteUserCommands(Queue<string> commands)
        {
            while (commands.Count > 0)
            {
                // Run user commands as long as there is no errors
                await PostToWebServer(commands.Dequeue());
            }
        }

        async Task PostToWebServer(string command)
        {
            var start = DateTime.Now;
            try
            {
                string URI = "http://localhost:8080";
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Command", command)
                });
                var result = await httpClient.PostAsync(URI, formContent);
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
