using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.TelegramDTO
{
    public class TelegramMediaGroup
    {
        public string chat_id { get; set; }
        public ITelegramMedia[] media { get; set; }
    }
}
