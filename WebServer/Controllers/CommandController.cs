using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebServer.Handlers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebServer
{
    [Produces("application/json")]
    [Route("api/command")]
    public class CommandController : Controller
    {
        readonly TransactionHandler _handler;

        public CommandController(TransactionHandler handler)
        {
            _handler = handler;
        }

        [HttpHead]
        public async Task<IActionResult> HeadAsync([FromQuery(Name = "cmd")] string Command)
        {
            var result = await _handler.HandleTransaction(Command);
            return StatusCode((int)result.Item1, result.Item2);
        }


        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery(Name = "cmd")] string Command)
        {
            var result = await _handler.HandleTransaction(Command);
            return StatusCode((int)result.Item1, result.Item2);
        }


        // POST api/<controller>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody]TransactionCommand transaction)
        {
            var result = await _handler.HandleTransaction(transaction.Command);
            return StatusCode((int)result.Item1, result.Item2);
        }

    }

    public class TransactionCommand
    {
        public string Command { get; set; }
    }
}
