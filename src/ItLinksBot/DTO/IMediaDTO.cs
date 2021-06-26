using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.DTO
{
    public interface IMediaDTO
    {
        public string FileName { get; set; }
        public byte[] ContentBytes { get; set; }
    }
}
