using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ADS_Web后台弱口令扫描
{
    public partial class gen_dict : Form
    {
        public static string symblo = "!@#$%&*_.";
        List<string> StrList = new List<string>() { "123","111","000","666","678","520","521","321","abc","qwe","zxc","asd","1234","1111","0000","6666","5.21","5.28","123456"};
        public gen_dict()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                listBox1.Items.Clear();
                //逐行读取信息项
                foreach(string A in textBox1.Lines)
                {
                    //逐行读取间隔符号
                    foreach(char B_info in symblo.ToList())
                    {
                        //逐行读取弱字符
                        foreach (string C in StrList)
                        {
                            string B = B_info.ToString();
                            listBox1.Items.Add(A + B);
                            listBox1.Items.Add(A + C);
                            listBox1.Items.Add(A + B + C);
                            listBox1.Items.Add(A + C + B);
                            listBox1.Items.Add(B + A);
                            listBox1.Items.Add(B + A + C);
                        }
                    }
                    
                    
                }
                MessageBox.Show("共生成 "+ listBox1.Items.Count+" 个字典");
            }
            catch (Exception)
            {
                MessageBox.Show("未知错误");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                //结果另存为
                SaveFileDialog op = new SaveFileDialog();
                op.Title = "字典结果另存为";
                op.FileName = "GoodPass.txt";
                if (op.ShowDialog() == DialogResult.OK)
                {
                    //FileInfo fi = new FileInfo(op.FileName);
                    FileStream fs = new FileStream(op.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    fs.Close();
                    StreamWriter sw = new StreamWriter(fs.Name);
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        sw.WriteLine(listBox1.Items[i]);
                    }
                    sw.Close();
                    MessageBox.Show("结果已导出");

                }
            }
            catch (Exception)
            {
                MessageBox.Show("未知错误");
            }
        }
    }
}
