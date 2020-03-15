using System;
using System.Collections.Generic;
using System.Text;

using Constants;
using Base;
using Utilities;
using Database;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

namespace SellTriggerService
{
    public class SetSellTrigger : BaseService
    {
        public SetSellTrigger(ServiceConstant sc, AuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            try
            {
                command.fundsSpecified = true;
                var msg = await SellTriggerTimer.StartOrUpdateTimer(command).ConfigureAwait(false);
                if (msg == null)
                {
                    return $"Trigger amount set successfully for stock {command.stockSymbol}";
                }
                return await LogErrorEvent(command, msg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await LogDebugEvent(command, ex.Message).ConfigureAwait(false);
                return await LogErrorEvent(command, "Error processing command.").ConfigureAwait(false);
            }
        }
    }
}
