using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Threading.Tasks;


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
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            init();
        }
        private int port = 10980;
        private NotifyIcon notifyIcon;
        private Queue<string> logQueue = new Queue<string>();
        private bool isProcessingLogQueue = false;
        private int autoMinisize = 1;
        AppConfig config = null;
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
                    try
                    {
                        message = JsonConvert.DeserializeObject<Message>(requestBody);
                        if (message.type == -1)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                //处理链接成功情况
                                addTextLog("设备连接成功");
                                SetSuccessImage();
                                if (autoMinisize==1)
                                    MinimizeWindowWithDelay();
                            });
                            responseString = "1";
                        }
                        else
                        {
                            Console.WriteLine(requestBody);
                            showNoti((NotiType)message.type, message.title, message.content);
                            responseString = "" + message.uuid;
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
            string conType = "wechat";
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

        public enum NotiType
        {
            wechat = 0,
            qq = 1
        }

        private void setExePath()
        {
            App.QQPathStr = GetQQNTExecutablePath();
            App.WeChatPathStr = GetWeChatExecutablePath();
            if (App.QQPathStr ==null || App.QQPathStr == string.Empty)
            {

                if (config.QQPath.Equals(""))
                {
                    //处理QQNT没找到路径
                    addTextLog("QQ安装检测: 失败");
                    Utils.WriteLog("Unable to find the executable file for QQNT", Utils.LogRun);
                }
                else
                {
                    if (Utils.IsValidExecutablePath(@config.QQPath))
                    {
                        addTextLog("QQ安装检测: 读取用户自定义");
                        App.QQPathStr = @config.QQPath;
                    }
                    else
                    {
                        //处理QQNT路径不合法
                        addTextLog("QQ安装检测: 用户自定义有误");
                        Utils.WriteLog("QQNT path is invalid", Utils.LogRun);
                    }
                }
            }
            else { 
                addTextLog("QQ安装检测: 注册表读取成功");
            }
            if (App.WeChatPathStr == null || App.WeChatPathStr == string.Empty)
            {

                if (config.WeChatPath.Equals(""))
                {
                    //处理微信没找到路径
                    addTextLog("微信安装检测: 失败");
                    Utils.WriteLog("Unable to find the executable file for wechat", Utils.LogRun);
                }
                else
                {
                    if (Utils.IsValidExecutablePath(@config.QQPath))
                    {
                        addTextLog("微信安装检测: 读取用户自定义");
                        App.WeChatPathStr = config.WeChatPath;
                    }
                    else
                    {
                        //处理微信没找到路径
                        addTextLog("微信安装检测: 失败");
                        Utils.WriteLog("Wechat is invalid", Utils.LogRun);
                    }

                }
              
            }
            else
            {
                addTextLog("微信安装检测: 注册表读取成功");
            }

        }
        private static string GetWeChatExecutablePath()
        {
            try
            {
                string regPath = @"Software\Tencent\WeChat";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                {
                    if (key != null)
                    {
                        object installPath = key.GetValue("InstallPath");
                        if (installPath != null)
                        {
                            string exePath = Path.Combine(installPath.ToString(), "WeChat.exe");
                            if (File.Exists(exePath))
                                return exePath;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.WriteLog("Error getting WeChat path: " + e.Message, Utils.LogErr);
            }


            return string.Empty;
        }


        private static string GetQQNTExecutablePath()
        {
            try
            {
                string regPath = @"Software\Tencent\QQNT";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath))
                {
                    if (key != null)
                    {
                        object installPath = key.GetValue("Install");
                        if (installPath != null)
                        {
                            string exePath = Path.Combine(installPath.ToString(), "QQ.exe");
                            if (File.Exists(exePath))
                                return exePath;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utils.WriteLog("Error getting QQNT path: " + e.Message, Utils.LogErr);
            }

            return string.Empty;
        }

        private void init()
        {
            try
            {
                //管理员权限 防火墙
                ConfigureFirewall(port);
                //设置端口
                port = Utils.GetAvailablePort(port);
                if (port == -1)
                {
                    //处理没有可用端口情况
                    setImageText(image, "无可用端口");
                    addTextLog("开启监听：失败");
                    Utils.WriteLog("No available port", Utils.LogRun);
                    return;
                }
                var localIPs = GetLocalIPv4s();
                if (localIPs.Count == 0)
                {
                    //处理没有可用ip情况
                    setImageText(image, "连接网络失败");
                    addTextLog("开启监听：失败");
                    Utils.WriteLog("No available ip", Utils.LogRun);
                    return;
                }
                addTextLog("开启监听：成功");
                config = ConfigManager.LoadConfig();
                autoMinisize = config.AutoMinimize;
                //设置exe路径
                setExePath();
                Thread httpListenerThread = new Thread(StartHttpListener);
                httpListenerThread.IsBackground = true;
                httpListenerThread.Start();
                Utils.WriteLog("HTTP Listener is running", Utils.LogRun);
                var ipStrs = string.Join(", ", localIPs);
                Console.WriteLine(ipStrs);
                var coms = Utils.CompressIPs(localIPs);
                var addStr = Utils.Hex2Str(port) + coms;
                setImage(image, addStr);
            }
            catch (Exception ex)
            {
                Utils.WriteLog("Error initializing: " + ex.Message, Utils.LogErr);
                Console.WriteLine("Error initializing: " + ex.Message);
            }

        }

        private void setImage(System.Windows.Controls.Image image, string content)
        {
            var qrImageBm = Utils.GenerateQRCode(content);
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

        private void setImageText(System.Windows.Controls.Image image, string content)
        {
            try
            {
                double imgWidth = image.ActualWidth;
                double imgHeight = image.ActualHeight;
                if (imgWidth <= 0 || imgHeight <= 0)
                {
                    imgWidth = 300;
                    imgHeight = 300;
                }

                var textBmp = Utils.CreateTextImage(content, (int)imgWidth, (int)imgHeight);
                using (MemoryStream memory = new MemoryStream())
                {
                    textBmp.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    image.Source = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                Utils.WriteLog("Error generating text image: " + ex.Message, Utils.LogErr);
                Console.WriteLine("Error generating text image: " + ex.Message);
            }
        }

        private async void addTextLog(string content)
        {
            logQueue.Enqueue(content);
            if (!isProcessingLogQueue)
            {
                ProcessLogQueue();
            }

        }

        private async void ProcessLogQueue()
        {
            isProcessingLogQueue = true;
            while (logQueue.Count > 0)
            {
                string content = logQueue.Dequeue();
                string labelText = label.Content?.ToString();
                if (string.IsNullOrEmpty(labelText))
                {
                    label.Content = content;
                }
                else
                {
                    label.Content = labelText + "\n" + content;
                }
                await Task.Delay(500); // 延迟0.5秒
            }
            isProcessingLogQueue = false;
        }


        private void SetSuccessImage()
        {
            try
            {
                string imgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", "success.png");
                if (File.Exists(imgPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imgPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    image.Source = bitmap;
                }
                else
                {
                    setImageText(image, "连接成功");
                }
            }
            catch (Exception ex)
            {
                Utils.WriteLog("Error loading success.png: " + ex.Message, Utils.LogErr);
                Console.WriteLine("加载 success.png 出错: " + ex.Message);
            }
        }

        private async void MinimizeWindowWithDelay()
        {
            await Task.Delay(2500); // 延迟1.5秒，非阻塞
            this.WindowState = WindowState.Minimized;
        }

    }
}
