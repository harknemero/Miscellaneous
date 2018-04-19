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
        private bool connected;
        private int pollInterval;
        private int pollCounter;

        public Form1()
        {
            I2CAccelerometerControl.Open();
            connected = false;
            continuePolling = true;
            pollInterval = 100;
            pollCounter = 0;
            InitializeComponent();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_doWork);
            backgroundWorker.RunWorkerAsync();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker_NewData);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }        

        private void BackgroundWorker_NewData(object sender, ProgressChangedEventArgs e)
        {
            short[] data = (short[]) e.UserState;
            label2.Text = "X-Axis: " + data[0];
            label3.Text = "Y-Axis: " + data[1];
            label4.Text = "Z-Axis: " + data[2];

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

            label5.Text = "Poll # " + pollCounter;
        }

        // Reads accelerometer status on a separate thread
        private void BackgroundWorker_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            
            short[] data = { 0, 0, 0 };
            while (continuePolling) //*** Currently nothing changes the continuePolling variable, so this will run until the program shuts down.
            {
                System.Threading.Thread.Sleep(pollInterval);

                connected = I2CAccelerometerControl.VerifyAccelerometer();
                

                if (!connected)
                {
                    I2CAccelerometerControl.Close();
                    I2CAccelerometerControl.Open();
                    pollCounter = 0;
                }
                else
                {
                    data = I2CAccelerometerControl.GetData();

                    pollCounter++;
                }
                backgroundWorker.ReportProgress(0, data);
            }
        }
    }
}
