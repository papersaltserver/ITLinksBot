using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItLinksBot.Models
{
    public class Provider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProviderID { get; set; }
        public string ProviderName { get; set; }
        public bool ProviderEnabled { get; set; }
        public string DigestURL { get; set; }
        public DateTime LatestSync { get; set; }
        public DateTime LatestIssue { get; set; }
        public int SubsequentErrors { get; set; }
    }
}
