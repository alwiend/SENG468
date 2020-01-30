using System;
using System.Collections.Generic;
using System.Text;
using Base;

namespace BuyService
{
    class BuyCommitCommand : BaseService
    {
        
        public static void Main(string[] args)
        {
            new BuyCommitCommand().StartService();
        }

        public BuyCommitCommand() : base(CommitBuy, 44443)
        {

        }

        public static string CommitBuy(string user)
        {
            List<string> transaction = BuyCache.GetItemsFromCache(user);
            // Add the amount of stock to stock database from cache
            BuyCache.RemoveItems(user);
            return $"Successfully bought {transaction[3]} worth of {transaction[1]} at {transaction[2]} per share.";
        } 
    }
}
