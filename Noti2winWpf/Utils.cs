using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Noti2winWpf
{
    class Utils
    {
        public static string LogErr = "  ERROR:";
        public static string LogRun = " RUNTIME:";
        public static Bitmap GenerateQRCode(string text)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCoder.QRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(20);
                }
            }
        }

        // 将 IP 地址压缩成更短的字符串
        public static string CompressIPs(List<string> ips)
        {
            if (ips == null || ips.Count == 0) return "";

            List<int> ipInts = ips.Select(IPToInt).ToList();

            // Delta Encoding: 计算相邻 IP 之间的差值
            List<int> deltas = new List<int> { ipInts[0] }; // 记录第一个 IP
            for (int i = 1; i < ipInts.Count; i++)
            {
                deltas.Add(ipInts[i] - ipInts[i - 1]);
            }

            // 将差值转换为字节流（强制大端字节序）
            byte[] bytes = deltas.SelectMany(i => BitConverter.GetBytes(i).Reverse()).ToArray();

            // 使用 Base64 进行压缩
            return Convert.ToBase64String(bytes);
        }

        // 解压缩 IP 地址
        static List<string> DecompressIPs(string compressed)
        {
            if (string.IsNullOrEmpty(compressed)) return new List<string>();

            byte[] bytes = Convert.FromBase64String(compressed);
            List<int> deltas = new List<int>();

            // 解析字节流，恢复差值列表（大端字节序）
            for (int i = 0; i < bytes.Length; i += 4)
            {
                deltas.Add(BitConverter.ToInt32(bytes.Skip(i).Take(4).Reverse().ToArray(), 0));
            }

            // 还原 IP 地址
            List<int> ipInts = new List<int> { deltas[0] };
            for (int i = 1; i < deltas.Count; i++)
            {
                ipInts.Add(ipInts[i - 1] + deltas[i]);
            }

            return ipInts.Select(IntToIP).ToList();
        }

        // IPv4 转换为整数
        static int IPToInt(string ip)
        {
            var parts = ip.Split('.').Select(byte.Parse).ToArray();
            return (parts[0] << 24) | (parts[1] << 16) | (parts[2] << 8) | parts[3];
        }

        // 整数转换回 IPv4
        static string IntToIP(int value)
        {
            return $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";
        }

        public static string Hex2Str(int num) {
            string hexStr=num.ToString("X4");
            string input = "0123456789";
            string map = "zyxwvutsrq";
            string result = "";
            foreach (char c in hexStr) {
                if (input.Contains(c))
                {
                    int index = c - '0';
                    result += map[index];
                }
                else {
                    result += c;
                }
            }
            return result;
        }

        public static void WriteLog(string message,string logType)
        {
            try
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    if (logType == null) {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
                    }
                    else if (logType == LogErr)
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogErr} {message}");
                    }
                    else if (logType == LogRun)
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogRun} {message}");
                    }
                  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logging error: " + ex.Message);
            }
        }


        public static int GetAvailablePort(int startPort = 10980)
        {
            const int maxPort = 65535;
            int port = startPort;
            while (port <= maxPort)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch (SocketException)
                {
                    port++;
                }
            }
            return -1; // 如果没有可用端口，返回 -1
        }

        public static Bitmap CreateTextImage(string text, int width = 300, int height = 300)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // 填充背景色
                g.Clear(System.Drawing.Color.White);

                // 定义绘制文本时使用的字体和画刷
                using (Font font = new Font("Arial", 20, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    // 测量文本所占用的大小
                    SizeF textSize = g.MeasureString(text, font);

                    // 计算使文本居中的位置
                    float x = (width - textSize.Width) / 2;
                    float y = (height - textSize.Height) / 2;

                    // 绘制居中文本
                    g.DrawString(text, font, Brushes.Black, x, y);
                }
            }
            return bmp;
        }

        public static bool IsValidExecutablePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            if (!Path.IsPathRooted(path))
                return false;
            if (!File.Exists(path))
                return false;
            if (!string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
    }
}
