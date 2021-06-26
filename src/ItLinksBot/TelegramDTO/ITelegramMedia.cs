using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.TelegramDTO
{
    public interface ITelegramMedia
    {
        public string type { get; }
        public string media { get; set; }
    }
}
