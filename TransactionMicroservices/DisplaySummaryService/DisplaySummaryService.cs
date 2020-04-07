using System;
using Utilities;
using Database;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text;
using System.Threading;

namespace TransactionServer.Services
{
    public class DisplaySummaryService : BaseService
    {
        public static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(Environment.ProcessorCount * 15, Environment.ProcessorCount * 10);
            var display_summary_service = new DisplaySummaryService(Service.DISPLAY_SUMMARY_SERVICE, new AuditWriter());
            await display_summary_service.StartService().ConfigureAwait(false);
            display_summary_service.Dispose();
        }

        public DisplaySummaryService(ServiceConstant sc, IAuditWriter aw) : base(sc, aw)
        {
        }

        protected override async Task<string> DataReceived(UserCommandType command)
        {
            string result;
            try
            {
                MySQL db = new MySQL();
                result = await db.PerformTransaction(DisplaySummary, command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                result = LogErrorEvent(command, "Error getting account details");
                LogDebugEvent(command, e.Message);
            }
            return result;
        }


        async Task<string> DisplaySummary(MySqlConnection cnn, UserCommandType command)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = cnn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "display_summary";

                cmd.Parameters.AddWithValue("@pUserId", command.username);
                cmd.Parameters["@pUserId"].Direction = ParameterDirection.Input;
                cmd.Parameters.Add(new MySqlParameter("@pMoney", MySqlDbType.Int32));
                cmd.Parameters["@pMoney"].Direction = ParameterDirection.Output;

                var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                
                StringBuilder stocks = new StringBuilder();
                try
                {
                    while (await reader.ReadAsync())
                    {
                        // Reads in stock and amount
                        
                        stocks.Append($"{reader.GetString(0)}&nbsp&nbsp&nbsp&nbsp${String.Format("{0:0.00}", Convert.ToDecimal(reader.GetValue(1)) / 100m)}<br>");
                    }
                }
                finally
                {
                    await reader.CloseAsync();
                }

                var money = Convert.ToDecimal(cmd.Parameters["@pMoney"].Value);
                if (money == -1)
                {
                    return "User does not exist";
                }
                
                return $"User {command.username} has ${String.Format("{0:0.00}", money / 100m)}<br>Stock&nbsp&nbsp&nbsp&nbspAmount<br>" + stocks.ToString();
            }
        }
    }
}
