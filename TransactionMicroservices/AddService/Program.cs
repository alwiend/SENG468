// Connects to Quote Server and returns quote
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Base;
using Utilities;

namespace AddService
{

    public class AddService : BaseService
    {
        public static void Main(string[] args)
        {
            new AddService().StartService();
        }

        public AddService() : base(AddMoney, 44441)
        {
        }

        // ExecuteClient() Method 
        static string AddMoney(string user_money)
        {
            string result = "";
            string[] args = user_money.Split(",");
            string user = args[0];
            int money = args[1].Contains(".") ? (int)(double.Parse(args[1])*100) : int.Parse(args[1]);
            Console.WriteLine($"Inserting {money} cents into {user}'s account");
            try
            {
                DB db = new DB();
                

                db.ExecuteNonQuery($"INSERT INTO user (userid, money) VALUES ('{user}',{money})");
                result = $"Successfully inserted {money} cents into {user}'s account";
                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                result = "Error occured adding money";
                Console.WriteLine(e.ToString());
            }
            return result;
        }
    }
}