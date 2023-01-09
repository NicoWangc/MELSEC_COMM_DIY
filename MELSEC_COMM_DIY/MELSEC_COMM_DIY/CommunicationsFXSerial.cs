using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MELSEC_COMM_DIY
{
    public static class CommunicationsFXSerial
    {
        public static int STX = 0x02;//Start of Text,固定的十六进制ASCII码
        //参考https://blog.csdn.net/angiusc/article/details/106041146
        public static int CMD_Read = 0x30;//读操作的命令码0
        public static int CMD_Write = 0x31;//写操作的命令码1
        public static int NUM1 = 0x30 + 0x31; //"01"
        public static int NUM2 = 0x30 + 0x32; //"02"
        public static int NUM4 = 0x30 + 0x34; //"04"

        public static string NUM1Str = "3031";
        public static string NUM2Str = "3032";
        public static string NUM4Str = "3034";
        //D区单个地址占用两字节，如果读D区四个连续地址，则为08
        //写数据时必须为偶数，如写数据到Y区Y0~Y7，NUM=2,读数据可为奇数
        //写操作时该值必须以4个字符为一组，且低位2个在前，高位2个在后。
        //比如：要写十进制10到D0中，10的十六制表示为A，要4个字符表示所以前面补0为000A；
        //又要求低位在前高位在后，则表示为0A00
        public static int EXT = 0x03;//End of Text

        public static string SendData(int CMD, string Addr, int Num, int Ext)
        {//读地址的发送方法
            string num = "";
            if (Num == 1)
            {
                num = NUM1Str;
            }
            else if (Num == 2)
            {
                num = NUM2Str;
            }
            else
            {
                num = NUM4Str;

            }
            string res = STX.ToString("X2") + CMD.ToString("X") + Str2Hex(Addr) + num + Ext.ToString("X2")
                + Sum(CMD, Addr, NUM2, EXT);
            return res;
        }
        public static string SendData(int CMD, string Addr, int Num, int Data, int Ext)
        {//写单个数据的发送方法
            string num = "";
            if (Num == 1)
            {
                num = NUM1Str;
            }
            else if (Num == 2)
            {
                num = NUM2Str;
            }
            else
            {
                num = NUM4Str;
            }
            string res = STX.ToString("X2") + CMD.ToString("X") + Str2Hex(Addr) + num +
               Str2Hex(DataTransform(Data)) + Ext.ToString("X2") + Sum(CMD, Addr, NUM2, Data, EXT);
            return res;
        }
        public static string SendData(int CMD, string Addr, int Num, int Data1, int Data2, int Ext)
        {//写两个地址和数据的方法
            string num = "";
            if (Num == 1)
            {
                num = NUM1Str;
            }
            else if (Num == 2)
            {
                num = NUM2Str;
            }
            else
            {
                num = NUM4Str;
            }
            string res = STX.ToString("X2") + CMD.ToString("X") + Str2Hex(Addr) + num +
                Str2Hex(DataTransform(Data1)) + Str2Hex(DataTransform(Data2)) + Ext.ToString("X2") + Sum(CMD, Addr, NUM4, Data1, Data2, EXT);
            return res;
        }

        public static string ByteArray2Hex(byte[] bytes)
        {//字节数组(十进制)转十六进制字符串
            string StrResult = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                StrResult += (bytes[i].ToString("X2"));
            }
            return StrResult;
        }

        public static byte[] HexStr2ByteArray(string str)
        {//十六进制字符串转字节数组（十进制）
            byte[] byteResult = new byte[str.Length/2];
            for (int i = 0; i < str.Length/2; i++)
            {
                byteResult[i] = (byte)Convert.ToInt32(str.Substring(i*2, 2), 16);
            }
            return byteResult;
        }

        public static string ValueWithoutZero(string num)
        {//去除字符前0
            string res = "";
            bool flag = false;
            int sum = 0;
            for (int i = 0; i < num.Length; i++)
            {
                sum += Convert.ToInt16((num[i].ToString()));
                if (!flag&&num.Substring(i, 1) == "0")
                {

                }
                else
                {
                    res += num[i].ToString();
                    flag = true;
                }
            }
            if (sum==0)
            {
                return "0";
            }
            return res;
        }

        public static string HexStr2Dec(string str)
        {//字符串转十六进制
            
            return Convert.ToInt32(str,16).ToString();
        }

        public static string Str2Hex(string str)
        {//字符串转十六进制
            string StrResult = "";
            for (int i = 0; i < str.Length; i++)
            {
                StrResult += Asc(str[i].ToString()).ToString("X");
            }
            return StrResult;
        }

        
        public static int Asc(string character)
        {//单一字符转十进制ASCII
            if (character.Length == 1)
            {
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                int intAsciiCode = (int)asciiEncoding.GetBytes(character)[0];
                return (intAsciiCode);
            }
            else
            {
                throw new Exception("Character is not valid.");
            }

        }

        public static string DataTransform(int data)
        {//十进制数转四位十六进制并交换高低字节，不足四位补0
            string HexCode = data.ToString("X4");
            string Result = "";
            int length = HexCode.Length;
            if (length != 4)
            {
                for (int i = 0; i < 4 - length; i++)
                {
                    Result = "0" + Result;
                }
            }
            else
            {
                Result = HexCode;
            }
            Result = Result.Substring(2, 2) + Result.Substring(0, 2);
            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string Sum(int CMD, string Addr, int Num, int Ext)
        {
            int sum = CMD + Num + Ext;
            string HexCode = Str2Hex(Addr);
            for (int i = 0; i < HexCode.Length / 2; i++)
            {
                sum += Convert.ToInt16(HexCode.Substring(i * 2, 2), 16);
                //十六进制数运算后以十进制呈现
            }
            string StringSum = sum.ToString("X");
            string Right2Char = StringSum.Substring(StringSum.Length - 2, 2);
            return Str2Hex(Right2Char);
        }

        public static string Sum(int CMD, string Addr, int Num, int Data, int Ext)
        {
            int sum = CMD + Num + Ext;
            string HexCode = Str2Hex(Addr);
            for (int i = 0; i < HexCode.Length / 2; i++)
            {
                sum += Convert.ToInt16(HexCode.Substring(i * 2, 2), 16);
            }
            string dataTrans = DataTransform(Data);
            for (int i = 0; i < 4; i++)
            {
                sum += Convert.ToInt32(Asc(dataTrans[i].ToString()));
            }

            string StringSum = sum.ToString("X");
            string Right2Char = StringSum.Substring(StringSum.Length - 2, 2);
            return Str2Hex(Right2Char);
        }

        public static string Sum(int CMD, string Addr, int Num, int Data1, int Data2, int Ext)
        {
            int sum = CMD + Num + Ext;
            string HexCode = Str2Hex(Addr);
            for (int i = 0; i < HexCode.Length / 2; i++)
            {
                sum += Convert.ToInt16(HexCode.Substring(i * 2, 2), 16);
            }
            string dataTrans1 = DataTransform(Data1);
            string dataTrans2 = DataTransform(Data2);
            for (int i = 0; i < 4; i++)
            {
                int tem1 = Convert.ToInt32(Asc(dataTrans1[i].ToString()));
                int tem2 = Convert.ToInt32(Asc(dataTrans2[i].ToString()));
                sum += (tem1 + tem2);
            }

            string StringSum = sum.ToString("X");
            string Right2Char = StringSum.Substring(StringSum.Length - 2, 2);
            return Str2Hex(Right2Char);
        }

        public static string TransAddrD(string str)
        {
            int num = Convert.ToInt32(str.Substring(1, str.Length - 1));
            string AddrResult = (num * 2 + 4096).ToString("X4");//1000Hex=4096Dec
            return AddrResult;
        }
    }
}
