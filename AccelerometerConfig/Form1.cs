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
        private BackgroundWorker backgroundWorker;
        private bool continuePolling;
        private int pollInterval;

        public Form1()
        {
            I2CAccelerometerControl.Open();
            continuePolling = true;
            pollInterval = 2000;
            InitializeComponent();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_doWork);
            backgroundWorker.RunWorkerAsync();
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker_ProgressChanged);
            backgroundWorker.WorkerReportsProgress = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if ((bool) e.UserState)
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

                if (!connected)
                {
                    I2CAccelerometerControl.Close();
                    I2CAccelerometerControl.Open();
                }

                backgroundWorker.ReportProgress(0, connected);
            }
        }
    }
}
