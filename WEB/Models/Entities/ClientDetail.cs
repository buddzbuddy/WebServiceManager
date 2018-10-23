using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public class ClientDetail
    {
        public Guid Id { get; set; }
        public Guid? ServiceDescriptionId { get; set; }

        public string ServiceURL { get; set; }
        public string TargetNamespace { get; set; }

        #region Header
        public string protocolVersion { get; set; }
        public string issue { get; set; }
        public string id1 { get; set; }
        public string userId { get; set; }
        public string s_objectType { get; set; }
        public string s_xRoadInstance { get; set; }
        public string s_memberClass { get; set; }
        public string s_memberCode { get; set; }
        public string s_subsystemCode { get; set; }
        public string s_serviceCode { get; set; }
        public string s_serviceVersion { get; set; }
        public string c_objectType { get; set; }
        public string c_xRoadInstance { get; set; }
        public string c_memberClass { get; set; }
        public string c_memberCode { get; set; }
        public string c_subsystemCode { get; set; }
        #endregion

        public string WsdlUrl { get; set; }
        public string MethodName { get; set; }
    }
}