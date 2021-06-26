using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.TelegramDTO
{
    public class TelegramPhoto : ITelegramMedia
    {
        public string type { get; } = "photo";
        public string media { get; set; }
        public string caption { get; set; }
        public bool ShouldSerializecaption()
        {
            return caption != "";
        }
    }
}
