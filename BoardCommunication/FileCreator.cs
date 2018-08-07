using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardCommunication
{
    /// <summary>
    /// Responsible for creating a file that summarizes the log from the PCB-board
    /// </summary>
    class FileCreator
    {
        private string outputfile;

        // Persian separator in Excel
        private string sep = ",";

        // Responsible for communication to the database
        DataBaseCommunicator dbCommunicator = new DataBaseCommunicator();

        public FileCreator()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            outputfile = Path.Combine(folder, "log.csv");
            using (StreamWriter file = new StreamWriter(outputfile))
            {
                file.WriteLine("Node" + sep + "CallTime" + sep + "CallDate" + sep + "CancelTime" + sep + "CancelDate" + sep + "OnTime" + sep + "OffTime" + sep + "CancelKey");
            }
        }

        public void CreateLogSummaryFile()
        {
            processData(dbCommunicator.GetAllLogRecords());
        }

        private void processData(List<LogDbRecord> list)
        {
            // Needed to keep track of on/off times
            int currentTag = -1;

            // Will represent a record in the file combining event 3 and 4
            LogFilerecord onOffTemp = new LogFilerecord("-", "-", "-", "-", "-", "-", "-", "-");

            // Will keep track of all call events before a cancel is triggered.
            List<LogFilerecord> callEvents = new List<LogFilerecord>();

            foreach (LogDbRecord dbRecord in list)
            {
                switch (dbRecord._event)
                {
                    case 1:
                        callEvents.Add(new LogFilerecord(
                            dbRecord.node.ToString(),
                            dbRecord.time,
                            dbRecord.date.ToString("yyyy-MM-dd"),
                            "-",
                            "-",
                            "-",
                            "-",
                            "-"
                            ));
                        break;
                    case 2:
                        foreach(LogFilerecord callEvent in callEvents)
                        {
                            callEvent.cancelTime = dbRecord.time;
                            callEvent.cancelDate = dbRecord.date.ToString("yyyy-MM-dd");
                            callEvent.cancelKey = dbRecord.node.ToString();
                            WriteFileRecordToFile(callEvent);
                        }
                        callEvents.Clear();
                        break;
                    case 3:
                        if(dbRecord.tag == currentTag)
                        {
                            onOffTemp.onTime = dbRecord.time;
                            WriteFileRecordToFile(onOffTemp);
                        }
                        else
                        {
                            // This code should not be reached.
                            Console.WriteLine("ERROR: matching event 3 not found");
                        }  

                        break;
                    case 4:
                        onOffTemp = new LogFilerecord("-", "-", "-", "-", "-", "-", dbRecord.time, "-");
                        currentTag = dbRecord.tag;
                        break;
                    default:
                        Console.WriteLine("ERROR: each line should be an event of type 1, 2, 3, or 4");
                        break;
                }
            }
        }

        /// <summary>
        /// Appends all the data from a LogfileRecord to the outputfile on 1 line 
        /// </summary>
        /// <param name="record">The record for which to write the data to the outputfile</param>
        private void WriteFileRecordToFile(LogFilerecord record)
        {
            // Create the line to write
            string line = record.node + sep
                + record.callTime + sep
                + record.callDate + sep
                + record.cancelTime + sep
                + record.cancelDate + sep
                + record.onTime + sep
                + record.offTime + sep
                + record.cancelKey;

            //outputfile.WriteLine(line);
            using (StreamWriter file = new StreamWriter(outputfile, true))
            {
                file.WriteLine(line);
            }
        }
    }
}
