using System;
using System.Collections.Generic;
using System.Text;

namespace QuoteServer
{
    public class Quote
    {
        private DateTime quoteTime = DateTime.MinValue;
        private double cost;
        private readonly string name;

        public string Name
        {
            get
            {
                return name;
            }
        }

        public double Cost
        {
            get
            {
                // If the quote is more than 60 seconds old
                // get a new quote
                if (quoteTime.AddSeconds(60) < DateTime.Now)
                {
                    quoteTime = DateTime.Now;
                    // Get a cent value from 1 to 1000
                    // Get dollar value by dividing by 100.00
                    cost = new Random().Next(1, 1000)/100.00;
                }
                return cost;
            }
        }

        public Quote(string name)
        {
            // Names are case sensitive
            this.name = name;
        }
    }
}
