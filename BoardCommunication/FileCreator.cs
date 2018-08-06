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

        // Responsible for communication to the database
        DataBaseCommunicator dbCommunicator = new DataBaseCommunicator();

        public FileCreator()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            outputfile = Path.Combine(folder, "log.txt");
            using (StreamWriter file = new StreamWriter(outputfile))
            {
                file.WriteLine("Node    CallTime    CallDate    CancelTime  CancelDate  OnTime  OffTime CancelKey");
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
            string line = record.node + "   "
                + record.callTime + "   "
                + record.callDate + "   "
                + record.cancelTime + " "
                + record.cancelDate + " "
                + record.onTime + " "
                + record.offTime + "    "
                + record.cancelKey;

            //outputfile.WriteLine(line);
            using (StreamWriter file = new StreamWriter(outputfile, true))
            {
                file.WriteLine(line);
            }
        }
    }
}
