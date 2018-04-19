using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AccelerometerConfig
{
    public partial class Form1 : Form
    {
        private bool continuePolling;
        private int pollInterval;

        public Form1()
        {
            I2CAccelerometerControl.Open();
            continuePolling = true;
            pollInterval = 2000;
            InitializeComponent();

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_doWork);
            backgroundWorker.RunWorkerAsync();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void updateStatus(bool connected)
        {
            if (connected)
            {
                label1.Text = "Accelerometer Connected";
                label1.ForeColor = Color.Green;
            }
            else
            {
                label1.Text = "Accelerometer Not Connected";
                label1.ForeColor = Color.Red;
            }
        }

        // Reads accelerometer status on a separate thread
        private void BackgroundWorker_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            int pollCounter = 0;
            while (continuePolling) //*** Currently nothing changes the continuePolling variable, so this will run until the program shuts down.
            {
                pollCounter++;
                System.Threading.Thread.Sleep(pollInterval); 
                bool connected = I2CAccelerometerControl.VerifyAccelerometer();

                updateStatus(connected);
            }
        }
    }
}
