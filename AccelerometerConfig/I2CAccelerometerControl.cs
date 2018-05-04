using System;
using System.Linq;
using System.Text;
using mcp2221_dll_m;
using System.Threading;
using System.ComponentModel;
using System.Collections;

namespace AccelerometerConfig
{
    class I2CAccelerometerControl
    {
        private const byte Accel_Address = 0x28;
        private static byte[] response;
        private static byte deviceId = 0x33;
        
        public static byte[] Response { get => response; set => response = value; }

        // Compares the returned device type to the type stored in static byte[] deviceType.
        // Returns true if the expected device type is returned.
        public static bool VerifyAccelerometer()
        {
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x0F);

            byte[] result = MCUSBI2C.I2CRead(Accel_Address, 1);
            if (result.Length == 1)
            {
                if (result[0] == deviceId)
                {
                    return true;
                }
            }
            return false;
        }

        // Returns an array containing the X, Y, and Z axis output (in that order)
        public static double[] GetData()
        {
            double[] data = new double[3];

            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x29);
            data[0] = (double) BitConverter.ToInt16(MCUSBI2C.I2CRead(Accel_Address, 2), 0);
            data[0] /= 16000;
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x2B);
            data[1] = (double) BitConverter.ToInt16(MCUSBI2C.I2CRead(Accel_Address, 2), 0);
            data[1] /= 16000;
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x2D);
            data[2] = (double) BitConverter.ToInt16(MCUSBI2C.I2CRead(Accel_Address, 2), 0);
            data[2] /= 16000;
            return data;
        }

        // Returns a string describing some of the current accelerometer settings.
        public static string GetConfiguration()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Sample Frequency: " + GetFrequency());
            sb.AppendLine("Interrupt 1 Threshold: " + GetThreshold1());
            sb.AppendLine("Interrupt 1 Duration: " + GetDuration1());
            sb.AppendLine("Interrupt 2 Threshold: " + GetThreshold2());
            sb.AppendLine("Interrupt 2 Duration: " + GetDuration2());

            return sb.ToString();
        }

        // Takes any int value and sets the accelerometer to the frequency closest to that value.
        public static void SetFrequency(int hz)
        {
            uint val = 0;
            if (hz < 1) val = 0;
            else if (hz < 10) val = 16;
            else if (hz < 25) val = 32;
            else if (hz < 50) val = 48;
            else if (hz < 100) val = 64;
            else if (hz < 200) val = 80;
            else if (hz < 400) val = 96;
            else if (hz < 1600) val = 112;
            else if (hz < 5376) val = 128;
            else val = 144;

            val += 15;
            byte[] v = { (byte)val };
            MCUSBI2C.I2CWrite(Accel_Address, 0x20, v);
        }

        // Returns a string describing the current sample frequency setting on the accelerometer.
        public static string GetFrequency()
        {
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x20);
            uint val = (uint) (MCUSBI2C.I2CRead(Accel_Address, 1)[0]) / 16;

            switch (val)
            {
                case 0: return "Power-Down Mode";
                case 1: return "1 hz";
                case 2: return "10 hz";
                case 3: return "25 hz";
                case 4: return "50 hz";
                case 5: return "100 hz";
                case 6: return "200 hz";
                case 7: return "400 hz";
                case 8: return "1600 hz";
                default: return "5376 hz";                
            }
        }

        // Configures the accelerometer interrupts by enabling them.
        public static void EnableInterrupts()
        {
            byte[] data = new byte[1] { 0xFF };
            MCUSBI2C.I2CWrite(Accel_Address, 0x30, data);
            MCUSBI2C.I2CWrite(Accel_Address, 0x34, data);
        }

        // Returns a string describing the current threshold setting for the Interrupt 1 pin.
        public static string GetThreshold1()
        {
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x32);
            int val = (int)(MCUSBI2C.I2CRead(Accel_Address, 1)[0]) * 16;
            double v = val;
            v /= 1000;
            return "" + v + "g";
        }

        // Sets the threshold setting for the Interrupt 1 pin.
        public static void SetThreshold1(double val)
        {
            ushort temp = (ushort) (val * 1000 / 16);
            byte[] data = { (byte)temp };
            MCUSBI2C.I2CWrite(Accel_Address, 0x32, data);
        }

        // Returns a string describing the current duration setting for the Interrupt 1 pin.        
        public static string GetDuration1()
        {
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x33);
            int val = (int)(MCUSBI2C.I2CRead(Accel_Address, 1)[0]);
            double v = val;
            v /= 10;
            return "" + v + "s";
        }

        // Sets the duration for the Interrupt 1 pin.
        public static void SetDuration1(double val)
        {
            ushort temp = (ushort)(val * 10);
            byte[] data = { (byte)temp };
            MCUSBI2C.I2CWrite(Accel_Address, 0x33, data);
        }

        // Returns an array of boolean values representing the status of the Interrupt 1 pin.
        // Boolean values represent overall interrupt, X axis, Y axis, and Z axis (in that order).
        public static bool[] GetInterruptStatus1()
        {
            bool[] status = new bool[4] { false, false, false, false };

            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x31);
            BitArray bits = new BitArray(MCUSBI2C.I2CRead(Accel_Address, 1));

            if(bits.Length == 8)
            {
                if(bits[6])
                {
                    status[0] = true;
                }
                if(bits[1] || bits[0])
                {
                    status[1] = true;
                }
                if(bits[3] || bits[2])
                {
                    status[2] = true;
                }
                if(bits[5] || bits[4])
                {
                    status[3] = true;
                }
            }

            return status;
        }

        // Returns a string describing the threshold value for the Interrupt 2 pin.
        public static string GetThreshold2()
        {
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x36);
            int val = (int)(MCUSBI2C.I2CRead(Accel_Address, 1)[0]) * 16;
            double v = val;
            v /= 1000;
            return "" + v + "g";
        }

        // Sets the threshold value for the Interrupt 2 pin.
        public static void SetThreshold2(double val)
        {
            ushort temp = (ushort)(val * 1000 / 16);
            byte[] data = { (byte)temp };
            MCUSBI2C.I2CWrite(Accel_Address, 0x36, data);
        }

        // Returns a string describing the duration for the Interrupt 2 pin.
        public static string GetDuration2()
        {
            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x37);
            int val = (int)(MCUSBI2C.I2CRead(Accel_Address, 1)[0]);
            double v = val;
            v /= 10;
            return "" + v + "s";
        }

        // Sets the duration for the Interrupt 2 pin.
        public static void SetDuration2(double val)
        {
            ushort temp = (ushort)(val * 10);
            byte[] data = { (byte)temp };
            MCUSBI2C.I2CWrite(Accel_Address, 0x37, data);
        }

        // Returns a boolean array representing the interrupt status for the Interrupt 2 pin.
        // Boolean values represent overall interrupt, X axis, Y axis, and Z axis (in that order).
        public static bool[] GetInterruptStatus2()
        {
            bool[] status = new bool[4] { false, false, false, false };

            MCUSBI2C.I2CWriteAddr(Accel_Address, 0x35);
            BitArray bits = new BitArray(MCUSBI2C.I2CRead(Accel_Address, 1));

            if (bits.Length == 8)
            {
                if (bits[6])
                {
                    status[0] = true;
                }
                if (bits[1] || bits[0])
                {
                    status[1] = true;
                }
                if (bits[3] || bits[2])
                {
                    status[2] = true;
                }
                if (bits[5] || bits[4])
                {
                    status[3] = true;
                }
            }

            return status;
        }
    }
}
