using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Base;
using Database;
using Newtonsoft.Json;
using Utilities;
using Constants;

namespace BuyService
{
    class BuyCommand : BaseService
    {
        public BuyCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw) 
        {
            DataReceived = BuyStock;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "buy",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        public string BuyStock(UserCommandType command)
        {
            string result;
            string stockCost = GetStock(command.stockSymbol);
            long balance;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT userid, money FROM user WHERE userid='{command.username}'");
                var userObject = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObject.Length <= 0)
                {
                    return "Insufficient balance";
                }

                balance = long.Parse(userObject[0]["money"]) / 100; // normalize
                decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost)); // most whole number stock that can buy

                if (balance < command.funds)
                {
                    return $"Not enough money to buy {command.funds} worth of {command.stockSymbol}.";
                }

                double amount = (double)numStock * double.Parse(stockCost); // amount to send to pending transactions
                double leftover = balance - amount;
                leftover *= 100; // Get rid of decimal
                amount *= 100; // Get rid of decimal

                // Store pending transaction
                db.ExecuteNonQuery($"INSERT INTO transactions (userid, stock, price, transType, transTime) VALUES ('{command.username}','{command.stockSymbol}',{amount},'BUY','{Unix.TimeStamp.ToString()}')");

                result = $"{numStock} stock is available for purchase at {stockCost} per share totalling {amount/100}.";

                // Update the amount in user account
                db.ExecuteNonQuery($"UPDATE user SET money = {leftover} WHERE userid='{command.username}'");

                command.funds = (long)leftover;
                LogTransactionEvent(command);
                }
                catch (Exception e)
                {
                    result = $"Error occured getting account details.";
                    Console.WriteLine(e.ToString());
                }
            return result;
        }

        //public static void CacheBuyRequest(double amount, string stock, string stockCost, string user)
        //{
        //    string transaction = $"{user},{stock},{stockCost},{Convert.ToString(amount)}";
        //    BuyCache.StoreItemsInCache(user, transaction);
        //}

        public static string GetStock(string stock)
        {
            string cost = "";
            try
            {
                // Establish the remote endpoint  
                // for the socketstring howtogeek = "www.google.com";
                IPAddress[] addresslist = Dns.GetHostAddresses("quoteserve.seng.uvic.ca");
                var ipAddr = addresslist.FirstOrDefault();
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 4448);

                // Creation TCP/IP Socket using  
                // Socket Class Costructor 
                Socket sender = new Socket(ipAddr.AddressFamily,
                           SocketType.Stream, ProtocolType.Tcp);

                try
                {

                    // Connect Socket to the remote  
                    // endpoint using method Connect() 
                    sender.Connect(localEndPoint);

                    // We print EndPoint information  
                    // that we are connected 
                    Console.WriteLine("Socket connected to -> {0} ",
                                  sender.RemoteEndPoint.ToString());

                    // Creation of message that 
                    // we will send to Server 
                    int byteSent = sender.Send(Encoding.ASCII.GetBytes(stock));

                    // Data buffer 
                    byte[] quoteReceived = new byte[1024];

                    // We receive the messagge using  
                    // the method Receive(). This  
                    // method returns number of bytes 
                    // received, that we'll use to  
                    // convert them to string 
                    int byteRecv = sender.Receive(quoteReceived);
                    cost = Encoding.ASCII.GetString(quoteReceived,
                                                     0, byteRecv);
                    Console.WriteLine($"Quote: {stock}\nCost: ${cost}");

                    // Close Socket using  
                    // the method Close() 
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
            return cost;
        }
    }
}
