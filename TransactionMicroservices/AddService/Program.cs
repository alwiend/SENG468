// Connects to Quote Server and returns quote
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Base;
using Database;
using Newtonsoft.Json;

namespace AddService
{

    public class AddService : BaseService
    {
        public static void Main(string[] args)
        {
            var add_service = new AddService();
            add_service.StartService(add_service.AddMoney);
        }

        public AddService() : base(44441)
        {
        }

        // ExecuteClient() Method 
        string AddMoney(string user_money)
        {
            string result;
            string[] args = user_money.Split(",");
            string user = args[0];
            int money = (int)(double.Parse(args[1])*100);
            Auditor.WriteLine($"Inserting {money} cents into {user}'s account");
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{user}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                string query = $"INSERT INTO user (userid, money) VALUES ('{user}',{money})";
                if (userObject.Length > 0)
                {
                    query = $"UPDATE user SET money={money} WHERE userid='{user}'";
                }

                db.ExecuteNonQuery(query);
                result = $"Successfully added {money} cents into {user}'s account";
                Auditor.WriteLine(result);
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