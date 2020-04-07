using Newtonsoft.Json;
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
    public class TransactionCommand
    {
        public string Command;
    }
    public class WorkloadGenerator
    {
        readonly Dictionary<string, Queue<string>> userCommands = new Dictionary<string, Queue<string>>();

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
                        //userCommands[args[1]].Enqueue(JsonConvert.SerializeObject(new TransactionCommand() { Command = s }));
                        userCommands[args[1]].Enqueue(s);
                    }
                }
            }
            Console.WriteLine($"Parsing Complete");

            Console.WriteLine("Spawn User Tasks");

            var executors = userCommands.Select(userCmds => new WorkloadExecutor(userCmds.Key, userCmds.Value));

            Console.WriteLine("Wait For User Tasks");

            DateTime start = DateTime.Now;
            await Task.WhenAll(executors.Select(executor => executor.ExecuteWorkload()));

            Console.WriteLine($"Ran for {(DateTime.Now - start).TotalSeconds} seconds");
            var finalQueue = new Queue<string>();
            //finalQueue.Enqueue(JsonConvert.SerializeObject(new TransactionCommand() { Command = finalCommand }));
            finalQueue.Enqueue(finalCommand);
            await new WorkloadExecutor("Dumplog", finalQueue).ExecuteWorkload().ConfigureAwait(false);

            return true;
        }       
    }
}
