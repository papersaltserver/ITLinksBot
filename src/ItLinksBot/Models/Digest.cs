using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItLinksBot.Models
{
    public class Digest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DigestId { get; set; }
        public DateTime DigestDay { get; set; }
        public string DigestName { get; set; }
        public string DigestURL { get; set; }
        public string DigestDescription { get; set; }
        public Provider Provider { get; set; }
        public ICollection<Link> Links { get; set; }
    }

    class DigestComparer : IEqualityComparer<Digest>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Digest x, Digest y)
        {

            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            //Check whether the products' properties are equal.
            return x.DigestURL == y.DigestURL;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Digest d)
        {
            //Check whether the object is null
            if (d is null) return 0;

            //Get hash code for the Name field if it is not null.
            int hashDigestUrl = d.DigestURL == null ? 0 : d.DigestURL.GetHashCode();

            //Calculate the hash code for the product.
            return hashDigestUrl;
        }
    }
}
