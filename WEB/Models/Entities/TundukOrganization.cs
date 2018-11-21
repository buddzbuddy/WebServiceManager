using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public class TundukOrganization
    {
        public int Id { get; set; }
        [Display(Name = "Организация")]
        [Required]
        public string Name { get; set; }
        [Display(Name = "Код в Тундуке")]
        [Required]
        [MaxLength(8),MinLength(8)]
        public string MemberCode { get; set; }

        [Required]
        public string MemberClass { get; set; } = "GOV";

        [Required]
        public string XRoadInstance { get; set; } = "central-server";

        public virtual ICollection<Subsystem> Subsystems { get; set; }
    }
}