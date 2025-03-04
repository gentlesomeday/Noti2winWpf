using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.System;
using QRCoder;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Noti2winWpf
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CreateNotifyIcon();
        }

        int port = 10980;
        private NotifyIcon notifyIcon;

        private void CreateNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon("icon.ico");
            notifyIcon.Text = "WPF 应用最小化到托盘";
            notifyIcon.Visible = true;

            // 创建右键菜单
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem showItem = new ToolStripMenuItem("显示窗口", null, (s, e) => ShowWindow());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出", null, (s, e) => System.Windows.Application.Current.Shutdown());
            menu.Items.Add(showItem);
            menu.Items.Add(exitItem);
            notifyIcon.ContextMenuStrip = menu;

            // 点击托盘图标恢复窗口
            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ShowWindow();
            };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide(); // 隐藏窗口
                notifyIcon.ShowBalloonTip(2000, "通知提示", "应用已最小化到托盘", ToolTipIcon.Info);
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Dispose(); // 释放资源
            base.OnClosing(e);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigureFirewall(port);
                Thread httpListenerThread = new Thread(StartHttpListener);
                httpListenerThread.IsBackground = true;
                httpListenerThread.Start();
                textBlock.Text = "HTTP Listener is running";
                var localIPs = GetLocalIPv4s();
                var ipStrs= string.Join(", ", localIPs);
                Console.WriteLine(ipStrs);
                var coms= Utils.CompressIPs(localIPs);
                var addStr = Utils.Hex2Str(port)+ coms;
                Console.WriteLine(addStr);

                var qrImageBm = Utils.GenerateQRCode(addStr);
                using (MemoryStream memory = new MemoryStream())
                {
                    qrImageBm.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    image.Source = bitmapImage;
                }
            }
            catch { 
            textBlock.Text = "HTTP Listener is not running";
            }
        }
        private void ConfigureFirewall(int port)
        {
            try
            {
                string ruleName = "Allow Port " + port;
                string command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={port}";
                ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process process = Process.Start(processStartInfo);
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to configure firewall: " + ex.Message);
            }
        }


        private void StartHttpListener()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:10980/");
            listener.Start();
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                // 解析请求的内容
                string requestBody;
                string responseString = "0";
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    requestBody = reader.ReadToEnd();
                    
                    Message message = null;
                    try {
                        // 如果消息类型为-1 那么则为连接测试
                      //  requestBody = requestBody.Replace("\\","");
                        //if (requestBody.StartsWith("\"")) {
                        //    requestBody=requestBody.Substring(1, requestBody.Length - 2);
                        //}
                        message = JsonConvert.DeserializeObject<Message>(requestBody);
                        if (message.type == -1)
                        {
                            Dispatcher.Invoke(() => {
                                textBlock1.Text = "有设备加入";
                            });
                            responseString = "1";
                        }
                        else {
                            Console.WriteLine(requestBody);
                            showNoti((NotiType)message.type, message.title, message.content);
                            responseString = ""+message.uuid;
                        }
                    }
                    catch (Exception e)
                    {
                        responseString = e.ToString();
                        Console.WriteLine(e);
                    }
                }
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }
        private void showNoti(NotiType type, string title, string content)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string imgPath = System.IO.Path.Combine(currentDirectory, "Image", "wechat.png");
            string argumentStr = "conversation";
            string conType= "wechat";
            if (type == NotiType.qq)
            {
                conType = "qq";
                imgPath = System.IO.Path.Combine(currentDirectory, "Image", "QQ.png");
            }

            new ToastContentBuilder()
                .AddArgument(argumentStr, conType)
                .AddText(title)
                .AddText(content)
                .AddAppLogoOverride(new Uri(@imgPath), ToastGenericAppLogoCrop.Default)
                .Show();
        }


        private List<string> GetLocalIPv4s()
        {
            List<string> localIPs = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIPs.Add(ip.ToString());
                }
            }
            return localIPs;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            showNoti(NotiType.wechat, "主窗口测试", "这是一条来自主窗口的消息内容");
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        }


  


        public enum NotiType
        {
            wechat = 0,
            qq = 1
        }
    }
}
