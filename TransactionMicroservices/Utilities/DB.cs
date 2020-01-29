using System;
using MySql.Data.MySqlClient;

namespace Utilities
{
    public class DB
    {

        public object Execute(string command)
        {
            string connetionString = null;
            MySqlConnection cnn;
            connetionString = "server=localhost;port=3306;database=db;uid=user;pwd=password;";
            cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                Console.WriteLine("Connected to db");

                MySqlCommand cmd = new MySqlCommand(command, cnn);
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    for (int i = 0; i < rdr.FieldCount; i++)
                    {
                        Console.Write(rdr[i]);
                    }
                    Console.WriteLine();
                }
                rdr.Close();

                cnn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public void ExecuteNonQuery(string command)
        {

            string connetionString = null;
            MySqlConnection cnn;
            connetionString = "server=localhost;port=3306;database=db;uid=user;pwd=password;";
            cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                Console.WriteLine("Connected to db");

                MySqlCommand cmd = new MySqlCommand(command, cnn);
                var result = cmd.ExecuteNonQuery();

                Console.WriteLine($"{result} rows inserted");

                cnn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

