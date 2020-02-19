using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Database
{
    public class MySQL
    {
        private static string ConnectionString = "server=databaseserver_db_1;port=3306;database=db;uid=user;pwd=password;";

        public async Task<Dictionary<string,object>[]> ExecuteAsync(string command)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
            using (MySqlConnection cnn = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(command, cnn))
                {
                    await cnn.OpenAsync().ConfigureAwait(false);
                    var rdr = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while(await rdr.ReadAsync().ConfigureAwait(false))
                    {
                        Dictionary<string, object> row = new Dictionary<string, object>();
                        for (int i = 0; i < rdr.FieldCount; i++)
                        {
                            row.Add(rdr.GetName(i), rdr[i]);
                        }
                        results.Add(row);
                    }
                }
            }
            return results.ToArray();
        }

        public async Task<int> ExecuteNonQueryAsync(string command)
        {
            int rows = 0;
            using (MySqlConnection cnn = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(command, cnn))
                {
                    await cnn.OpenAsync().ConfigureAwait(false);
                    rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            return rows;
        }
    }
}

