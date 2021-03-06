﻿using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using ClassLibrary1;

namespace GropuWork01
{
    public partial class frmGetData : Form
    {
        #region 重载 
        public frmGetData()
        {
            
            InitializeComponent();
        }
        #endregion
        #region  load
        private void frmGetData_Load(object sender, EventArgs e)
        {
            try
            {
                #region  serialPort1设置
                //设置通信端口
                serialPort1.PortName = ConfigurationManager.AppSettings["Port"];
                //设置串行波特率。
                serialPort1.BaudRate = int.Parse(ConfigurationManager.AppSettings["BaudRate"]);
                //设置每个字节的标准数据位长度。
                serialPort1.DataBits = int.Parse(ConfigurationManager.AppSettings["DataBits"]);
                //设置每个字节的标准停止位数。
                serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "1");
                //设置奇偶校验检查协议
                serialPort1.Parity = System.IO.Ports.Parity.None;
                //设置读取操作未完成时发生超时之前的毫秒数。
                serialPort1.ReadTimeout = -1;
                #endregion
                //Console.WriteLine("111111111111");
                serialPort1.Open();//打开串口
                //Console.WriteLine("111111111111");
                this.listBox1.Items.Clear();
                this.label4.Text = "数据格式:共12字节\r\n" + "开始(2A)+空格(20,1字节)+湿度(4字节,51.9)+空格(20,1字节）+\r\n\r\n" +
                    "温度(4字节,19.6)+结束(0A)\r\n\r\n" +
                    "参数设置:波特率9600，数据检验无,数据位8，停止位1\r\n\r\n" +
                    "其他:每两秒采集一次数据";
                this.StartPosition = FormStartPosition.CenterScreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        #endregion
        #region close
        private void button1_Click_1(object sender, EventArgs e)
        {
            this.Dispose();
        }
        #endregion

        #region 读取串口数据
        private void serialPort1_DataReceived_1(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(100);
                //获取接收缓冲区中数据的字节数。
                int bytes = serialPort1.BytesToRead;
                byte[] buffer = new byte[bytes];
                if (bytes == 0) return;
                //0 要写入字符的 buffer 中的偏移量.   bytes要读取的最大字符数。 如果 count 大于输入缓冲区中的字符数，则读取较少的字符。
                serialPort1.Read(buffer, 0, bytes);
                //Console.WriteLine(buffer);
                //读入十进制内容 42 32 52 49 46 57 32 49 52 46 54 10
                //读了很长时间才看出来 前面是湿度 后面才是温度
                string str = ByteArrayToHeXString(buffer);
                log(str);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static string ByteArrayToHeXString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 2);
            // 先把byte转成字符串 再用0填充字符串的左边 用空格填充字符串右边
            // 保证每两字节插入一空格
            // 把8位无符号整型数转成以16进制的string形式再接着从右向左占满两位，占不满用0补齐，再从左向右占满三位，占不满用空格补齐。
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            return sb.ToString().ToUpper();
        }
        void log(string value)
        {
            try
            {
                value = value.Replace(" ", "");
                #region 多线程下显示数据
                // 在拥有此控件的基础窗口句柄的线程上执行指定的委托。
                this.txt原始数据.Invoke(new EventHandler(delegate
                {
                    this.txt原始数据.Text = value;
                }));
                this.txt温度.Invoke(new EventHandler(delegate
                {
                    // 14 15...22
                    this.txt温度.Text = Class1.UnHex(value.Substring(14, 8), "ASCII");
                }));
                this.txt湿度.Invoke(new EventHandler(delegate
                {
                    this.txt湿度.Text = Class1.UnHex(value.Substring(4, 8), "UTF-8");
                }));
                this.listBox1.Invoke(new EventHandler(delegate {
                    if (!string.IsNullOrEmpty(this.txt温度.Text) &&
                        !string.IsNullOrEmpty(this.txt湿度.Text))
                    {
                        this.listBox1.Items.Insert(0, "温度=" + Class1.UnHex(value.Substring(14, 8), "ASCII")
                            + ",湿度=" + Class1.UnHex(value.Substring(4, 8), "UTF-8"));
                    }
                }));
                #endregion

                #region 保存数据
                if (this.checkBox1.Checked)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Class1.UnHex(value.Substring(14, 8), "ASCII")) &&
                            !string.IsNullOrEmpty(Class1.UnHex(value.Substring(4, 8), "ASCII")))
                        {
                            /*StreamWriter sw = new StreamWriter(Application.StartupPath + "\\data.txt",
                                true, Encoding.ASCII);
                            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                                Class1.UnHex(value.Substring(14, 8), "ASCII") + "," +
                                Class1.UnHex(value.Substring(4, 8), "UTF-8"));
                            //sw.WriteLine(value);
                            sw.Flush();
                            sw.Close();
                            sw.Dispose();
                            MessageBox.Show(value); //程序能走到这，齐了怪了，为什么保存不了数据呢?*/
                            // 无奈 换了一种方法 这种方法会报 "无法写入已关闭的textWriter"
                            /*FileInfo file = new FileInfo(System.AppDomain.CurrentDomain.BaseDirectory + "data.txt"); 
                            StreamWriter sw = null;
                            if (!file.Exists)
                            {
                                sw = file.CreateText();
                                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                                Class1.UnHex(value.Substring(14, 8), "ASCII") + "," +
                                Class1.UnHex(value.Substring(4, 8), "UTF-8"));
                            }
                            else
                            {
                                sw = file.AppendText();
                                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                                Class1.UnHex(value.Substring(14, 8), "ASCII") + "," +
                                Class1.UnHex(value.Substring(4, 8), "UTF-8"));
                            }
                            sw.Close();
                            sw.Flush();
                            sw.Dispose();
                        */
                            try
                            {
                                //FileStream aFile = new FileStream("d://data.txt", FileMode.OpenOrCreate);
                                StreamWriter sw = File.AppendText("d://data.txt");
                                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                                Class1.UnHex(value.Substring(14, 8), "ASCII") + "," +
                                Class1.UnHex(value.Substring(4, 8), "UTF-8"));
                                //sw.WriteLine(value);
                                sw.Flush();
                                sw.Close();
                                sw.Dispose();
                            }
                            catch (IOException ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.ReadLine();
                            }
                            //MessageBox.Show(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        }
}