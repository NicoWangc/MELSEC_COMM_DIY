using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Timers;

namespace MELSEC_COMM_DIY
{
    public static class SerialCommunications
    {
        public static SerialPort SP_ReadData = null;
        public static string msg;//接收的数据
        public static string PortName = "COM9";
        public static int BaudRate = 115200;
        public static int DataBits = 7;
        public static StopBits StopBits = StopBits.One;
        public static Parity ParitySensor = Parity.Even;
        public static byte[] send = null;

        public static void SP_ReadData_DataReceived2(object sender, SerialDataReceivedEventArgs e)
        {
            // 接收数据
            List<byte> buffer = new List<byte>();
            byte[] data = new byte[1024];
            while (true)
            {
                System.Threading.Thread.Sleep(5);
                if (SP_ReadData.BytesToRead < 1)
                {
                    break;
                }

                int recCount = SP_ReadData.Read(data, 0, SP_ReadData.BytesToRead);

                byte[] buffer2 = new byte[recCount];
                Array.Copy(data, 0, buffer2, 0, recCount);
                buffer.AddRange(buffer2);
            }

            if (buffer.Count == 0)
                return;
            byte[] str = buffer.ToArray();//2 51 52 49 50 3 67 68
            msg = CommunicationsFXSerial.ByteArray2Hex(str); 
        }

        public static void SP_ReadData_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {//一旦接收到数据，该事件便会触发，一次传输会触发多次
            int ReadCount = SP_ReadData.BytesToRead;//本轮事件触发时的剩余字节数
            int ReadCountOld = ReadCount;//上一轮事件触发时的剩余字节数
            int ReadTimes = 0;
            do
            {
                Thread.Sleep(10);
                ReadCountOld = ReadCount;
                ReadCount = SP_ReadData.BytesToRead;
                ReadTimes += 1;
            } while (ReadCount != ReadCountOld);
            //确定数据缓冲结束，开始读取
            List<byte> buffer = new List<byte>();
            byte[] data = new byte[ReadCount];
            //string strRes = SP_ReadData.ReadExisting();
            int recCount = SP_ReadData.Read(data, 0, ReadCount);
            //byte[] buffer2 = new byte[recCount];
            //Array.Copy(data, 0, buffer2, 0, recCount);
            //buffer.AddRange(buffer2);

            if (data.Count() == 0)
                return;
            byte[] str = data.ToArray();//2 51 52 49 50 3 67 68
            msg = CommunicationsFXSerial.ByteArray2Hex(str); 
        }

        public static void OpenDevice()
        {
            try
            {
                SP_ReadData = new SerialPort();
                SP_ReadData.PortName = PortName;
                SP_ReadData.BaudRate = BaudRate;
                SP_ReadData.DataBits = DataBits;
                SP_ReadData.StopBits = StopBits;
                SP_ReadData.Parity = ParitySensor;
                SP_ReadData.DataReceived += SP_ReadData_DataReceived;
                if (SP_ReadData.IsOpen)
                {
                    SP_ReadData.Close();
                }
                SP_ReadData.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            

        }

        public static void WriteDevice(string addr, int value)
        {
            addr = CommunicationsFXSerial.TransAddrD(addr);
            string WriteCode = CommunicationsFXSerial.SendData(CommunicationsFXSerial.CMD_Write, addr, 2, value, CommunicationsFXSerial.EXT);
            send = CommunicationsFXSerial.HexStr2ByteArray(WriteCode);//"02313137443030343334313237383536033138"
            SP_ReadData.Write(send, 0, send.Length);
            //try
            //{
                
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //}
          }
        public static int ReadDevice(string addr)
        {
            int intRes = 0;
            try
            {
                intRes = ReadDeviceSingle(addr);
            }
            catch (Exception ex)
            {//如果错误，重读一次
                intRes = ReadDeviceSingle(addr);
            }
            return intRes;
        }

        /// <summary>
        /// 读D区软元件的数值
        /// </summary>
        /// <param name="addr">D区软元件的地址，如D100</param>
        /// <returns>返回读到的数据，字符串形式</returns>
        public static int ReadDeviceSingle(string addr)
        {
            addr = CommunicationsFXSerial.TransAddrD(addr);
            string ReadCode = CommunicationsFXSerial.SendData(CommunicationsFXSerial.CMD_Read, addr, 2, CommunicationsFXSerial.EXT);
            send = CommunicationsFXSerial.HexStr2ByteArray(ReadCode);//"0230313744303032033731"
            byte[] result;
            SP_ReadData.Write(send, 0, send.Length);
            //try
            //{
            //此处使用try...Catch会让程序变慢，有概率无法捕捉到数据
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //}
            Thread.Sleep(20);
            if (msg != "")//"0233343132034344"
            {
                //result = Encoding.ASCII.GetString(HslCommunication.BasicFramework.
                //    SoftBasic.ByteToHexString(msg)).ToString();
                result = CommunicationsFXSerial.HexStr2ByteArray(msg);
                string res = ((char)result[3]).ToString() + ((char)result[4]).ToString() + ((char)result[1]).ToString() + ((char)result[2]).ToString();
                int resInt = Convert.ToInt32(CommunicationsFXSerial.HexStr2Dec(res));
                return resInt;
            }
            else
            {
                return 9999;
            }
        }
    }
}
