using System;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Database
{
    public class MySQL
    {

        public string Execute(string command)
        {
            string connetionString = "server=databaseserver_db_1;port=3306;database=db;uid=user;pwd=password;";
            MySqlConnection cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                Console.WriteLine("Connected to db");

                MySqlCommand cmd = new MySqlCommand(command, cnn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);

                using (JsonWriter jsonWriter = new JsonTextWriter(sw))
                {
                    jsonWriter.WriteStartArray();

                    while (rdr.Read())
                    {
                        jsonWriter.WriteStartObject();

                        int fields = rdr.FieldCount;

                        for (int i = 0; i < fields; i++)
                        {
                            jsonWriter.WritePropertyName(rdr.GetName(i));
                            jsonWriter.WriteValue(rdr[i]);
                        }

                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndArray();

                }
                rdr.Close();

                cnn.Close();
                return sw.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";
        }

        public string ExecuteNonQuery(string command)
        {
            string connetionString = "server=databaseserver_db_1;port=3306;database=db;uid=user;pwd=password;";
            MySqlConnection cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                Console.WriteLine("Connected to db");

                MySqlCommand cmd = new MySqlCommand(command, cnn);
                var result = cmd.ExecuteNonQuery();

                Console.WriteLine($"{result} rows inserted");

                cnn.Close();
                return $"{{'rows': {result}}}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return $"{{'rows': 0}}";
        }
    }
}

