using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MELSEC_COMM_DIY
{
    class AddrAssembly
    {
        public static Dictionary<string, int> dict = new Dictionary<string, int>
        {
            #region T区典型地址
            {"T0",0x00C0},//T0~T7共用同个位地址，下同
            {"T40",0x00C5},
            {"T80",0x00CA},
            {"T120",0x00CF},
            #endregion

            #region M区典型地址
            {"M0",0x0100},//M0~M7共用同个位地址，下同
            {"M8",0x0101},
            {"M40",0x0105},
            {"M80",0x010A},
            {"M120",0x010F},
            #endregion

            #region D区典型地址
            {"D0",0x1000},
            {"D1",0x1002},
            {"D2",0x1004},
            {"D3",0x1006},
            {"D4",0x1008},
            {"D5",0x100A},
            {"D11",0x1002},
            {"D127",0x1002},
            {"D128",0x1002},
            {"D255",0x1008},
            {"D256",0x11FE},
            {"D999",0x17CE},
            {"D1000",0x17D0},
            {"D2000",0x0FA0},
            #endregion

            #region X区典型地址
            {"X00",0x0080},//X00~X07共用同个位地址，下同
            {"X10",0x0081},
            {"X70",0x0087},
            {"X100",0x0088},//X100~X104
            {"X110",0x0089},//X110~X117
            {"X120",0x008A},//X120~X127
            {"X170",0x008F},//X170~X177
            #endregion

            #region Y区典型地址
            {"Y00",0x00A0},//Y00~Y07共用同个位地址，下同
            {"Y70",0x00A7},
            {"Y100",0x00A8},//Y100~Y104
            {"Y170",0x00AF},//Y170~Y177
            #endregion

            #region S区典型地址
            {"S0",0x0000},//S0~S7共用同个位地址，下同
            {"S8",0x0001},
            {"S40",0x0005},
            {"S80",0x000A},
            {"S120",0x000F},
            #endregion

            #region C区典型地址
            {"C0",0x01C0},//C0~C7共用同个位地址，下同
            {"C8",0x01C1},
            {"C40",0x01C5},
            {"C80",0x01CA},
            {"C120",0x01CF}
            #endregion
        };
    }
}
