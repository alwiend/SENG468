using System;

namespace WorkloadGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: WorkloadGenerator.exe <filename>");
                return;
            }
            WorkloadGenerator gen = new WorkloadGenerator();
            DateTime start = DateTime.Now;
            bool success = gen.RunAsync(args[0]).Result;
            Console.WriteLine($"Ran for {(DateTime.Now-start).TotalSeconds} seconds");

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
