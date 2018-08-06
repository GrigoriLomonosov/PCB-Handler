using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace BoardCommunication
{
    /// <summary>
    /// Responsible for handling communication with a pcb-board through a push-protocol.
    /// </summary>
    class BoardCommunicator
    {
        
        // Settings to communicate with serial port, based on app.config file
        private string portName = System.Configuration.ConfigurationManager.AppSettings["portName"];
        private int bps = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["baudrate"]);
        private int nrOfDatabits = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["nrOfDatabits"]);
        private StopBits stopbits = (StopBits)Enum.Parse(typeof(StopBits), System.Configuration.ConfigurationManager.AppSettings["stopbits"]);
        private Parity parity = (Parity)Enum.Parse(typeof(Parity), System.Configuration.ConfigurationManager.AppSettings["parity"]);

        // Keeps track of the data that was read through the serial port
        string readData;

        // Variable to check for how many ACK's the application is waiting, after any type of update. 
        int waitingForAck = 0;
        
        // Responsible for communication to the database
        DataBaseCommunicator dbCommunicator = new DataBaseCommunicator();

        private SerialPort port;


        public BoardCommunicator()
        {
            port = new SerialPort(portName, bps, parity, nrOfDatabits, stopbits);
            port.DataReceived += new SerialDataReceivedEventHandler(DataInPort);
            if (!port.IsOpen)
            {
                //TODO do not forget to uncomment
                //port.Open();
                //CheckingForLog();
            }
            else
            {
                Console.WriteLine("Cannot open a port more than once");
            }
        }

        /// <summary>
        /// When data is received through the COM-port, DataInport handles this event. 
        /// The date, node, event,time, tag are extracted and used to add a record to a database of logs. 
        /// After correct entry of the data in the datbase, an acknowledgment to the PCB-board is sent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataInPort(object sender, SerialDataReceivedEventArgs e)
        {
            readData = port.ReadExisting();
            ProcessReceivedData(readData);
        }

        /// <summary>
        /// Processes the data received through the COM-port. 
        /// </summary>
        /// <param name="receivedString">A string received from the PCB-board</param>
        private void ProcessReceivedData(string receivedString)
        {
            // Received string should be a databaserecord
            if (waitingForAck == 0)
            {
                string[] splittedString = System.Text.RegularExpressions.Regex.Split(receivedString, @"\s+");
                Console.WriteLine("Received message: " + receivedString);
                Console.WriteLine("Received message length" + receivedString.Length);
                if(receivedString.Length == 53)
                {
                    if (dbCommunicator.AddBoardLogToDatabase(splittedString[2], splittedString[3], splittedString[4], splittedString[5], splittedString[6]))
                    {
                        SendData();
                    }
                }
                else
                {
                    Console.WriteLine("Received a log string in an incorrect format. String should contain 53 chars");
                }
            }
            // Received string should be an acknowledgment
            else
            {
                Console.WriteLine(receivedString);
                waitingForAck--;
            }
        }

        /// <summary>
        /// Sends acknowledgement to PCB-board that data was received and correctly entered in database
        /// </summary>
        private void SendData()
        {
            string acknowledgment = "LOG OK" + "\r" + "\n";
            port.Write(acknowledgment);
            Console.Write(acknowledgment);
        }

        /// <summary>
        /// Asks the board to send a logline for the database very 0.5 seconds
        /// </summary>
        private void CheckingForLog()
        {
            var logTimer = new Timer(CheckTheLog, null, 1000, 500);
        }

        private void CheckTheLog(object state)
        {
            Console.WriteLine("in check log");
            port.Write("BGLOG");
        }

        /// <summary>
        /// Updates the date of the PCB-board to today's system date
        /// </summary>
        public void UpdateDate()
        {
            try
            {
                waitingForAck++;
                DateTime received = DateTime.Today;

                // Step 1: notify you want to change the date
                string startValue = "BGDAT";
                port.Write(startValue);

                // Step 2: convert the integers to hex
                string hexString = (received.Year - 2000).ToString("X2") + received.Month.ToString("X2") + received.Day.ToString("X2");
                byte[] bytes = HexStringToByteArray(hexString);

                // Step 3: Write the bytes
                port.Write(bytes, 0, 3);
            }
            catch(OverflowException e)
            {
                Console.WriteLine("ERROR: unable to create update string for date. " + e.Message);
                waitingForAck--;
            }
        }

        /// <summary>
        /// Updates the time of the PCB-board to the current system time
        /// </summary>
        public void UpdateTime()
        {
            try
            {
                waitingForAck++;
                DateTime received = DateTime.Now;

                // Step 1: notify you want to change the time
                string startValue = "BGTIM";
                port.Write(startValue);

                // Step 2: convert the integers to hex
                string hexString = received.Hour.ToString("X2") + received.Minute.ToString("X2") + received.Second.ToString("X2");
                byte[] bytes = HexStringToByteArray(hexString);

                // step 3: write the bytes
                port.Write(bytes, 0, 3);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: unable to create update string for time. " + e.Message);
                waitingForAck--;
            }
        }

        /// <summary>
        /// Updates the volume of the alarm of the PCB-board
        /// </summary>
        /// <param name="received">The update should be done to the received value. A value between 20 and 65 should be given (not included)</param>
        /// <returns>True if update was successful, false otherwise</returns>
        public bool UpdateVolume(decimal received)
        {
            try
            {
                waitingForAck++;
                int volume = Decimal.ToInt32(received);
                if (volume > 64 || volume < 21)
                {
                    waitingForAck--;
                    Console.WriteLine("Please enter a number >20 and <65");
                    return false;
                }

                //Step 1: Notify change volume
                string startValue = "BGVOL";
                port.Write(startValue);

                //Step 2: Convert to byte-array
                string hexString = volume.ToString("X2");
                byte[] bytes = HexStringToByteArray(hexString);

                //Step 3: Write the results
                port.Write(bytes, 0,1);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: unable to create update string for volume. " + e.Message);
                waitingForAck--;
                return false;
            }
        }

        /// <summary>
        /// Returns a byte-array for a given string containing hex-numbers.
        /// </summary>
        /// <param name="hex">The hex-string should contain an even number of characters</param>
        /// <returns>An array holding the different bytes in the hex-string</returns>
        private byte[] HexStringToByteArray(string hex)
        {
            int offset = hex.StartsWith("0x") ? 2 : 0;
            if ((hex.Length % 2) != 0)
            {
                throw new ArgumentException("Invalid length: " + hex.Length);
            }
            byte[] ret = new byte[(hex.Length - offset) / 2];

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = (byte)((ParseNybble(hex[offset]) << 4)
                                 | ParseNybble(hex[offset + 1]));
                offset += 2;
            }
            return ret;
        }

        private int ParseNybble(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }
            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            throw new ArgumentException("Invalid hex digit: " + c);
        }
    }
}
