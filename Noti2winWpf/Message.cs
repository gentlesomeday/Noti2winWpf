using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noti2winWpf
{
    class Message
    {
        public int type { get; set; }
        public string content { get; set; }
        public string title { get; set; }
        public int uuid { get; set; }
        public string time { get; set; }
        public string iconBase64 { get; set; }

        public Message(int type, string content, string title, int uuid, string time, string iconBase64)
        {
            this.type = type;
            this.content = content;
            this.title = title;
            this.uuid = uuid;
            this.time = time;
            this.iconBase64 = iconBase64;
        }
    }
}
