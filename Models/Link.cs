using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ItLinksBot.Models
{
    public class Link
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LinkID { get; set; }
        public string URL { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int LinkOrder { get; set; }
        public Digest Digest { get; set; }
    }

    class LinkComparer : IEqualityComparer<Link>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Link x, Link y)
        {

            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the products' properties are equal.
            return x.URL == y.URL && x.Digest.DigestName == y.Digest.DigestName;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Link l)
        {
            //Check whether the object is null
            if (l is null) return 0;

            //Get hash code for the Name field if it is not null.
            int hashLinkURL = l.URL == null ? 0 : l.URL.GetHashCode();
            int hashDigestName = l.Digest.DigestName == null ? 0 : l.Digest.DigestName.GetHashCode();
            //Calculate the hash code for the product.
            return hashLinkURL ^ hashDigestName;
        }
    }
}
