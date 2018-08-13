using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace BoardCommunication
{
    public partial class Board_Communicator : Form
    {
        private BoardCommunicator boardCommunicator = new BoardCommunicator();

        private FileCreator fileCreator = new FileCreator();

        private int closeAfterMillis = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["closeAfterMillis"]);

        private int previousVolumeValue = 21;
        public Board_Communicator()
        {
            InitializeComponent();
            boardCommunicator.ackReceived += HandleReceivedACK;
        }

        /// <summary>
        /// Updates the time of the PCB board to the current system time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateTime(object sender, EventArgs e)
        {
            boardCommunicator.UpdateTime();
            Console.WriteLine("pressed update time");
        }

        /// <summary>
        /// Updates the time of the PCB-board to the date to today's system date
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateDate(object sender, EventArgs e)
        {
            boardCommunicator.UpdateDate();
            Console.WriteLine("pressed update date");
        }

        ///// <summary>
        ///// Updates the volume of the PCB-board. Only values greater than 20 and smaller than 65 are allowed
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //public void UpdateVolume(object sender, EventArgs e)
        //{
        //    //boardCommunicator.UpdateVolume(Int32.Parse(textBox1.Text));
        //    Console.WriteLine("pressed update volume: " + textBox1.Text);
        //}

        /// <summary>
        /// Creates a file, which summarizes the log in the from the PCB-boards
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CreateLogSummaryFile(object sender, EventArgs e)
        {
            if (fileCreator.CreateLogSummaryFile())
            {
                //TODO change message box if needed
                //AutoClosingMessageBox.Show("Text", "Caption", closeAfterMillis);
                MessageBox.Show("Successfully written to file");
            }
            else
            {
                //TODO check for correct string and change MessageBox if needed
                MessageBox.Show("ERROR: failed to write to file");
                //AutoClosingMessageBox.Show("Text", "Caption", closeAfterMillis);
            }
            Console.WriteLine("pressed create log");
        }

        /// <summary>
        /// When an ACK is received through the serial port, an automatically closing messagebox is shown with the received ACK.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        void HandleReceivedACK(object sender, AckReceivedEventArgs a)
        {
            //TODO change to select the wanted MessageBox
            MessageBox.Show(a.Ack);
            //AutoClosingMessageBox.Show(a.Ack, "Received Message from port", closeAfterMillis);

            // If volume setting failed, reset the trackbar value
            if (a.Ack == System.Configuration.ConfigurationManager.AppSettings["timeSetNOK"])
            {
                trackBar1.Value = previousVolumeValue;
            }

            // Show original message from board in textbox
            this.Invoke(new MethodInvoker(delegate ()
            {
                textBox2.Text = a.originalACK;
            }));
            
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trackBar1, trackBar1.Value.ToString());
            //textBox1.Text = trackBar1.Value.ToString();
        }

        private void trackbar1_MouseUp(object sender, EventArgs e)
        {
            boardCommunicator.UpdateVolume(trackBar1.Value);
        }

        private void trackbar1_MouseDown(object sender, EventArgs e)
        {
            previousVolumeValue = trackBar1.Value;
        }

        private void Board_Communicator_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'pCBLogsDataSet.PCBLogs' table. You can move, or remove it, as needed.
            this.pCBLogsTableAdapter.Fill(this.pCBLogsDataSet.PCBLogs);

        }
    }
}
