using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public class TransmitHistoryItem : HistoryItemBase
    {
        [ForeignKey("ServiceDetail")]
        public int? ServiceDetailId { get; set; }
        public virtual ServiceDetail ServiceDetail { get; set; }

        public string c_objectType { get; set; }
        public string c_xRoadInstance { get; set; }
        [Display(Name = "Тип организации")]
        public string c_memberClass { get; set; }
        [Display(Name = "Код организации")]
        public string c_memberCode { get; set; }
        [Display(Name = "Код подсистемы")]
        public string c_subsystemCode { get; set; }

        [Display(Name = "Кол-во строк в результате")]
        public int? OutputRows { get; set; }
    }
}