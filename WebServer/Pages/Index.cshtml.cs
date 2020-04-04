using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Utilities;
using WebServer.Handlers;

namespace WebServer.Pages
{
    public class IndexModel : PageModel
    {
        TransactionHandler _handler;

        [BindProperty]
        public string Command { get; set; }

        [BindProperty]
        public string Result { get; set; }

        public IndexModel(TransactionHandler handler)
        {
            _handler = handler;
        }

        public async Task<IActionResult> OnGetAsync(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return Page();
            Command = cmd;
            Result = (await _handler.HandleTransaction(cmd)).Item2;
            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            Result = (await _handler.HandleTransaction(Command)).Item2;
            return Page();
        }
    }
}
