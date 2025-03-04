using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noti2winWpf
{
    class Utils
    {
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
       

}
}
