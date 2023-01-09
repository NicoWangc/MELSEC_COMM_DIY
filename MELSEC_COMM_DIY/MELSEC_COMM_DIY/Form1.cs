using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Timers;
using System.Diagnostics;
using MitsubishiFxPlc;

namespace MELSEC_COMM_DIY
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();
            SerialCommunications.OpenDevice();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(TESTread);
            th.IsBackground = true;
            th.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(TESTwrite);
            th.IsBackground = true;
            th.Start();
            
        }

        public void TESTread()
        {
            int i = 0;//总循环次数
            int j = 0;//错误次数
            while (true)
            {
                i++;
                Thread.Sleep(30);
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    string res = SerialCommunications.ReadDevice("D255");
                    sw.Stop();
                    res = CommunicationsFXSerial.HexStr2Dec(res);
                    label4.Text = i.ToString()+":"+res + " ";
                    label4.Text += sw.ElapsedMilliseconds.ToString() + "ms";
                    sw.Reset();
                }
                catch (Exception ex)
                {
                    j++;
                    label8.Text = "错误次数:"+j.ToString();
                    label4.Text = "读取失败！报错内容：" + ex.ToString();
                }
            }
        }

        public void TESTwrite()
        {
            int i = 0;//总循环次数
            int j = 0;//错误次数
            while (true)
            {
                i++;
                Thread.Sleep(30);
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    SerialCommunications.WriteDevice("D255", 1);
                    sw.Stop();
                    label5.Text = i.ToString()+":"+sw.ElapsedMilliseconds.ToString() + "ms";
                    sw.Reset();
                }
                catch (Exception ex)
                {
                    j++;
                    label9.Text = "错误次数:" + j.ToString();
                    label5.Text = "读取失败！报错内容：" + ex.ToString();
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Cls_Fx2N fx = new Cls_Fx2N();
            fx.OpenPort();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int[] res = fx.Dvalue16(255, 1);
            sw.Stop();
            label4.Text = sw.ElapsedMilliseconds.ToString() + "ms";
            sw.Reset();
            label5.Text = "写入成功！";
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                string res = SerialCommunications.ReadDevice("D255");
                sw.Stop();
                res = CommunicationsFXSerial.HexStr2Dec(res);
                label4.Text =res + " ";
                label4.Text += sw.ElapsedMilliseconds.ToString() + "ms";
                sw.Reset();
            }
            catch (Exception ex)
            {
                label4.Text = "读取失败！报错内容：" + ex.ToString();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                SerialCommunications.WriteDevice("D255", 1);
                sw.Stop();
                label5.Text =sw.ElapsedMilliseconds.ToString() + "ms";
                sw.Reset();
            }
            catch (Exception ex)
            {
                label5.Text = "读取失败！报错内容：" + ex.ToString();
            }
        }
    }
}
