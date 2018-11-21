using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public class Subsystem
    {
        public int Id { get; set; }

        [ForeignKey("TundukOrganization")]
        public int? TundukOrganizationId { get; set; }
        public virtual TundukOrganization TundukOrganization { get; set; }

        [Required]
        public string Name { get; set; }
        
        [Required]
        public string TargetNamespace { get; set; }

        [Required]
        public string ProtocolVersion { get; set; } = "4.0";

        public virtual ICollection<ServiceCode> ServiceCodes { get; set; }
    }
}