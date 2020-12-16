using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ADS_Web后台弱口令扫描
{
    public partial class Proxy : Form
    {
        
        public Proxy()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string server = textBox_server.Text;
                string port = textBox_port.Text;
                //测试连接
                var url = textBox1.Text;
                var proxy = new WebProxy(server + ":" + port, true);
                //创建要请求的对象
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Proxy = proxy;
                var res = (HttpWebResponse)req.GetResponse();

                //超时时间 5秒后无响应就放弃
                req.Timeout = 5000;
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                //读取返回的内容
                var html = sr.ReadToEnd();
                if (html != null)
                {
                    MessageBox.Show("连接成功");
                }
                else
                {
                    MessageBox.Show("连接失败");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("连接超时");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //保存代理内容
            try
            {
                string server = textBox_server.Text;
                string port = textBox_port.Text;
                //测试连接
                var url = textBox1.Text;
                var proxy = new WebProxy(server + ":" + port, true);
                //创建要请求的对象
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Proxy = proxy;
                var res = (HttpWebResponse)req.GetResponse();

                //超时时间 5秒后无响应就放弃
                req.Timeout = 5000;
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                //读取返回的内容
                var html = sr.ReadToEnd();
                if (html != null)
                {
                    Form1.Proxy_server = textBox_server.Text;
                    Form1.Proxy_port = textBox_port.Text;
                    MessageBox.Show("代理设置已保存");
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("连接失败");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("连接超时");
            }
        }

        private void Proxy_Load(object sender, EventArgs e)
        {
            textBox_server.Text = Form1.Proxy_server;
            textBox_port.Text = Form1.Proxy_port;
        }
    }
}
