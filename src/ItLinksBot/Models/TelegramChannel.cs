using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItLinksBot.Models
{
    public class TelegramChannel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ChannelID { get; set; }
        public string ChannelName { get; set; }
        public Provider Provider { get; set; }
        public ICollection<LinkPost> Posts { get; set; }
    }
}
