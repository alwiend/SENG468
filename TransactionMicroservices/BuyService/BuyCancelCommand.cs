using System;
using System.Collections.Generic;
using System.Text;
using Utilities;
using Base;

namespace BuyService
{
    class BuyCancelCommand : BaseService
    {
        public static void Main(string[] args)
        {
            new BuyCancelCommand().StartService();
        }

        public BuyCancelCommand() : base(CancelBuy, 44444)
        {

        }

        public static string CancelBuy(string user)
        {
            string result = "";
            List<string> transaction = BuyCache.GetItemsFromCache(user);
            try
            {
                DB db = new DB();

                int balance = db.ExecuteNonQuery($"SELECT money FROM user WHERE user = {user}");
                int newBalance = balance + Convert.ToInt32(transaction[3]) * 100; 
                db.ExecuteNonQuery($"UPDATE user SET money = {newBalance} WHERE user = {user}");
                result = $"Successfully canceled most recent buy command.\n" +
                    $"Your new balance is {newBalance}";
            }
            catch (Exception e)
            {
                result = "Error occured changing acccount balance";
                Console.WriteLine(e.ToString());
            }
            BuyCache.RemoveItems(user);
            return result;
        }
    }
}
