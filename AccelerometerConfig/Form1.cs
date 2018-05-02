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
        private string configString;
        private bool[] interrupt1Status;
        private bool[] interrupt2Status;

        public Form1()
        {
            I2CAccelerometerControl.Open();
            connected = false;
            continuePolling = true;
            pollInterval = 0; // Set to zero for continuous polling
            pollCounter = 0;
            configString = "";
            interrupt1Status = new bool[4] { false, false, false, false };
            interrupt2Status = new bool[4] { false, false, false, false };
            InitializeComponent();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_doWork);
            backgroundWorker.RunWorkerAsync();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker_NewData);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            I2CAccelerometerControl.SetFrequency(Convert.ToInt32(textBox1.Text));
            I2CAccelerometerControl.SetThreshold1(Convert.ToDouble(textBox2.Text));
            I2CAccelerometerControl.SetDuration1(Convert.ToDouble(textBox3.Text));
            I2CAccelerometerControl.SetThreshold2(Convert.ToDouble(textBox4.Text));
            I2CAccelerometerControl.SetDuration2(Convert.ToDouble(textBox5.Text));
            I2CAccelerometerControl.EnableInterrupts();
            button2_Click(sender, e); // if settings are being changed, then flags should probably be reset too.
        }

        // Reset labels 13-20 to black text
        private void button2_Click(object sender, EventArgs e)
        {
            label13.ForeColor = Color.Black;
            label14.ForeColor = Color.Black;
            label15.ForeColor = Color.Black;
            label16.ForeColor = Color.Black;
            label17.ForeColor = Color.Black;
            label18.ForeColor = Color.Black;
            label19.ForeColor = Color.Black;
            label20.ForeColor = Color.Black;
        }

        // Reads accelerometer status on a separate thread
        private void BackgroundWorker_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            
            double[] data = { 0, 0, 0 };
            while (continuePolling) //*** Currently nothing changes the continuePolling variable, so this will run until the program shuts down.
            {
                System.Threading.Thread.Sleep(pollInterval);

                connected = I2CAccelerometerControl.VerifyAccelerometer();
                

                if (!connected)
                {
                    I2CAccelerometerControl.Close();
                    I2CAccelerometerControl.Open();
                    pollCounter = 0;
                    configString = "";
                }
                else
                {
                    data = I2CAccelerometerControl.GetData();
                    configString = I2CAccelerometerControl.GetConfiguration();
                    I2CAccelerometerControl.GetInterruptStatus1().CopyTo(interrupt1Status, 0);
                    I2CAccelerometerControl.GetInterruptStatus2().CopyTo(interrupt2Status, 0);

                    pollCounter++;
                }
                backgroundWorker.ReportProgress(0, data);
            }
        }

        private void BackgroundWorker_NewData(object sender, ProgressChangedEventArgs e)
        {
            double[] data = (double[])e.UserState;
            label2.Text = "X-Axis: " + data[0].ToString("N3");
            label3.Text = "Y-Axis: " + data[1].ToString("N3");
            label4.Text = "Z-Axis: " + data[2].ToString("N3");

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
            richTextBox1.Text = configString;
            updateInterruptStatus();
        }

        private void updateInterruptStatus()
        {
            if (interrupt1Status[0])
            {
                label13.ForeColor = Color.Red;
            }
            if (interrupt1Status[1])
            {
                label14.ForeColor = Color.Red;
            }
            if (interrupt1Status[2])
            {
                label15.ForeColor = Color.Red;
            }
            if (interrupt1Status[3])
            {
                label16.ForeColor = Color.Red;
            }
            if (interrupt2Status[0])
            {
                label17.ForeColor = Color.Red;
            }
            if (interrupt2Status[1])
            {
                label18.ForeColor = Color.Red;
            }
            if (interrupt2Status[2])
            {
                label19.ForeColor = Color.Red;
            }
            if (interrupt2Status[3])
            {
                label20.ForeColor = Color.Red;
            }
        }
    }
}
