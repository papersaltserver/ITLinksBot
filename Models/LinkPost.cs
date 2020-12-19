using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ItLinksBot.Models
{
    public class LinkPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PostID { get; set; }
        public TelegramChannel Channel { get; set; }
        public int TelegramMessageID { get; set; }
        public DateTime PostDate { get; set; }
        public string PostLink { get; set; }
        public string PostText { get; set; }
        public Link Link { get; set; }
        
    }
}
