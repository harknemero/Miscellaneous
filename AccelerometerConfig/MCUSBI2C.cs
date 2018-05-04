using System;
using System.Linq;
using System.Text;
using mcp2221_dll_m;
using System.Threading;
using System.ComponentModel;
using System.Collections;

namespace AccelerometerConfig
{
    static class MCUSBI2C
    {
        //I2C Port constants
        private const uint MAXI2CSPEED = 400000;
        private const uint MINI2CSPEED = 46875;
        private const uint vid = 0x04D8;
        private const uint pid = 0x00DD;
        private const byte FLASH = 0;
        private const byte SRAM = 1;
        private const byte SEVENBITADDR = 1;
        //I2C Slave address
        private const byte PDBAddress = 0x30;
        private const byte UIA_Address = 0x32;
        private const byte UIB_Address = 0x34;
        private const byte FS_Address = 0x36;
        private const byte LCDADDRESS = 0x3E;
        private const byte XRAYSRC_ADD = 0x3A;
        private const byte BMS_Address = 0x55;

        // LCD Display constants
        private const byte LCDSENDCOMD = 0x00;
        private const byte LCDSENDDATA = 0x40;
        private const byte LCDCLEARDSP = 0x01;
        private const byte LCDRTRNHOME = 0x02;
        private const byte LCDFNCSETNM = 0x38;
        private const byte LCDFNCSETET = 0x39;
        private const byte LCDCRAMADDR = 0x40;
        private const byte LCDDRAMADDR = 0x80;

        // VMI Source constants
        private const byte KV_SETREG = 0x10;
        private const byte UA_SETREG = 0x12;
        private const byte READCOMMAND = 0x14;

        // I2C Error constants 
        public const int I2C_NO_ERROR = 0x0000;
        public const int I2C_NO_BRIDGE = 0x0001;
        public const int I2C_NOT_OPEN = 0x0002;
        public const int I2C_BAD_SETSPEED = 0x0004;
        public const int I2C_CFGERR = 0x0008;
        public const int I2C_DISPLAY_ERROR = 0x0010;

        private static IntPtr mHandle;
        private static byte POWERCFG = (byte)(MCP2221.M_MCP2221_USB_SELF | MCP2221.M_MCP2221_USB_REMOTE);
        private static bool DeviceOpen;
        private static byte OUTPUT = MCP2221.M_MCP2221_GPDIR_OUTPUT;
        private static byte INPUT = MCP2221.M_MCP2221_GPDIR_INPUT;
        private static byte NOTGPIO = MCP2221.M_MCP2221_GPFUNC_IO;
        private static byte GPIO = MCP2221.M_MCP2221_GPFUNC_IO;
        private static byte SSPND = MCP2221.M_MCP2221_GP_SSPND;
        private static byte USBCFG = MCP2221.M_MCP2221_GP_USBCFG;
        private static byte IOC = MCP2221.M_MCP2221_GP_IOC;
        private static byte LOW = MCP2221.M_MCP2221_GPVAL_LOW;
        private static byte HIGH = MCP2221.M_MCP2221_GPVAL_HIGH;
        private static readonly object locker = new object();
        private static byte[] GPIODIR = new byte[4] { INPUT, NOTGPIO, NOTGPIO, OUTPUT };
        private static byte[] GPIOFNC = new byte[4] { GPIO, IOC, USBCFG, GPIO };
        private static byte[] GPIOVAL = new byte[4] { LOW, NOTGPIO, NOTGPIO, LOW };
        private static uint i2cspeed;

        public struct DeviceInfoStruct
        {
            public string LibraryVersion;
            public string Manufacture;
            public string Product;
            public string Serial;
            public string FactorySN;
            public string HardwareVer;
            public string FirmwareRev;
        }
        public static DeviceInfoStruct DeviceInfo;

        static MCUSBI2C()
        {
            DeviceOpen = false;
            i2cspeed = MAXI2CSPEED;
        }

        //#region USB to I2C Bridge        
        public static int Open()            // Open USB to I2C bridge
        {
            int rtval;
            byte powerbyte = 0;
            uint currentbyte = 0;
            byte[] FPins = new byte[4];
            byte[] DPins = new byte[4];
            byte[] VPins = new byte[4];
            rtval = OpenbyIndex();
            if (rtval == MCP2221.M_E_NO_ERR)
            {
                // Check is self power and wake on int is enabled
                rtval = MCP2221.M_Mcp2221_GetUsbPowerAttributes(mHandle, ref powerbyte, ref currentbyte);
                if ((powerbyte & (MCP2221.M_MCP2221_USB_SELF | MCP2221.M_MCP2221_USB_REMOTE)) != POWERCFG)
                {
                    // Set self power and wake on int
                    rtval = rtval | MCP2221.M_Mcp2221_SetUsbPowerAttributes(mHandle, POWERCFG, currentbyte);
                    MCP2221.M_Mcp2221_Close(mHandle);
                    if (OpenbyIndex() != MCP2221.M_E_NO_ERR)
                    {
                        DeviceOpen = false;
                        MCP2221.M_Mcp2221_Reset(mHandle);
                        MCP2221.M_Mcp2221_CloseAll();
                        return I2C_NO_BRIDGE;
                    }
                }
                //int test = MCP2221.M_Mcp2221_SetAdvancedCommParams(mHandle, 3, 10);
                rtval = rtval | MCP2221.M_Mcp2221_GetGpioSettings(mHandle, SRAM, FPins, DPins, VPins);
                bool isFPins = FPins.SequenceEqual(GPIOFNC);
                bool isDpins = DPins.SequenceEqual(GPIODIR);
                if ((isFPins == false) || (isDpins == false))
                {
                    rtval = rtval | MCP2221.M_Mcp2221_SetGpioSettings(mHandle, FLASH, GPIOFNC, GPIODIR, GPIOVAL);
                    rtval = rtval | MCP2221.M_Mcp2221_SetGpioSettings(mHandle, SRAM, GPIOFNC, GPIODIR, GPIOVAL);
                }
                else
                {
                    rtval = rtval | MCP2221.M_Mcp2221_SetGpioValues(mHandle, GPIOVAL);
                }
                rtval = rtval | MCP2221.M_Mcp2221_ClearInterruptPinFlag(mHandle);
                if (rtval != MCP2221.M_E_NO_ERR)
                {
                    MCP2221.M_Mcp2221_Reset(mHandle);
                    return I2C_CFGERR;
                }
                DeviceOpen = true;
            }
            else
            {
                DeviceOpen = false;
                return I2C_NO_BRIDGE;
            }
            return I2C_NO_ERROR;
        }

        private static int OpenbyIndex()
        {
            int rtval;
            uint nDev = 0;
            DeviceInfo.LibraryVersion = MCP2221.M_Mcp2221_GetLibraryVersion();
            rtval = MCP2221.M_Mcp2221_GetConnectedDevices(vid, pid, ref nDev);
            if (rtval == MCP2221.M_E_NO_ERR)
            {
                mHandle = MCP2221.M_Mcp2221_OpenByIndex(vid, pid, nDev - 1);
                rtval = MCP2221.M_Mcp2221_GetLastError();
                if (rtval == MCP2221.M_E_NO_ERR)
                {
                    DeviceInfo.Manufacture = MCP2221.M_Mcp2221_GetManufacturerDescriptor(mHandle);
                    DeviceInfo.Product = MCP2221.M_Mcp2221_GetProductDescriptor(mHandle);
                    DeviceInfo.Serial = MCP2221.M_Mcp2221_GetSerialNumberDescriptor(mHandle);
                    DeviceInfo.FactorySN = MCP2221.M_Mcp2221_GetFactorySerialNumber(mHandle);
                    DeviceInfo.HardwareVer = MCP2221.M_Mcp2221_GetHardwareRevision(mHandle);
                    DeviceInfo.FirmwareRev = MCP2221.M_Mcp2221_GetFirmwareRevision(mHandle);
                    rtval = MCP2221.M_Mcp2221_GetLastError();
                    if (rtval == MCP2221.M_E_NO_ERR)
                    {
                        int rval = MCP2221.M_Mcp2221_SetSpeed(mHandle, i2cspeed);
                        int loop = 100;
                        while (rval != MCP2221.M_E_NO_ERR)
                        {
                            MCP2221.M_Mcp2221_I2cCancelCurrentTransfer(mHandle);
                            MCP2221.M_Mcp2221_Reset(mHandle);
                            MCP2221.M_Mcp2221_GetConnectedDevices(vid, pid, ref nDev);
                            mHandle = MCP2221.M_Mcp2221_OpenByIndex(vid, pid, nDev - 1);
                            rval = MCP2221.M_Mcp2221_SetSpeed(mHandle, i2cspeed);
                            loop--;
                            if (loop == 0) break;
                        }
                    }

                }
            }
            return rtval;
        }

        public static int I2CSpeed(uint value)
        {
            lock (locker)
            {
                if ((value <= MAXI2CSPEED) && (value >= MINI2CSPEED) && DeviceOpen)
                {
                    int rtval = MCP2221.M_Mcp2221_SetSpeed(mHandle, value);
                    if (rtval != MCP2221.M_E_NO_ERR) return I2C_BAD_SETSPEED;
                }
                return I2C_NO_ERROR;
            }

        }

        public static bool IsDeviceOpen()
        {
            return DeviceOpen;
        }

        public static void Close()
        {
            if (DeviceOpen)
            {
                int rtval = MCP2221.M_Mcp2221_CloseAll();
                if (rtval == I2C_NO_ERROR) DeviceOpen = false;
            }
        }

        public static int I2CWriteAddr(byte i2caddr, byte regaddr)
        {
            lock (locker)
            {
                int rtval;
                byte[] dbuffer = new byte[1];
                dbuffer[0] = regaddr;
                rtval = MCP2221.M_Mcp2221_I2cWrite(mHandle, (uint)dbuffer.Length, i2caddr, SEVENBITADDR, dbuffer);
                if (rtval != I2C_NO_ERROR) MCP2221.M_Mcp2221_I2cCancelCurrentTransfer(mHandle);
                return rtval;
            }

        }

        public static int I2CWrite(byte i2caddr, byte regaddr, byte[] data)
        {
            lock (locker)
            {
                int rtval;
                byte[] dbuffer = new byte[1 + data.Length];
                Buffer.BlockCopy(data, 0, dbuffer, 1, data.Length);
                dbuffer[0] = regaddr;
                rtval = MCP2221.M_Mcp2221_I2cWrite(mHandle, (uint)dbuffer.Length, i2caddr, SEVENBITADDR, dbuffer);
                if (rtval != I2C_NO_ERROR)
                {
                    MCP2221.M_Mcp2221_I2cCancelCurrentTransfer(mHandle);
                }
                return rtval;
            }
        }

        public static byte[] I2CRead(byte i2caddr, uint length)
        {
            lock (locker)
            {
                int rtval;
                byte[] dbuffer = new byte[length];
                rtval = MCP2221.M_Mcp2221_I2cRead(mHandle, length, i2caddr, SEVENBITADDR, dbuffer);
                if (rtval != I2C_NO_ERROR) MCP2221.M_Mcp2221_I2cCancelCurrentTransfer(mHandle);
                return dbuffer;
            }
        }
    }
}

