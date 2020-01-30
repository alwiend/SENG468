using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Base;

namespace BuyService
{
    class BuyCommand : BaseService
    {
        public static void Main(string[] args)
        {
            new BuyCommand().StartService();
        }

        public BuyCommand() : base(BuyStock, 44442) 
        {

        }

        static string BuyStock(string stock_purchase)
        {
            string[] args = stock_purchase.Split(",");
            string user = args[0];
            string stock = args[1];
            double money = args[3].Contains(".") ? (int)(double.Parse(args[2]) / 100) : int.Parse(args[2]);
            string result = "";

            string stockCost = GetStock(stock);

            Console.Write($"Sending offer for {money} of {stock}");
            int balance = 0;

            try
            {
                DB db = new DB();

                balance = db.ExecuteQuery($"Select money FROM user WHERE user = {user}");

                if (balance < 1)
                {
                    return result = "Insufficient balance";
                }
            }
            catch (Exception e)
            {
                result = "Error occured getting account details.";
                Console.WriteLine(e.ToString());
            }

            double numStock = Math.Floor(money / double.Parse(stockCost));

            if (numStock < 1)
            {
                return result = $"Not enough money in account to buy {stock}";
            }

            double amount = numStock * double.Parse(stockCost); // amount to send to pending transactions
            double leftover = balance - amount;
            leftover *= 100;

            DateTime timeStamp = DateTime.Now;

            CacheBuyRequest(amount, stock, stockCost, user);

            result = $"{numStock} stock is available for purchase at {stockCost} per share totalling {amount}.";

            try
            {
                DB db = new DB();

                db.ExecuteNonQuery($"UPDATE user SET money = {leftover} WHERE user = {user}");
            }
            catch (Exception e)
            {
                result = "Error occured changing acccount balance";
                Console.WriteLine(e.ToString());
            }

            return result;
        }
        public static void CacheBuyRequest(double amount, string stock, string stockCost, string user)
        {
            List<string> transaction = new List<string> { user, stock, stockCost, Convert.ToString(amount) };
            BuyCache.StoreItemsInCache(user, transaction);
        }

        public static string GetStock(string stock)
        {
            string cost = "";
            try
            {

                // Establish the remote endpoint  
                // for the socket
                IPAddress ipAddr = IPAddress.Parse("172.1.0.10");
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
