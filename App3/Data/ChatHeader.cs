using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App3.Data
{
    public class ChatHeader
    {
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public string Href { get; set; }
        public int UnreadCount { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public int Order = -1;
    }
}
