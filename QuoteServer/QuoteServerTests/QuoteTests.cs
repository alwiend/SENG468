using QuoteServer;
using Xunit;

namespace QuoteServerTests
{
    public class QuoteTests
    {
        [Theory]
        [InlineData("abc")]
        [InlineData("def")]
        [InlineData("ghi")]
        // New Quotes should be generated with
        // cost > 0
        public void NewQuote(string name)
        {
            Quote q = new Quote(name);
            Assert.True(q.Cost > 0);
            Assert.Equal(name, q.Name);
        }
    }
}
