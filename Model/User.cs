using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WJS_MovieLens.Model
{
    public class User
    {
        public long Id { get; set; }
        public long Age { get; set; }

        [MaxLength(1)]
        public string Gender { get; set; }
        public string ZipCode { get; set; }

        public virtual Occupation Occupation { get; set; }
        public virtual ICollection<UserMovie> UserMovies {get;set;}
    }
}
