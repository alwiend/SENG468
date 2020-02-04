﻿// Connects to Quote Server and returns quote
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Base;
using Constants;
using Database;
using Newtonsoft.Json;
using Utilities;

namespace AddService
{

    public class AddService : BaseService
    {
        public static void Main(string[] args)
        {
            var add_service = new AddService(Service.ADD_SERVICE, new AuditWriter());
            add_service.StartService();
        }

        public AddService (ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = AddMoney;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "add",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        // ExecuteClient() Method 
        object AddMoney(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                string query = $"INSERT INTO user (userid, money) VALUES ('{command.username}',{command.funds})";
                long funds = (long)(command.funds * 100);
                if (userObject.Length > 0)
                {
                    funds += long.Parse(userObject[0]["money"]);
                    query = $"UPDATE user SET money={funds} WHERE userid='{command.username}'";
                }

                db.ExecuteNonQuery(query);
                result = $"Successfully added {command.funds} into {command.username}'s account";
                command.funds = Convert.ToDecimal(funds) / 100.0m;

                LogTransactionEvent(command);
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