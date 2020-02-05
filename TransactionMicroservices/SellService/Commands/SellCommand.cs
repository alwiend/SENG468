using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Base;
using Database;
using Newtonsoft.Json;
using Utilities;
using Constants;

namespace SellService
{
    class SellCommand : BaseService
    {
        public SellCommand(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
            DataReceived = SellStock;
        }

        void LogTransactionEvent(UserCommandType command)
        {
            AccountTransactionType transaction = new AccountTransactionType()
            {
                timestamp = Unix.TimeStamp.ToString(),
                server = ServiceDetails.Abbr,
                transactionNum = command.transactionNum,
                action = "sell",
                username = command.username,
                funds = command.funds
            };
            Auditor.WriteRecord(transaction);
        }

        object SellStock(UserCommandType command)
        {
            string result;
            string stockCost = GetStock(command.stockSymbol);
            double stockBalance;
            try
            {
                MySQL db = new MySQL();
                var hasUser = db.Execute($"SELECT price FROM stocks WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                var userObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasUser);
                if (userObj.Length > 0)
                {
                    stockBalance = double.Parse(userObj[0]["price"]) / 100;
                    if (stockBalance < (double)command.funds)
                    {
                        return $"Insufficient amount of stock {command.stockSymbol} for this transaction";
                    }
                    decimal numStock = Math.Floor(command.funds / decimal.Parse(stockCost));
                    double amount = Math.Round((double)numStock * double.Parse(stockCost), 2);
                    double leftoverStock = stockBalance - amount;

                    var hasMoney = db.Execute($"SELECT money FROM user WHERE userid='{command.username}'");
                    var moneyObj = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(hasMoney);
                    db.ExecuteNonQuery($"INSERT INTO transactions (userid, stock, price, transType, transTime) VALUES ('{command.username}','{command.stockSymbol}',{amount*100},'SELL','{Unix.TimeStamp}')");
                    result = $"${amount} of stock {command.stockSymbol} is available to sell at ${stockCost} per share";
                    db.ExecuteNonQuery($"UPDATE stocks SET price={leftoverStock*100} WHERE userid='{command.username}' AND stock='{command.stockSymbol}'");
                } else
                {
                    result = $"User {command.username} does not own any {command.stockSymbol} stock";
                }
            }
            catch (Exception e)
            {
                result = $"Error occured getting account details.";
                Console.WriteLine(e.ToString());
            }
            LogTransactionEvent(command);
            return result;
        }

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
