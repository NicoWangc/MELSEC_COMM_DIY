using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Net.Mime;

namespace MitsubishiFxPlc
{
    public class Cls_Fx2N
    {
        private SerialPort sp = new SerialPort();

        public string spStatus;
        public string spActive; //是否启用
        public string PlcName;     //PLC名称
        public int PlcDnum;        //D元件个数
        public int PlcColNum;      //D元件每行个数
        public int PlcDRead;       //D元件读取起始地址
        public int PlcDWrite;      //D元件写入起始地址
        public int PlcMWrite;      //M元件写入起始地址
        public int PlcMbyte;       //M元件使用字节数
        public int PlcXRead;       //X元件读取起始地址
        public int PlcXbyte;       //X元件使用字节数
        public int PlcYRead;       //Y元件读取起始地址
        public int PlcYbyte;       //Y元件使用字节数

        //位强制ON/OFF
        private string BF_Address;
        //位读
        private string BR_Address;
        //C读
        private string CR_Address;
        //D读/写
        private string DR_Address;

        private string PLCBuffer;
        //Private PortOpen As Boolean

        private string ReadStr;
        public int M_timeout;

        public Cls_Fx2N()
        {
            sp.ReadTimeout = 1000;
            sp.WriteTimeout = 1000;
            M_timeout = 500;

            sp.BaudRate = 115200;
            sp.Parity = System.IO.Ports.Parity.Even;
            //设置奇偶校验位，使位数等于偶数
            sp.DataBits = 7;
            //每个字节的标准数据位长度
            sp.StopBits = System.IO.Ports.StopBits.One;
            //使用的停止位数
            sp.ReadBufferSize = 1024;
            //输入缓冲区的大小
            sp.WriteBufferSize = 1024;
            //输出缓冲区的大小

            sp.PortName = "COM9";
            //通信端口           
            spActive = "Y";
        }


        public int BaudRate
        {
            get
            {
                return sp.BaudRate;
            }
            set
            {
                BaudRate = value;
            }
        }

        public int TimeOut
        {
            get { return sp.ReadTimeout; }
            set
            {
                sp.ReadTimeout = value;
                sp.WriteTimeout = value;
            }
        }

        public bool PortOpenState
        {
            get { return sp.IsOpen; }
        }

        //Property 属性
        public int CommPort
        {
            get
            {
                return Convert.ToInt32(sp.PortName.Substring(3, 1));
            }

            set
            {
                try
                {
                    if (sp.IsOpen)
                        throw new Exception("串行端口已经打开!");

                    sp.PortName = "COM" + value.ToString();
                }
                catch (Exception)
                {
                    throw new Exception("端口错误");
                }
            }
        }

        public void OpenPort()
        {
            if (!sp.IsOpen)
            {
                sp.ReadTimeout = 1000;
                sp.WriteTimeout = 1000;
                try
                {
                    sp.Open();
                }
                catch (Exception err)
                {
                    spStatus = "串行端口打开错误 " + CommPort + ": " + err.Message;
                }
                spStatus = "串行端口" + CommPort + " 打开成功";
            }
            else {
                spStatus = "串行端口" + CommPort + " 已经打开";
            }
        }


        public bool ClosePort()
        {
            //Ensure port is opened before attempting to close:
            if (sp.IsOpen)
            {
                try
                {
                    sp.Close();
                }
                catch (Exception err)
                {
                    spStatus = "串行端口打开错误 " + CommPort + ": " + err.Message;
                    return false;
                }
                spStatus = sp.PortName + " 串行端口关闭成功";
                return true;
            }
            else {
                spStatus = sp.PortName + " 串行端口没有打开";
                return false;
            }
        }



        /// <summary>
        /// BitType--输入类型M-0,X-1,Y-2，Typenum为字节个数，范围为01H-40H(1-64个字节)
        /// </summary>
        /// <param name="BitType"></param>
        /// <param name="BitAd"></param>
        /// <param name="Typenum"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string BitValue(int BitType, int BitAd, int Typenum)
        {
            //位元件查询,一般用来查询状态
            lock (sp)
            {
                BR_Address = CbitAdd(BitType, BitAd);
                string str_Read = "";
                string sReceiveData = "";
                if (sp.IsOpen)
                {
                    sp.DiscardInBuffer();
                    sp.DiscardOutBuffer();

                    string buffer;
                    string SunStr;

                    SunStr = "0" + BR_Address;
                    SunStr += Typenum.ToString("X").PadLeft(2, '0');

                    buffer = Chr(2) + SunStr + Chr(3) + SunCheck(SunStr);


                    try
                    {
                        sp.WriteLine(buffer);

                        System.Threading.Thread.Sleep(200);
                        sReceiveData = sp.ReadExisting(); //返回格式  chr(2)+data+chr(3)+ 校验码

                        sReceiveData = sReceiveData.Substring(1, sReceiveData.Length - 4);

                        for (int i = 0; i <= sReceiveData.Length - 1; i++)
                        {
                            str_Read += ProceStrLen(sReceiveData[i].ToString());
                        }
                        if (str_Read == "")
                        {
                            spStatus = "返回空值 PLC与系统通讯";
                            return str_Read;
                        }
                    }
                    catch (Exception err)
                    {
                        spStatus = "PLC发送读取指令错误: " + err.Message;
                        return "";
                    }
                }
                else
                {
                    spStatus = "串口未打开，请检查串口！";
                    return "";
                }
                return str_Read;
            }

        }


        /// <summary>
        /// BitType--输入类型M-0,X-1,Y-2，Typenum为字节个数，范围为01H-40H(1-64个字节)
        /// </summary>
        /// <param name="BitType"></param>
        /// <param name="BitAd"></param>
        /// <param name="Typenum"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string WriteValue(int BitType, int BitAd, int Typenum, string SendData)
        {
            //位元件查询,一般用来查询状态
            lock (sp)
            {
                if (BitType == 99)
                {
                    BR_Address = CDAdd(BitAd);
                }
                else
                {
                    BR_Address = CbitAdd(BitType, BitAd);
                }

                if (sp.IsOpen)
                {
                    sp.DiscardInBuffer();
                    sp.DiscardOutBuffer();

                    string buffer;
                    string SunStr;

                    SunStr = "1" + BR_Address;
                    SunStr += Typenum.ToString("X").PadLeft(2, '0');

                    //将二进制字符串转换为16进制字符串
                    SunStr = SunStr + Convert.ToInt32(SendData,2).ToString("X").ToUpper();

                    buffer = Chr(2) + SunStr + Chr(3) + SunCheck(SunStr);

                    try
                    {
                        sp.WriteLine(buffer);

                        System.Threading.Thread.Sleep(200);
                        string str_Read = sp.ReadExisting();
                        if (str_Read == "")
                        {
                            spStatus = "写入PLC返回空值!";        
                            return "PLC写入失败";
                        }
                        if (Asc(str_Read) != 6)
                        {
                            spStatus = "校验失败";
                            return "PLC写入失败";
                        }
                            
                        return "PLC写入成功!";
                    }
                    catch (Exception err)
                    {
                        spStatus = "PLC发送读取指令错误: " + err.Message;
                        return "";
                    }
                }
                else
                {
                    spStatus = "串口未打开，请检查串口！";
                    return "";
                }

            }
        }


        /// <summary>
        /// BitType--输入类型M-0,X-1,Y-2，Typenum为字节个数，范围为01H-40H(1-64个字节)
        /// </summary>
        /// <param name="BitType"></param>
        /// <param name="BitAd"></param>
        /// <param name="Typenum"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string WriteValue(int BitType, int BitAd, int Typenum, int SendData)
        {
            //位元件查询,一般用来查询状态
            lock (sp)
            {
                if (BitType == 99)
                {
                    BR_Address = CDAdd(BitAd);
                }
                else
                {
                    BR_Address = CbitAdd(BitType, BitAd);
                }

                if (sp.IsOpen)
                {
                    sp.DiscardInBuffer();
                    sp.DiscardOutBuffer();

                    string buffer;
                    string SunStr;

                    SunStr = "1" + BR_Address;
                    SunStr += Typenum.ToString("X").PadLeft(2, '0');

                    SunStr = SunStr + ValueToHex(SendData);

                    buffer = Chr(2) + SunStr + Chr(3) + SunCheck(SunStr);


                    try
                    {
                        sp.WriteLine(buffer);

                        System.Threading.Thread.Sleep(200);
                        string str_Read = sp.ReadExisting();
                        if (str_Read == "")
                        {
                            spStatus = "写入PLC返回空值!";
                            return "PLC写入失败";
                        }
                        if (Asc(str_Read) != 6)
                        {
                            spStatus = "校验失败";
                            return "PLC写入失败";
                        }
                        return "PLC写入成功!";
                    }
                    catch (Exception err)
                    {
                        spStatus = "PLC发送读取指令错误: " + err.Message;
                        return err.Message;
                    }
                }
                else
                {
                    spStatus = "串口未打开，请检查串口！";
                    return "";
                }
            }
        }



        /// <summary>
        /// Dvalue16-批量读16位数据,返回整型数组，Ad-指地址，num为从Ad开始读的多少个数
        /// </summary>
        /// <param name="Ad"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public int[] Dvalue16(int Ad, int num)
        {
            //D元件批量读取 ,检测状态
            string s = "";
            DR_Address = CDAdd(Ad);

            //int typenum = num + 1;
            int typenum = num;

            int[] numArray = new int[typenum];
            string str_Read = "";
            string D_AcceptData = "";
            lock (sp)
            {
                if (sp.IsOpen)
                {
                    sp.DiscardInBuffer();
                    sp.DiscardOutBuffer();

                    string buffer;
                    string SunStr;

                    //SunStr = "0" + "E00" + DR_Address;
                    SunStr = "0" + DR_Address;
                    SunStr += (typenum * 2).ToString("X").PadLeft(2, '0');

                    buffer = Chr(2) + SunStr + Chr(3) + SunCheck(SunStr);




                    try
                    {
                        sp.WriteLine(buffer);
                        //                         while (sp.BytesToRead < 36)
                        //                        {
                        //                            System.Threading.Thread.Sleep(10);
                        //                        }}
                        System.Threading.Thread.Sleep(200);
                        D_AcceptData = sp.ReadExisting(); //返回格式  chr(2)+data+chr(3)+ 校验码

                        //sReceiveData = sReceiveData.Substring(1, sReceiveData.Length - (num - 1));
                        D_AcceptData = D_AcceptData.Substring(1, (D_AcceptData.Length - 4));

                        for (int i = 0; i <= D_AcceptData.Length - 1; i++)
                        {
                            str_Read += ProceStrLen(D_AcceptData[i].ToString());
                        }

                        if (str_Read == "")
                        {
                            spStatus = "返回空值 PLC与系统通讯";
                            return numArray;
                        }

                        //给数组赋值
                        for (int i = 0; i <= typenum - 1; i++)
                        {
                            string D_strL = D_AcceptData.Substring(i * 4, 4).Substring(0, 2);
                            string D_strH = D_AcceptData.Substring(i * 4, 4).Substring(2, 2);

                            string D_str = D_strH + D_strL;
                            if (D_str == "")
                            {
                                D_str = "0";
                            }
                            numArray[i] = HexToValue(D_str);
                        }
                    }
                    catch (Exception err)
                    {
                        spStatus = "PLC发送读取指令错误: " + err.Message;
                    }

                }
                else {
                    spStatus = "串口未打开，请检查串口！";
                }
                return numArray;
            }

            //读D10
            //02 45 30 30 34 30 31 34 30 32 03 44 33
            //02 33 34 31 32 03 43 44 
        }


        /// <summary>
        /// CbitAdd-指寄存器地址查询，BitType-位类型，BitAd-位地址
        /// </summary>
        /// <param name="BitType"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string CbitAdd(int BitType, int BitAd)
        {
            switch (BitType)
            {
                case 1:
                    //"X"
                    switch (BitAd)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            this.BR_Address = "0080";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                        case 16:
                        case 17:
                            this.BR_Address = "0081";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 20:
                        case 21:
                        case 22:
                        case 23:
                        case 24:
                        case 25:
                        case 26:
                        case 27:
                            this.BR_Address = "0082";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 30:
                        case 31:
                        case 32:
                        case 33:
                        case 34:
                        case 35:
                        case 36:
                        case 37:
                            this.BR_Address = "0083";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 40:
                        case 41:
                        case 42:
                        case 43:
                        case 44:
                        case 45:
                        case 46:
                        case 47:
                            this.BR_Address = "0084";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                        case 56:
                        case 57:
                            this.BR_Address = "0085";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 60:
                        case 61:
                        case 62:
                        case 63:
                        case 64:
                        case 65:
                        case 66:
                        case 67:
                            this.BR_Address = "0086";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 70:
                        case 71:
                        case 72:
                        case 73:
                        case 74:
                        case 75:
                        case 76:
                        case 77:
                            this.BR_Address = "0087";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 100:
                        case 101:
                        case 102:
                        case 103:
                        case 104:
                        case 105:
                        case 106:
                        case 107:
                            this.BR_Address = "0088";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 110:
                        case 111:
                        case 112:
                        case 113:
                        case 114:
                        case 115:
                        case 116:
                        case 117:
                            this.BR_Address = "0089";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 120:
                        case 121:
                        case 122:
                        case 123:
                        case 124:
                        case 125:
                        case 126:
                        case 127:
                            this.BR_Address = "008A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 130:
                        case 131:
                        case 132:
                        case 133:
                        case 134:
                        case 135:
                        case 136:
                        case 137:
                            this.BR_Address = "008B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 140:
                        case 141:
                        case 142:
                        case 143:
                        case 144:
                        case 145:
                        case 146:
                        case 147:
                            this.BR_Address = "008C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 150:
                        case 151:
                        case 152:
                        case 153:
                        case 154:
                        case 155:
                        case 156:
                        case 157:
                            this.BR_Address = "008D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 160:
                        case 161:
                        case 162:
                        case 163:
                        case 164:
                        case 165:
                        case 166:
                        case 167:
                            this.BR_Address = "008E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 170:
                        case 171:
                        case 172:
                        case 173:
                        case 174:
                        case 175:
                        case 176:
                        case 177:
                            this.BR_Address = "008F";
                            break; // TODO: might not be correct. Was : Exit Select

                    }
                    break; // TODO: might not be correct. Was : Exit Select

                case 2:
                    //"Y"
                    switch (BitAd)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            this.BR_Address = "00A0";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                        case 16:
                        case 17:
                            this.BR_Address = "00A1";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 20:
                        case 21:
                        case 22:
                        case 23:
                        case 24:
                        case 25:
                        case 26:
                        case 27:
                            this.BR_Address = "00A2";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 30:
                        case 31:
                        case 32:
                        case 33:
                        case 34:
                        case 35:
                        case 36:
                        case 37:
                            this.BR_Address = "00A3";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 40:
                        case 41:
                        case 42:
                        case 43:
                        case 44:
                        case 45:
                        case 46:
                        case 47:
                            this.BR_Address = "00A4";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                        case 56:
                        case 57:
                            this.BR_Address = "00A5";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 60:
                        case 61:
                        case 62:
                        case 63:
                        case 64:
                        case 65:
                        case 66:
                        case 67:
                            this.BR_Address = "00A6";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 70:
                        case 71:
                        case 72:
                        case 73:
                        case 74:
                        case 75:
                        case 76:
                        case 77:
                            this.BR_Address = "00A7";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 100:
                        case 101:
                        case 102:
                        case 103:
                        case 104:
                        case 105:
                        case 106:
                        case 107:
                            this.BR_Address = "00A8";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 110:
                        case 111:
                        case 112:
                        case 113:
                        case 114:
                        case 115:
                        case 116:
                        case 117:
                            this.BR_Address = "00A9";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 120:
                        case 121:
                        case 122:
                        case 123:
                        case 124:
                        case 125:
                        case 126:
                        case 127:
                            this.BR_Address = "00AA";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 130:
                        case 131:
                        case 132:
                        case 133:
                        case 134:
                        case 135:
                        case 136:
                        case 137:
                            this.BR_Address = "00AB";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 140:
                        case 141:
                        case 142:
                        case 143:
                        case 144:
                        case 145:
                        case 146:
                        case 147:
                            this.BR_Address = "00AC";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 150:
                        case 151:
                        case 152:
                        case 153:
                        case 154:
                        case 155:
                        case 156:
                        case 157:
                            this.BR_Address = "00AD";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 160:
                        case 161:
                        case 162:
                        case 163:
                        case 164:
                        case 165:
                        case 166:
                        case 167:
                            this.BR_Address = "00AE";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 170:
                        case 171:
                        case 172:
                        case 173:
                        case 174:
                        case 175:
                        case 176:
                        case 177:
                            this.BR_Address = "00AF";
                            break; // TODO: might not be correct. Was : Exit Select

                    }
                    break; // TODO: might not be correct. Was : Exit Select

                case 0:
                    //"M"

                    switch (BitAd)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            this.BR_Address = "0100";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                            this.BR_Address = "0101";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 16:
                        case 17:
                        case 18:
                        case 19:
                        case 20:
                        case 21:
                        case 22:
                        case 23:
                            this.BR_Address = "0102";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 24:
                        case 25:
                        case 26:
                        case 27:
                        case 28:
                        case 29:
                        case 30:
                        case 31:
                            this.BR_Address = "0103";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 32:
                        case 33:
                        case 34:
                        case 35:
                        case 36:
                        case 37:
                        case 38:
                        case 39:
                            this.BR_Address = "0104";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 40:
                        case 41:
                        case 42:
                        case 43:
                        case 44:
                        case 45:
                        case 46:
                        case 47:
                            this.BR_Address = "0105";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 48:
                        case 49:
                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                            this.BR_Address = "0106";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 56:
                        case 57:
                        case 58:
                        case 59:
                        case 60:
                        case 61:
                        case 62:
                        case 63:
                            this.BR_Address = "0107";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 64:
                        case 65:
                        case 66:
                        case 67:
                        case 68:
                        case 69:
                        case 70:
                        case 71:
                            this.BR_Address = "0108";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 72:
                        case 73:
                        case 74:
                        case 75:
                        case 76:
                        case 77:
                        case 78:
                        case 79:
                            this.BR_Address = "0109";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 80:
                        case 81:
                        case 82:
                        case 83:
                        case 84:
                        case 85:
                        case 86:
                        case 87:
                            this.BR_Address = "010A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 88:
                        case 89:
                        case 90:
                        case 91:
                        case 92:
                        case 93:
                        case 94:
                        case 95:
                            this.BR_Address = "010B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 96:
                        case 97:
                        case 98:
                        case 99:
                        case 100:
                        case 101:
                        case 102:
                        case 103:
                            this.BR_Address = "010C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 104:
                        case 105:
                        case 106:
                        case 107:
                        case 108:
                        case 109:
                        case 110:
                        case 111:
                            this.BR_Address = "010D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 112:
                        case 113:
                        case 114:
                        case 115:
                        case 116:
                        case 117:
                        case 118:
                        case 119:
                            this.BR_Address = "010E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 120:
                        case 121:
                        case 122:
                        case 123:
                        case 124:
                        case 125:
                        case 126:
                        case 127:
                            this.BR_Address = "010F";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 128:
                        case 129:
                        case 130:
                        case 131:
                        case 132:
                        case 133:
                        case 134:
                        case 135:
                            this.BR_Address = "0110";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 136:
                        case 137:
                        case 138:
                        case 139:
                        case 140:
                        case 141:
                        case 142:
                        case 143:
                            this.BR_Address = "0111";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 144:
                        case 145:
                        case 146:
                        case 147:
                        case 148:
                        case 149:
                        case 150:
                        case 151:
                            this.BR_Address = "0112";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 152:
                        case 153:
                        case 154:
                        case 155:
                        case 156:
                        case 157:
                        case 158:
                        case 159:
                            this.BR_Address = "0113";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 160:
                        case 161:
                        case 162:
                        case 163:
                        case 164:
                        case 165:
                        case 166:
                        case 167:
                            this.BR_Address = "0114";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 168:
                        case 169:
                        case 170:
                        case 171:
                        case 172:
                        case 173:
                        case 174:
                        case 175:
                            this.BR_Address = "0115";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 176:
                        case 177:
                        case 178:
                        case 179:
                        case 180:
                        case 181:
                        case 182:
                        case 183:
                            this.BR_Address = "0116";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 184:
                        case 185:
                        case 186:
                        case 187:
                        case 188:
                        case 189:
                        case 190:
                        case 191:
                            this.BR_Address = "0117";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 192:
                        case 193:
                        case 194:
                        case 195:
                        case 196:
                        case 197:
                        case 198:
                        case 199:
                            this.BR_Address = "0118";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 200:
                        case 201:
                        case 202:
                        case 203:
                        case 204:
                        case 205:
                        case 206:
                        case 207:
                            this.BR_Address = "0119";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 208:
                        case 209:
                        case 210:
                        case 211:
                        case 212:
                        case 213:
                        case 214:
                        case 215:
                            this.BR_Address = "011A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 216:
                        case 217:
                        case 218:
                        case 219:
                        case 220:
                        case 221:
                        case 222:
                        case 223:
                            this.BR_Address = "011B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 224:
                        case 225:
                        case 226:
                        case 227:
                        case 228:
                        case 229:
                        case 230:
                        case 231:
                            this.BR_Address = "011C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 232:
                        case 233:
                        case 234:
                        case 235:
                        case 236:
                        case 237:
                        case 238:
                        case 239:
                            this.BR_Address = "011D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 240:
                        case 241:
                        case 242:
                        case 243:
                        case 244:
                        case 245:
                        case 246:
                        case 247:
                            this.BR_Address = "011E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 248:
                        case 249:
                        case 250:
                        case 251:
                        case 252:
                        case 253:
                        case 254:
                        case 255:
                            this.BR_Address = "011F";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 256:
                        case 257:
                        case 258:
                        case 259:
                        case 260:
                        case 261:
                        case 262:
                        case 263:
                            this.BR_Address = "0120";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 264:
                        case 265:
                        case 266:
                        case 267:
                        case 268:
                        case 269:
                        case 270:
                        case 271:
                            this.BR_Address = "0121";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 272:
                        case 273:
                        case 274:
                        case 275:
                        case 276:
                        case 277:
                        case 278:
                        case 279:
                            this.BR_Address = "0122";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 280:
                        case 281:
                        case 282:
                        case 283:
                        case 284:
                        case 285:
                        case 286:
                        case 287:
                            this.BR_Address = "0123";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 288:
                        case 289:
                        case 290:
                        case 291:
                        case 292:
                        case 293:
                        case 294:
                        case 295:
                            this.BR_Address = "0124";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 296:
                        case 297:
                        case 298:
                        case 299:
                        case 300:
                        case 301:
                        case 302:
                        case 303:
                            this.BR_Address = "0125";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 304:
                        case 305:
                        case 306:
                        case 307:
                        case 308:
                        case 309:
                        case 310:
                        case 311:
                            this.BR_Address = "0126";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 312:
                        case 313:
                        case 314:
                        case 315:
                        case 316:
                        case 317:
                        case 318:
                        case 319:
                            this.BR_Address = "0127";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 320:
                        case 321:
                        case 322:
                        case 323:
                        case 324:
                        case 325:
                        case 326:
                        case 327:
                            this.BR_Address = "0128";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 328:
                        case 329:
                        case 330:
                        case 331:
                        case 332:
                        case 333:
                        case 334:
                        case 335:
                            this.BR_Address = "0129";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 336:
                        case 337:
                        case 338:
                        case 339:
                        case 340:
                        case 341:
                        case 342:
                        case 343:
                            this.BR_Address = "012A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 344:
                        case 345:
                        case 346:
                        case 347:
                        case 348:
                        case 349:
                        case 350:
                        case 351:
                            this.BR_Address = "012B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 352:
                        case 353:
                        case 354:
                        case 355:
                        case 356:
                        case 357:
                        case 358:
                        case 359:
                            this.BR_Address = "012C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 360:
                        case 361:
                        case 362:
                        case 363:
                        case 364:
                        case 365:
                        case 366:
                        case 367:
                            this.BR_Address = "012D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 368:
                        case 369:
                        case 370:
                        case 371:
                        case 372:
                        case 373:
                        case 374:
                        case 375:
                            this.BR_Address = "012E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 376:
                        case 377:
                        case 378:
                        case 379:
                        case 380:
                        case 381:
                        case 382:
                        case 383:
                            this.BR_Address = "012F";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 384:
                        case 385:
                        case 386:
                        case 387:
                        case 388:
                        case 389:
                        case 390:
                        case 391:
                            this.BR_Address = "0130";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 392:
                        case 393:
                        case 394:
                        case 395:
                        case 396:
                        case 397:
                        case 398:
                        case 399:
                            this.BR_Address = "0131";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 400:
                        case 401:
                        case 402:
                        case 403:
                        case 404:
                        case 405:
                        case 406:
                        case 407:
                            this.BR_Address = "0132";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 408:
                        case 409:
                        case 410:
                        case 411:
                        case 412:
                        case 413:
                        case 414:
                        case 415:
                            this.BR_Address = "0133";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 416:
                        case 417:
                        case 418:
                        case 419:
                        case 420:
                        case 421:
                        case 422:
                        case 423:
                            this.BR_Address = "0134";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 424:
                        case 425:
                        case 426:
                        case 427:
                        case 428:
                        case 429:
                        case 430:
                        case 431:
                            this.BR_Address = "0135";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 432:
                        case 433:
                        case 434:
                        case 435:
                        case 436:
                        case 437:
                        case 438:
                        case 439:
                            this.BR_Address = "0136";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 440:
                        case 441:
                        case 442:
                        case 443:
                        case 444:
                        case 445:
                        case 446:
                        case 447:
                            this.BR_Address = "0137";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 448:
                        case 449:
                        case 450:
                        case 451:
                        case 452:
                        case 453:
                        case 454:
                        case 455:
                            this.BR_Address = "0138";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 456:
                        case 457:
                        case 458:
                        case 459:
                        case 460:
                        case 461:
                        case 462:
                        case 463:
                            this.BR_Address = "0139";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 464:
                        case 465:
                        case 466:
                        case 467:
                        case 468:
                        case 469:
                        case 470:
                        case 471:
                            this.BR_Address = "013A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 472:
                        case 473:
                        case 474:
                        case 475:
                        case 476:
                        case 477:
                        case 478:
                        case 479:
                            this.BR_Address = "013B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 480:
                        case 481:
                        case 482:
                        case 483:
                        case 484:
                        case 485:
                        case 486:
                        case 487:
                            this.BR_Address = "013C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 488:
                        case 489:
                        case 490:
                        case 491:
                        case 492:
                        case 493:
                        case 494:
                        case 495:
                            this.BR_Address = "013D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 496:
                        case 497:
                        case 498:
                        case 499:
                        case 500:
                        case 501:
                        case 502:
                        case 503:
                            this.BR_Address = "013E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 504:
                        case 505:
                        case 506:
                        case 507:
                        case 508:
                        case 509:
                        case 510:
                        case 511:
                            this.BR_Address = "013F";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 512:
                        case 513:
                        case 514:
                        case 515:
                        case 516:
                        case 517:
                        case 518:
                        case 519:
                            this.BR_Address = "0140";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 520:
                        case 521:
                        case 522:
                        case 523:
                        case 524:
                        case 525:
                        case 526:
                        case 527:
                            this.BR_Address = "0141";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 528:
                        case 529:
                        case 530:
                        case 531:
                        case 532:
                        case 533:
                        case 534:
                        case 535:
                            this.BR_Address = "0142";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 536:
                        case 537:
                        case 538:
                        case 539:
                        case 540:
                        case 541:
                        case 542:
                        case 543:
                            this.BR_Address = "0143";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 544:
                        case 545:
                        case 546:
                        case 547:
                        case 548:
                        case 549:
                        case 550:
                        case 551:
                            this.BR_Address = "0144";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 552:
                        case 553:
                        case 554:
                        case 555:
                        case 556:
                        case 557:
                        case 558:
                        case 559:
                            this.BR_Address = "0145";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 560:
                        case 561:
                        case 562:
                        case 563:
                        case 564:
                        case 565:
                        case 566:
                        case 567:
                            this.BR_Address = "0146";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 568:
                        case 569:
                        case 570:
                        case 571:
                        case 572:
                        case 573:
                        case 574:
                        case 575:
                            this.BR_Address = "0147";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 576:
                        case 577:
                        case 578:
                        case 579:
                        case 580:
                        case 581:
                        case 582:
                        case 583:
                            this.BR_Address = "0148";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 584:
                        case 585:
                        case 586:
                        case 587:
                        case 588:
                        case 589:
                        case 590:
                        case 591:
                            this.BR_Address = "0149";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 592:
                        case 593:
                        case 594:
                        case 595:
                        case 596:
                        case 597:
                        case 598:
                        case 599:
                            this.BR_Address = "014A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 600:
                        case 601:
                        case 602:
                        case 603:
                        case 604:
                        case 605:
                        case 606:
                        case 607:
                            this.BR_Address = "014B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 608:
                        case 609:
                        case 610:
                        case 611:
                        case 612:
                        case 613:
                        case 614:
                        case 615:
                            this.BR_Address = "014C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 616:
                        case 617:
                        case 618:
                        case 619:
                        case 620:
                        case 621:
                        case 622:
                        case 623:
                            this.BR_Address = "014D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 624:
                        case 625:
                        case 626:
                        case 627:
                        case 628:
                        case 629:
                        case 630:
                        case 631:
                            this.BR_Address = "014E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 632:
                        case 633:
                        case 634:
                        case 635:
                        case 636:
                        case 637:
                        case 638:
                        case 639:
                            this.BR_Address = "014F";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 640:
                        case 641:
                        case 642:
                        case 643:
                        case 644:
                        case 645:
                        case 646:
                        case 647:
                            this.BR_Address = "0150";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 648:
                        case 649:
                        case 650:
                        case 651:
                        case 652:
                        case 653:
                        case 654:
                        case 655:
                            this.BR_Address = "0151";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 656:
                        case 657:
                        case 658:
                        case 659:
                        case 660:
                        case 661:
                        case 662:
                        case 663:
                            this.BR_Address = "0152";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 664:
                        case 665:
                        case 666:
                        case 667:
                        case 668:
                        case 669:
                        case 670:
                        case 671:
                            this.BR_Address = "0153";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 672:
                        case 673:
                        case 674:
                        case 675:
                        case 676:
                        case 677:
                        case 678:
                        case 679:
                            this.BR_Address = "0154";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 680:
                        case 681:
                        case 682:
                        case 683:
                        case 684:
                        case 685:
                        case 686:
                        case 687:
                            this.BR_Address = "0155";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 688:
                        case 689:
                        case 690:
                        case 691:
                        case 692:
                        case 693:
                        case 694:
                        case 695:
                            this.BR_Address = "0156";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 696:
                        case 697:
                        case 698:
                        case 699:
                        case 700:
                        case 701:
                        case 702:
                        case 703:
                            this.BR_Address = "0157";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 704:
                        case 705:
                        case 706:
                        case 707:
                        case 708:
                        case 709:
                        case 710:
                        case 711:
                            this.BR_Address = "0158";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 712:
                        case 713:
                        case 714:
                        case 715:
                        case 716:
                        case 717:
                        case 718:
                        case 719:
                            this.BR_Address = "0159";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 720:
                        case 721:
                        case 722:
                        case 723:
                        case 724:
                        case 725:
                        case 726:
                        case 727:
                            this.BR_Address = "015A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 728:
                        case 729:
                        case 730:
                        case 731:
                        case 732:
                        case 733:
                        case 734:
                        case 735:
                            this.BR_Address = "015B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 736:
                        case 737:
                        case 738:
                        case 739:
                        case 740:
                        case 741:
                        case 742:
                        case 743:
                            this.BR_Address = "015C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 744:
                        case 745:
                        case 746:
                        case 747:
                        case 748:
                        case 749:
                        case 750:
                        case 751:
                            this.BR_Address = "015D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 752:
                        case 753:
                        case 754:
                        case 755:
                        case 756:
                        case 757:
                        case 758:
                        case 759:
                            this.BR_Address = "015E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 760:
                        case 761:
                        case 762:
                        case 763:
                        case 764:
                        case 765:
                        case 766:
                        case 767:
                            this.BR_Address = "015F";

                            break; // TODO: might not be correct. Was : Exit Select


                        case 768:
                        case 769:
                        case 770:
                        case 771:
                        case 772:
                        case 773:
                        case 774:
                        case 775:
                            this.BR_Address = "0160";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 776:
                        case 777:
                        case 778:
                        case 779:
                        case 780:
                        case 781:
                        case 782:
                        case 783:
                            this.BR_Address = "0161";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 784:
                        case 785:
                        case 786:
                        case 787:
                        case 788:
                        case 789:
                        case 790:
                        case 791:
                            this.BR_Address = "0162";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 792:
                        case 793:
                        case 794:
                        case 795:
                        case 796:
                        case 797:
                        case 798:
                        case 799:
                            this.BR_Address = "0163";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 800:
                        case 801:
                        case 802:
                        case 803:
                        case 804:
                        case 805:
                        case 806:
                        case 807:
                            this.BR_Address = "0164";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 808:
                        case 809:
                        case 810:
                        case 811:
                        case 812:
                        case 813:
                        case 814:
                        case 815:
                            this.BR_Address = "0165";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 816:
                        case 817:
                        case 818:
                        case 819:
                        case 820:
                        case 821:
                        case 822:
                        case 823:
                            this.BR_Address = "0166";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 824:
                        case 825:
                        case 826:
                        case 827:
                        case 828:
                        case 829:
                        case 830:
                        case 831:
                            this.BR_Address = "0167";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 832:
                        case 833:
                        case 834:
                        case 835:
                        case 836:
                        case 837:
                        case 838:
                        case 839:
                            this.BR_Address = "0168";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 840:
                        case 841:
                        case 842:
                        case 843:
                        case 844:
                        case 845:
                        case 846:
                        case 847:
                            this.BR_Address = "0169";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 848:
                        case 849:
                        case 850:
                        case 851:
                        case 852:
                        case 853:
                        case 854:
                        case 855:
                            this.BR_Address = "016A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 856:
                        case 857:
                        case 858:
                        case 859:
                        case 860:
                        case 861:
                        case 862:
                        case 863:
                            this.BR_Address = "016B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 864:
                        case 865:
                        case 866:
                        case 867:
                        case 868:
                        case 869:
                        case 870:
                        case 871:
                            this.BR_Address = "016C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 872:
                        case 873:
                        case 874:
                        case 875:
                        case 876:
                        case 877:
                        case 878:
                        case 879:
                            this.BR_Address = "016D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 880:
                        case 881:
                        case 882:
                        case 883:
                        case 884:
                        case 885:
                        case 886:
                        case 887:
                            this.BR_Address = "016E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 888:
                        case 889:
                        case 890:
                        case 891:
                        case 892:
                        case 893:
                        case 894:
                        case 895:
                            this.BR_Address = "016F";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 896:
                        case 897:
                        case 898:
                        case 899:
                        case 900:
                        case 901:
                        case 902:
                        case 903:
                            this.BR_Address = "0170";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 904:
                        case 905:
                        case 906:
                        case 907:
                        case 908:
                        case 909:
                        case 910:
                        case 911:
                            this.BR_Address = "0171";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 912:
                        case 913:
                        case 914:
                        case 915:
                        case 916:
                        case 917:
                        case 918:
                        case 919:
                            this.BR_Address = "0172";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 920:
                        case 921:
                        case 922:
                        case 923:
                        case 924:
                        case 925:
                        case 926:
                        case 927:
                            this.BR_Address = "0173";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 928:
                        case 929:
                        case 930:
                        case 931:
                        case 932:
                        case 933:
                        case 934:
                        case 935:
                            this.BR_Address = "0174";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 936:
                        case 937:
                        case 938:
                        case 939:
                        case 940:
                        case 941:
                        case 942:
                        case 943:
                            this.BR_Address = "0175";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 944:
                        case 945:
                        case 946:
                        case 947:
                        case 948:
                        case 949:
                        case 950:
                        case 951:
                            this.BR_Address = "0176";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 952:
                        case 953:
                        case 954:
                        case 955:
                        case 956:
                        case 957:
                        case 958:
                        case 959:
                            this.BR_Address = "0177";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 960:
                        case 961:
                        case 962:
                        case 963:
                        case 964:
                        case 965:
                        case 966:
                        case 967:
                            this.BR_Address = "0178";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 968:
                        case 969:
                        case 970:
                        case 971:
                        case 972:
                        case 973:
                        case 974:
                        case 975:
                            this.BR_Address = "0179";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 976:
                        case 977:
                        case 978:
                        case 979:
                        case 980:
                        case 981:
                        case 982:
                        case 983:
                            this.BR_Address = "017A";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 984:
                        case 985:
                        case 986:
                        case 987:
                        case 988:
                        case 989:
                        case 990:
                        case 991:
                            this.BR_Address = "017B";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 992:
                        case 993:
                        case 994:
                        case 995:
                        case 996:
                        case 997:
                        case 998:
                        case 999:
                            this.BR_Address = "017C";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 1000:
                        case 1001:
                        case 1002:
                        case 1003:
                        case 1004:
                        case 1005:
                        case 1006:
                        case 1007:
                            this.BR_Address = "017D";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 1008:
                        case 1009:
                        case 1010:
                        case 1011:
                        case 1012:
                        case 1013:
                        case 1014:
                        case 1015:
                            this.BR_Address = "017E";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 1016:
                        case 1017:
                        case 1018:
                        case 1019:
                        case 1020:
                        case 1021:
                        case 1022:
                        case 1023:
                            this.BR_Address = "017F";

                            break; // TODO: might not be correct. Was : Exit Select

                    }

                    break; // TODO: might not be correct. Was : Exit Select

                case 3:
                    //"S"
                    this.BR_Address = "0000";
                    break; // TODO: might not be correct. Was : Exit Select

                case 4:
                    //"T"
                    this.BR_Address = "00C0";
                    break;
                case 5:
                    //"C"

                    switch (BitAd)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            this.BR_Address = "01C0";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                            this.BR_Address = "01C1";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 16:
                        case 17:
                        case 18:
                        case 19:
                        case 20:
                        case 21:
                        case 22:
                        case 23:
                            this.BR_Address = "01C2";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 24:
                        case 25:
                        case 26:
                        case 27:
                        case 28:
                        case 29:
                        case 30:
                        case 31:
                            this.BR_Address = "01C3";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 32:
                        case 33:
                        case 34:
                        case 35:
                        case 36:
                        case 37:
                        case 38:
                        case 39:
                            this.BR_Address = "01C4";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 40:
                        case 41:
                        case 42:
                        case 43:
                        case 44:
                        case 45:
                        case 46:
                        case 47:
                            this.BR_Address = "01C5";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 48:
                        case 49:
                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                            this.BR_Address = "01C6";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 56:
                        case 57:
                        case 58:
                        case 59:
                        case 60:
                        case 61:
                        case 62:
                        case 63:
                            this.BR_Address = "01C7";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 64:
                        case 65:
                        case 66:
                        case 67:
                        case 68:
                        case 69:
                        case 70:
                        case 71:
                            this.BR_Address = "01C8";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 72:
                        case 73:
                        case 74:
                        case 75:
                        case 76:
                        case 77:
                        case 78:
                        case 79:
                            this.BR_Address = "01C9";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 80:
                        case 81:
                        case 82:
                        case 83:
                        case 84:
                        case 85:
                        case 86:
                        case 87:
                            this.BR_Address = "01CA";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 88:
                        case 89:
                        case 90:
                        case 91:
                        case 92:
                        case 93:
                        case 94:
                        case 95:
                            this.BR_Address = "01CB";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 96:
                        case 97:
                        case 98:
                        case 99:
                        case 100:
                        case 101:
                        case 102:
                        case 103:
                            this.BR_Address = "01CC";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 104:
                        case 105:
                        case 106:
                        case 107:
                        case 108:
                        case 109:
                        case 110:
                        case 111:
                            this.BR_Address = "01CD";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 112:
                        case 113:
                        case 114:
                        case 115:
                        case 116:
                        case 117:
                        case 118:
                        case 119:
                            this.BR_Address = "01CE";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 120:
                        case 121:
                        case 122:
                        case 123:
                        case 124:
                        case 125:
                        case 126:
                        case 127:
                            this.BR_Address = "01CF";

                            break; // TODO: might not be correct. Was : Exit Select

                        case 128:
                        case 129:
                        case 130:
                        case 131:
                        case 132:
                        case 133:
                        case 134:
                        case 135:
                            this.BR_Address = "01D0";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 136:
                        case 137:
                        case 138:
                        case 139:
                        case 140:
                        case 141:
                        case 142:
                        case 143:
                            this.BR_Address = "01D1";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 144:
                        case 145:
                        case 146:
                        case 147:
                        case 148:
                        case 149:
                        case 150:
                        case 151:
                            this.BR_Address = "01D2";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 152:
                        case 153:
                        case 154:
                        case 155:
                        case 156:
                        case 157:
                        case 158:
                        case 159:
                            this.BR_Address = "01D3";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 160:
                        case 161:
                        case 162:
                        case 163:
                        case 164:
                        case 165:
                        case 166:
                        case 167:
                            this.BR_Address = "01D4";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 168:
                        case 169:
                        case 170:
                        case 171:
                        case 172:
                        case 173:
                        case 174:
                        case 175:
                            this.BR_Address = "01D5";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 176:
                        case 177:
                        case 178:
                        case 179:
                        case 180:
                        case 181:
                        case 182:
                        case 183:
                            this.BR_Address = "01D6";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 184:
                        case 185:
                        case 186:
                        case 187:
                        case 188:
                        case 189:
                        case 190:
                        case 191:
                            this.BR_Address = "01D7";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 192:
                        case 193:
                        case 194:
                        case 195:
                        case 196:
                        case 197:
                        case 198:
                        case 199:
                            this.BR_Address = "01D8";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 200:
                        case 201:
                        case 202:
                        case 203:
                        case 204:
                        case 205:
                        case 206:
                        case 207:
                            this.BR_Address = "01D9";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 208:
                        case 209:
                        case 210:
                        case 211:
                        case 212:
                        case 213:
                        case 214:
                        case 215:
                            this.BR_Address = "01DA";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 216:
                        case 217:
                        case 218:
                        case 219:
                        case 220:
                        case 221:
                        case 222:
                        case 223:
                            this.BR_Address = "01DB";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 224:
                        case 225:
                        case 226:
                        case 227:
                        case 228:
                        case 229:
                        case 230:
                        case 231:
                            this.BR_Address = "01DC";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 232:
                        case 233:
                        case 234:
                        case 235:
                        case 236:
                        case 237:
                        case 238:
                        case 239:
                            this.BR_Address = "01DD";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 240:
                        case 241:
                        case 242:
                        case 243:
                        case 244:
                        case 245:
                        case 246:
                        case 247:
                            this.BR_Address = "01DE";
                            break; // TODO: might not be correct. Was : Exit Select

                        case 248:
                        case 249:
                        case 250:
                        case 251:
                        case 252:
                        case 253:
                        case 254:
                        case 255:
                            this.BR_Address = "01DF";

                            break; // TODO: might not be correct. Was : Exit Select

                    }
                    break; // TODO: might not be correct. Was : Exit Select

                case 6:
                    //""
                    return "";
            }
            return BR_Address;
        }


        /// <summary>
        /// CCAdd-转换C元件16位地址和32位地址是不同的，参考协议
        /// </summary>
        /// <param name="Ad"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string CCAdd(int Ad)
        {
            if (Ad < 200)
            {
                CR_Address = DecimalToHex(((Ad * 2) + 2560));
                //2560即H0A00，单字节运算
            }
            else {
                CR_Address = DecimalToHex((((Ad - 200) * 4) + 3072));
                //3072即H0C00，运算双字节
            }
            return CR_Address;
        }
        public string CDAdd(int Ad)
        {
            //D0 ,>>4 0 0 0
            //            if (Ad < 8000)
            //            {
            //                DR_Address = ValueToHex(Ad * 2 + 0x4000);
            //            }
            //            else {
            //                Ad = Ad - 8000;
            //                DR_Address = ValueToHex(Ad*2 + 0xe00);
            //            }
            int Dr_2 = Ad / 128;
            int Dr_3 = (Ad % 128) / 8;
            int Dr_4 = ((Ad % 128) % 8) * 2;

            DR_Address = "1" + DecimalToHex(Dr_2) + DecimalToHex(Dr_3) + DecimalToHex(Dr_4);

            return DR_Address;
        }


        private void GetResponse(ref byte[] response)
        {
            for (int i = 0; i <= response.Length - 1; i++)
            {
                response[i] = (byte)sp.ReadByte();
                //Debug.Print(response(i))
                //从输入缓冲区读入数据直到指定的字符6
                if (response[0] == (byte)6)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// CheckResponse--校验响应
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool CheckResponse(byte[] response)
        {
            //Perform a basic CheckResponse check:
            byte[] MesCheckSum = new byte[2];
            int CheckSum;
            string buf;

            //从输入缓冲区读入数据直到指定的字符6
            if (response[0] == (byte)6)
            {
                return true;
            }

            CheckSum = 0;
            for (int i = 1; i <= response.Length - 3; i++)
            {
                CheckSum = (CheckSum + response[i]) & 0xff;
            }
            //buf = String.Format("{0:X2}", CheckSum)

            buf = DecimalToHex(CheckSum).Length == 1 ? "0" + DecimalToHex(CheckSum) : DecimalToHex(CheckSum);



            MesCheckSum = System.Text.Encoding.ASCII.GetBytes(buf.Substring(0, 2));
            //加校验和
            if (MesCheckSum[0] == response[response.Length - 2] && MesCheckSum[1] == response[response.Length - 1])
            {
                return true;
            }
            else {
                return false;
            }
        }
        //
        //        private string DecToBinary(short Ascii)
        //        {
        //            //(ascii码表示法)转二进制
        //
        //            int Dec = DecimalToHex(Chr(Ascii));
        //            string dectobin = "";
        //            while (Dec > 0)
        //            {
        //                dectobin = (Dec % 2).ToString + dectobin;
        //                Dec = Dec / 2;
        //            }
        //
        //            int i = Len(dectobin);
        //            if (i < 4)
        //            {
        //                dectobin = new string("0", 4 - i) + dectobin;
        //            }
        //            return dectobin;
        //
        //        }

        /// <summary>
        /// ProceStrLen--处理字符串长度
        /// </summary>
        /// <param name="ProceStr"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string ProceStrLen(string ProceStr)
        {
            switch (ProceStr.Length)
            {
                case 0:
                    return "0000";
                case 1:
                    return ("000" + ProceStr);
                case 2:
                    return ("00" + ProceStr);
                case 3:
                    return ("0" + ProceStr);
                case 4:
                    return ProceStr;
                case 5:
                    return ProceStr.Substring(1, 4);
            }
            return "FFFF";
        }



        public int HexToDecimal(string str)
        {
            int hexToInt = 0;
            switch (str)
            {
                case "0":
                    hexToInt = 0;
                    break;
                case "1":
                    hexToInt = 1;
                    break;
                case "2":
                    hexToInt = 2;
                    break;
                case "3":
                    hexToInt = 3;
                    break;
                case "4":
                    hexToInt = 4;
                    break;
                case "5":
                    hexToInt = 5;
                    break;
                case "6":
                    hexToInt = 6;
                    break;
                case "7":
                    hexToInt = 7;
                    break;
                case "8":
                    hexToInt = 8;
                    break;
                case "9":
                    hexToInt = 9;
                    break;
                case "A":
                    hexToInt = 10;
                    break;
                case "B":
                    hexToInt = 11;
                    break;
                case "C":
                    hexToInt = 12;
                    break;
                case "D":
                    hexToInt = 13;
                    break;
                case "E":
                    hexToInt = 14;
                    break;
                case "F":
                    hexToInt = 15;
                    break;
            }
            return hexToInt;
        }

        public string DecimalToHex(int Decimal)
        {
            String intToHex = "";
            switch (Decimal)
            {
                case 0:
                    intToHex = "0";
                    break;
                case 1:
                    intToHex = "1";
                    break;
                case 2:
                    intToHex = "2";
                    break;
                case 3:
                    intToHex = "3";
                    break;
                case 4:
                    intToHex = "4";
                    break;
                case 5:
                    intToHex = "5";
                    break;
                case 6:
                    intToHex = "6";
                    break;
                case 7:
                    intToHex = "7";
                    break;
                case 8:
                    intToHex = "8";
                    break;
                case 9:
                    intToHex = "9";
                    break;
                case 10:
                    intToHex = "A";
                    break;
                case 11:
                    intToHex = "B";
                    break;
                case 12:
                    intToHex = "C";
                    break;
                case 13:
                    intToHex = "D";
                    break;
                case 14:
                    intToHex = "E";
                    break;
                case 15:
                    intToHex = "F";
                    break;
            }
            return intToHex;
        }


        public int HexToValue(string str)
        {
            string One, Two, Three, Four;
            One = str.Substring(0, 1);
            Two = str.Substring(1, 1);
            Three = str.Substring(2, 1);
            Four = str.Substring(3, 1);
            return HexToDecimal(One) * 4096 + HexToDecimal(Two) * 256 + HexToDecimal(Three) * 16 + HexToDecimal(Four);
        }


        public string ValueToHex(int val)
        {
            int One, Two, Three, Four, leaving;
            int Val = 0;
            Val = val;
            leaving = Val % 4096;
            if (Val > 4096)
            {
                Val = Val - leaving;
                One = Val / 4096;
            }
            else
            {
                One = 0;
            }

            Val = leaving;
            leaving = Val % 256;
            if (Val > 256)
            {
                Val = Val - leaving;
                Two = Val / 256;
            }
            else
            {
                Two = 0;
            }
            Val = leaving;
            leaving = Val % 16;
            if (Val > 16)
                Val = Val - leaving;
            Three = Val / 16;
            Four = leaving;
            return DecimalToHex(Three) + DecimalToHex(Four) + DecimalToHex(One) + DecimalToHex(Two); //高低位反转
        }


        // 是否为双字节字符。         
        private static bool IsTwoBytesChar(string chr)
        {
            // 使用中文支持编码  
            Encoding ecode = Encoding.GetEncoding("gb18030");
            if (ecode.GetByteCount(chr) == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //C#实现VB中的asc和chr函数，字符（含中文）转ASCII      
        // 传入单个字符，得到字符的ASCII码   
        public static int Asc(string chr)
        {
            Encoding ecode = Encoding.GetEncoding("GB18030");
            Byte[] codeBytes = ecode.GetBytes(chr);
            if (IsTwoBytesChar(chr))
            {
                // 双字节码为高位乘256，再加低位
                // 该为无符号码，再减65536
                return (int)codeBytes[0] * 256 + (int)codeBytes[1] - 65536;
            }
            else
            {
                return (int)codeBytes[0];
            }
        }

        // 传入单个字符的ASCII码，得到ASCII码对应的字符（含双字节）
        public static string Chr(int asc)
        {
            //asc = asc + 65536;
            Encoding asciiEncoding = Encoding.GetEncoding("GB18030");
            Byte[] chrByte = BitConverter.GetBytes((short)asc);
            string strCharacter = string.Empty;
            if (asc < 0 || asc > 255)
            {
                Byte[] chrByteStr = new byte[2];
                chrByteStr[0] = chrByte[1];
                chrByteStr[1] = chrByte[0];
                strCharacter = asciiEncoding.GetString(chrByteStr);
            }
            else
            {
                Byte[] chrByteStr = new byte[1];
                chrByteStr[0] = chrByte[0];
                strCharacter = asciiEncoding.GetString(chrByteStr);
            }
            return (strCharacter);
        }


        public string SunCheck(string inputstr)
        {
            int slen, i, j, result;
            string tempfcs, strRet;
            strRet = "";
            result = 0;
            slen = inputstr.Length;
            for (i = 0; i < slen; i++)
            {
                j = Asc(inputstr.Substring(i, 1).ToUpper());
                result = result + j;
            }
            result = result + 3;
            tempfcs = Convert.ToString(result, 16).ToUpper();//转换成16进制              
            if (tempfcs.Length == 1)
                strRet = "0" + tempfcs;
            else if (tempfcs.Length == 2)
                strRet = tempfcs;
            else if (tempfcs.Length > 2)
                strRet = tempfcs.Substring(tempfcs.Length - 2, 2);
            return strRet;
        }


    }
}
