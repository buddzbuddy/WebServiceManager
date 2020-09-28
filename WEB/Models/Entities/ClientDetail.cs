using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WEB.Models.Entities
{
    public class ClientDetail
    {
        public Guid Id { get; set; }

        [ForeignKey("ServiceDescription")]
        public Guid? ServiceDescriptionId { get; set; }
        public virtual ServiceDescription ServiceDescription { get; set; }


        [ForeignKey("ServiceCode")]
        [Required]
        public int? ServiceCodeId { get; set; }
        public virtual ServiceCode ServiceCode { get; set; }

        [Required]
        [Display(Name = "Шаблон XML из SoapUI")]
        [DataType(DataType.MultilineText)]
        [AllowHtml]
        public string RequestXML { get; set; }
    }
}