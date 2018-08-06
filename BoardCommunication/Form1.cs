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
    public partial class Form1 : Form
    {
        private BoardCommunicator boardCommunicator = new BoardCommunicator();

        private FileCreator fileCreator = new FileCreator();
        public Form1()
        {
            InitializeComponent();
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

        /// <summary>
        /// Updates the volume of the PCB-board. Only values greater than 20 and smaller than 65 are allowed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateVolume(object sender, EventArgs e)
        {
            boardCommunicator.UpdateVolume(numericUpDown1.Value);
            Console.WriteLine("pressed update volume");
        }

        /// <summary>
        /// Creates a file, which summarizes the log in the from the PCB-boards
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CreateLogSummaryFile(object sender, EventArgs e)
        {
            fileCreator.CreateLogSummaryFile();
            Console.WriteLine("pressed create log");
        }
    }
}
