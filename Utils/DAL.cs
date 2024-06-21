using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace TestDistributedTransactions.Utils
{
    public class DAL
    {
        private const string _dbConnectionString = "Server=tcp:eftestdbserver.database.windows.net,1433;Initial Catalog=eftest1;Persist Security Info=False;User ID=efadmin;Password=AthenaTommy123@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public static int ExecuteGetCountSP(string spName)
        {
            string SqlConnectionString = _dbConnectionString;
            SqlConnection conn = new SqlConnection(SqlConnectionString);
            conn.Open();

            int returnedData = 0;

            SqlCommand command = new SqlCommand(spName, conn);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    returnedData = reader.GetInt32(0);
                }
            }

            conn.Close();

            return returnedData;
        }

        public static void ExecuteUpdateSP(string spName)
        {
            /*
            string SqlConnectionString = "Server=tcp:eftestdbserver.database.windows.net,1433;Initial Catalog=eftest1;Persist Security Info=False;User ID=efadmin;Password=AthenaTommy123@;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            SqlConnection conn = new SqlConnection(SqlConnectionString);
            conn.Open();

            SqlCommand command = new SqlCommand(spName, conn);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.ExecuteNonQuery();

            conn.Close();
            */
        }

    }
}
