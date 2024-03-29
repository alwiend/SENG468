﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace WorkloadGenerator
{

    class WorkloadExecutor
    {
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
            string cmd;
            while (commands.Count > 0)
            {
                // Run user commands as long as there is no errors
                cmd = commands.Dequeue();
                await SendToWebServer(cmd).ConfigureAwait(false);
            }
        }

        async Task<bool> SendToWebServer(string command)
        {
            var start = DateTime.Now;
            var success = false;
            try
            {
                var result = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{Program.URI}?cmd={command}"));
               
                success = result.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable;
                if (!result.IsSuccessStatusCode)
                    Console.WriteLine(result.StatusCode);
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
            return success;
        }


        async Task PostToWebServer(string command)
        {
            var start = DateTime.Now;
            try
            {
                //var content = new StringContent(command, Encoding.UTF8, "application/json");
                
                var result = await httpClient.GetAsync($"{Program.URI}?cmd={command}");
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
