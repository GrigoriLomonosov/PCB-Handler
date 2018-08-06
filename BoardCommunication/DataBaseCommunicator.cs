using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace BoardCommunication
{
    /// <summary>
    /// Responsible for communication with a database of logs.
    /// </summary>
    class DataBaseCommunicator
    {

        private string connectionString;

        public DataBaseCommunicator()
        {
            connectionString = ConfigurationManager.ConnectionStrings["PCBLogs"].ConnectionString;
        }

        /// <summary>
        /// Adds a line of the log to the database. The line was received from a PCB-board.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="node"></param>
        /// <param name="_event"></param>
        /// <param name="time"></param>
        /// <param name="tag"></param>
        /// <returns>True if the record was correctly entered, false otherwise</returns>
        public bool AddBoardLogToDatabase(string date, string node, string _event, string time, string tag)
        {

            string query = "INSERT INTO dbo.PCBLogs(Date, Node, Event, Time, Tag) VALUES(@date, @node, @event, @time, @tag);";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@node", node);
                    cmd.Parameters.AddWithValue("@event", _event);
                    cmd.Parameters.AddWithValue("@time", time);
                    cmd.Parameters.AddWithValue("@tag", tag);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("Unable to add record to PCBLogs database because of SQLException: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to add record to PCBLogs database: " + e.Message);
                return false;
            }

            // Record correctly added to database
            return true;
        }

        /// <summary>
        /// Returns a list of all valid databaserecords.
        /// </summary>
        /// <returns></returns>
        public List<LogDbRecord> GetAllLogRecords()
        {
            //Order is important to keep chronology of logs
            string query = "SELECT * FROM  dbo.PCBLogs ORDER BY id";

            List<LogDbRecord> result = new List<LogDbRecord>();
            try
            {
                using (SqlConnection myConnection = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, myConnection))
                {
                    myConnection.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Check for invalid DB records
                            if ((Convert.IsDBNull(reader["id"])) ||
                                (Convert.IsDBNull(reader["date"])) ||
                                (Convert.IsDBNull(reader["node"])) ||
                                (Convert.IsDBNull(reader["event"])) ||
                                (Convert.IsDBNull(reader["time"])) ||
                                (Convert.IsDBNull(reader["tag"])))  
                            {
                                Console.WriteLine("Invalid database record ignored");
                            }
                            else
                            {
                                result.Add(new LogDbRecord(
                                Convert.ToInt32(reader["id"]),
                                Convert.ToDateTime(reader["date"]),
                                Convert.ToInt32(reader["node"]),
                                Convert.ToInt32(reader["event"]),
                                Convert.ToString(reader["time"]),
                                Convert.ToInt32(reader["tag"])));

                                Console.WriteLine(reader["id"].ToString());
                            }
                        }
                        myConnection.Close();
                    }
                }
                return result;
            }
            catch (SqlException e)
            {
                Console.WriteLine("SQL ERROR: " + e.Message);
                return null;
            }

        }
    }
}
