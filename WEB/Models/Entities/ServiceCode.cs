using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public class ServiceCode
    {
        public int Id { get; set; }

        [ForeignKey("Subsystem")]
        public int? SubsystemId { get; set; }
        public virtual Subsystem Subsystem { get; set; }

        [Required]
        public string Name { get; set; }

        [Display(Name = "ServiceVersion")]
        [Required]
        public string Version { get; set; } = "v1";

        [NotMapped]
        public string WsdlPath
        {
            get
            {
                var tunduk_host = ConfigurationManager.AppSettings["TUNDUK_HOST"];
                var uriBuilder = new UriBuilder(tunduk_host + "/wsdl");
                var parameters = HttpUtility.ParseQueryString(string.Empty);
                if(Subsystem != null)
                {
                    if (Subsystem.TundukOrganization != null)
                    {
                        parameters["xRoadInstance"] = Subsystem.TundukOrganization.XRoadInstance;
                        parameters["memberClass"] = Subsystem.TundukOrganization.MemberClass;
                        parameters["memberCode"] = Subsystem.TundukOrganization.MemberCode;
                    }
                    parameters["subsystemCode"] = Subsystem.Name;
                    parameters["serviceCode"] = Name;
                    parameters["version"] = Version;
                    uriBuilder.Query = parameters.ToString();
                }
                return uriBuilder.Uri.AbsoluteUri;
            }
        }
    }
}