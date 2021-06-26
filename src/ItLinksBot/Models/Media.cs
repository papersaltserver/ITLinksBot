using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItLinksBot.Models
{
    public class Media
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string FileName { get; set; }
        public byte[] ContentBytes { get; set; }
        public Link Link { get; set; }
    }
}
