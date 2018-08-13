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

        // Strings from app.config to communicate with client
        private string dateSetOK = System.Configuration.ConfigurationManager.AppSettings["dateSetOK"];
        private string dateSetNOK = System.Configuration.ConfigurationManager.AppSettings["dateSetNOK"];
        private string timeSetOK = System.Configuration.ConfigurationManager.AppSettings["timeSetOK"];
        private string timeSetNOK = System.Configuration.ConfigurationManager.AppSettings["timeSetNOK"];
        private string volumeSetOK = System.Configuration.ConfigurationManager.AppSettings["volumeSetOK"];
        private string volumeSetNOK = System.Configuration.ConfigurationManager.AppSettings["volumeSetNOK"];

        // Contains certain methods for dataprocessing
        CommonUtilities common = new CommonUtilities();

        // Variable to check for how many ACK's the application is waiting, after any type of update. 
        int waitingForAck = 0;
        
        // Responsible for communication to the database
        DataBaseCommunicator dbCommunicator = new DataBaseCommunicator();

        // Event to be fired after receiving an ACK through the serial port. 
        public event EventHandler<AckReceivedEventArgs> ackReceived;

        // Keeps track of the data that was read through the serial port
        string readData;

        // Contains for every ACK an appropriate message
        Dictionary<string, string> acks = new Dictionary<string, string>();

        // The serial port to communicate with
        private SerialPort port;


        public BoardCommunicator()
        {
            FillAckDictionary();
            port = new SerialPort(portName, bps, parity, nrOfDatabits, stopbits);
            port.DataReceived += new SerialDataReceivedEventHandler(DataInPort);
            if (!port.IsOpen)
            {
                port.Open();
                CheckingForLog();
            }
            else
            {
                Console.WriteLine("Cannot open a port more than once");
            }
        }

        /// <summary>
        /// This method is needed to handle Ack received events
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnACKReceived(AckReceivedEventArgs e)
        {
            EventHandler<AckReceivedEventArgs> handler = ackReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Sets the message in the ACK received event and fires the event
        /// </summary>
        /// <param name="message">The message in the ACK received event</param>
        /// <param name="originalMessage">The original message received from the board</param>
        private void SetArgsAndFireEvent(string message, string originalMessage)
        {
            AckReceivedEventArgs args = new AckReceivedEventArgs();
            args.Ack = message;
            args.originalACK = originalMessage;
            OnACKReceived(args);
        }

        /// <summary>
        /// When data is received through the COM-port, DataInport handles this event. 
        /// The date, node, event,time, tag are extracted and used to add a record to a database of logs. 
        /// After correct entry of the data in the database, an acknowledgment to the PCB-board is sent.
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
                        try {
                            SendData();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Problem sending ACK to board: " + e.Message);
                        }
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
                // Extra line to check for incorrect format from board
                if(receivedString.Contains("DATE SET OK"))
                {
                    receivedString = "DATE SET OK\r\n";
                }
                string ACK = SelectCorrectAnswer(receivedString);
                if (ACK != null)
                {
                    SetArgsAndFireEvent(ACK, receivedString);
                    waitingForAck--;
                }
            }
        }

        private void FillAckDictionary()
        {
            //TODO check if these strings are correct as key in the dictionary
            acks["DATE SET OK\r\n"] = dateSetOK;
            acks["DATE SET NOK\r\n"] = dateSetNOK;
            acks["TIME SET OK\r\n"] = timeSetOK;
            acks["TIME SET NOK\r\n"] = timeSetNOK;
            acks["VOL SET OK\r\n"] = volumeSetOK;
            acks["VOL SET NOK\r\n"] = volumeSetNOK;
        }
        
        /// <summary>
        /// Selects a response string for a given ACK
        /// </summary>
        /// <param name="ACK">The ACK received through the serial port</param>
        /// <returns>A message</returns>
        private string SelectCorrectAnswer(string ACK)
        {
            string result = null;
            if (!acks.TryGetValue(ACK, out result))
            { /* key doesn't exist */
                Console.WriteLine("ERROR: Did not recognize the ACK as key");
                return null;  //"undefined ACK received from board: " + ACK;
            }
            return result;
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
            // Important to use the timer of System.Threading instead of System.Timers
            var logTimer = new System.Threading.Timer(CheckTheLog, null, 1000, 500);
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
                byte[] bytes = common.HexStringToByteArray(hexString);

                // Step 3: Write the bytes
                port.Write(bytes, 0, 3);

                //TODO remove testcode
                //SetArgsAndFireEvent(dateSetOK);
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
                byte[] bytes = common.HexStringToByteArray(hexString);

                // step 3: write the bytes
                port.Write(bytes, 0, 3);

                //TODO remove testcode
                //SetArgsAndFireEvent(SelectCorrectAnswer("TIME SET OK"));
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
        public void UpdateVolume(decimal received)
        {
            try
            {
                waitingForAck++;
                int volume = Decimal.ToInt32(received);
                if (volume > 64 || volume < 21)
                {
                    waitingForAck--;
                    Console.WriteLine("Please enter a number >20 and <65");
                }

                //Step 1: Notify change volume
                string startValue = "BGVOL";
                port.Write(startValue);

                //Step 2: Convert to byte-array
                string hexString = volume.ToString("X2");
                byte[] bytes = common.HexStringToByteArray(hexString);

                //Step 3: Write the results
                port.Write(bytes, 0,1);

                //TODO remove testcode
                //SetArgsAndFireEvent(volumeSetOK);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: unable to create update string for volume. " + e.Message);
                waitingForAck--;
            }
        }
    }

    /// <summary>
    /// Responsible to raise an event with a certain string as message
    /// </summary>
    public class AckReceivedEventArgs : EventArgs
    {
        public string Ack { get; set; }
        public string originalACK { get; set; }
    }
}
