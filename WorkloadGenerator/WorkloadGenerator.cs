using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WorkloadGenerator
{
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
                        userCommands[args[1]].Enqueue(s);
                    }
                }
            }
            Console.WriteLine("Parsing Complete");

            Console.WriteLine("Spawn User Tasks");
            List<Task> tasks = new List<Task>(userCommands.Count);
            foreach (string user in userCommands.Keys)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    return ExecuteUserCommands(userCommands[user]);
                }));
            }
            Console.WriteLine("Spawning Complete");

            Console.WriteLine("Wait For Completion");
            await Task.WhenAll(tasks);
            PostToWebServer(finalCommand);

            return true;
        }

        private bool ExecuteUserCommands(Queue<string> commands)
        {
            bool success = true;
            while (commands.Count > 0 && success)
            {
                // Run user commands as long as there is no errors
                success = success && PostToWebServer(commands.Dequeue());
            }
            return success;
        }

        bool PostToWebServer(string command)
        {
            WebRequest request = WebRequest.Create("http://localhost:8080");
            request.ContentType = "multipart/form-data;";
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            try
            {
                byte[] byteArray = Encoding.UTF8.GetBytes($"Command={HttpUtility.UrlEncode(command)}");

                // Set the ContentLength property of the WebRequest.  
                request.ContentLength = byteArray.Length;

                // Get the request stream.  
                Stream dataStream = request.GetRequestStream();
                // Write the data to the request stream.  
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.  
                dataStream.Close();

                // Get the response.  
                WebResponse response = request.GetResponse();
                // Cancel if a command failed
                if (((HttpWebResponse)response).StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                // Close the response.  
                response.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
