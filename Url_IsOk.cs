using System;
using HtmlAgilityPack;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Threading;

namespace ADS_Web后台弱口令扫描
{
    public partial class Url_IsOk : Form
    {
        public static HtmlNode uname_input;
        public Url_IsOk()
        {
            InitializeComponent();
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

        private void Error_show(string url)
        {
            label2.Text = url;
        }
        public delegate void Fclose();//定义委托
        private void Error_Close()
        {
            Form1.m_form1.Start();
            this.Close();
        }

        /*
         *HEAD请求确认能否访问，返回Bool值
         */
        public bool Prepare(string url)
        {
            try
            {
                //对目标URL发送GET请求，判断目标是否存活
                HttpWebRequest Req_HEAD = (HttpWebRequest)WebRequest.Create(url);
                if (Form1.Proxy_server != null && Form1.Proxy_port != null)
                {
                    var proxy = new WebProxy(Form1.Proxy_server + ":" + Form1.Proxy_port, true);
                    Req_HEAD.Proxy = proxy;
                }

                Req_HEAD.Method = "GET";
                Req_HEAD.AllowAutoRedirect = false;
                Req_HEAD.ContentType = "text/html;charset=UTF-8";
                Req_HEAD.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
                Req_HEAD.Timeout = 10000;

                HttpWebResponse Rep_HEAD = (HttpWebResponse)Req_HEAD.GetResponse();
                int ret_HEAD = (int)Rep_HEAD.StatusCode;
                //如果不能访问，就立即停止本线程，返回 False
                if (ret_HEAD == 200 || ret_HEAD == 302 || ret_HEAD == 301)
                {
                    return true;
                }
                else
                {
                    string message = url + "  目标地址无法访问";
                    LogAdd ld = new LogAdd(Error_log);
                    this.BeginInvoke(ld, new object[] { message });
                    return false;
                }
            }
            catch (Exception ex)//捕获异常，然后退出
            {
                string message = url + "  目标地址无法访问 -> 捕获异常 "+ex.ToString();
                LogAdd ld = new LogAdd(Error_log);
                this.BeginInvoke(ld, new object[] { message });
                return false;
            }
        }


        /*
         * 定位Form表单位置
         */
        public bool Find_form(string url)
        {
            try
            {
                //定位form表单,解析出用户名和密码字段
                var web = new HtmlWeb();
                var doc = web.Load(url);
                string OA_Selected = Form1.OA_Selected;

                //目标类型选择
                switch (OA_Selected)
                {
                    case "通达OA":
                        uname_input = doc.DocumentNode.SelectSingleNode("//input[@name='UNAME']");
                        break;
                    case "致远OA":
                        uname_input = doc.DocumentNode.SelectSingleNode("//input[@name='login_username']");
                        break;
                    default:
                        uname_input = doc.DocumentNode.SelectSingleNode("//input[1]");
                        break;

                }
                
                HtmlNode pass_input = doc.DocumentNode.SelectSingleNode("//input[@type='password']");
                
                if (uname_input == null || pass_input == null)
                {
                    return false;//无法找到登录表单
                }
                else
                {
                    Form1.User_field = uname_input.Attributes["name"].Value;//用户名字段
                    Form1.Pass_field = pass_input.Attributes["name"].Value;//密码字段
                    return true;
                }
            }catch(Exception ex)
            {
                string message = "无法找到登录表单 " + url + " " + ex.ToString();
                LogAdd ld = new LogAdd(Error_log);
                this.BeginInvoke(ld, new object[] { message });
                return false;
            }
        }
        //写入URL
        public delegate void UrlAdd(string url);//定义委托
        private void U_add(string url)
        {
            Form1.Url.Add(url);
        }

        //筛选出不能访问，不能找到表单的目标 方法
        public void IsOk_URL(string url)
        {
            string OA_Selected = Form1.OA_Selected;
            LogAdd lds = new LogAdd(Error_show);
            this.BeginInvoke(lds, new object[] { url });

            switch (OA_Selected)
            {
                case "tomcat":
                    if (Prepare(url))
                    {
                        UrlAdd ua = new UrlAdd(U_add);
                        this.BeginInvoke(ua, new object[] { url });
                    }
                    break;
                default:
                    if (Prepare(url) && Find_form(url))
                    {
                        UrlAdd ua = new UrlAdd(U_add);
                        this.BeginInvoke(ua, new object[] { url });
                    }
                    break;

            }
        }

        private void Url_IsOk_Load(object sender, EventArgs e)
        {
            try
            {
                ThreadPool.SetMinThreads(1, 1);////设置线程池在新请求预测中维护的空闲线程数
                ThreadPool.SetMaxThreads(1000, 1000);//设置线程池最大线程数，用来控制线程
                RegisteredWaitHandle rhw = null;//为线监控程池线程结束做准备
                StreamReader sr = new StreamReader(Form1.Urls_path);//从该文件加载URL目标字典
                while (!sr.EndOfStream)
                {
                    string url = sr.ReadLine();//遍历目标
                    ThreadPool.UnsafeQueueUserWorkItem(uk => IsOk_URL(url), new object[] { url });
                }
                sr.Close();
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
                        Fclose ldC = new Fclose(Error_Close);
                        this.BeginInvoke(ldC, new object[] { });
                        rhw = null;


                    }
                }), null, 100, false);
            }
            catch (Exception)
            {
                MessageBox.Show("请先导入目标地址");
                //string message = "枚举验证错误 " + exd.ToString();//这个没写入日志
                Fclose ldC = new Fclose(Error_Close);
                this.BeginInvoke(ldC, new object[] {});
            }
        }
    }
}
