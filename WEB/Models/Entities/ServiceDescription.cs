using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using WEB.Models.Enums;

namespace WEB.Models.Entities
{
    public class ServiceDescription
    {
        public Guid Id { get; set; }
        [Display(Name = "Имя соединения(сервиса/клиента)")]
        [Required]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Номер действующего соглашения")]
        public string AgreementNo { get; set; }

        [Required]
        [Display(Name = "Тип соединения")]
        public ConnectionType ConnectionType { get; set; }

        [Required]
        [Display(Name = "Вид взаимодействия")]
        public InteractionType InteractionType { get; set; }

        [Display(Name = "Статус (активный/неактивный)")]
        public bool IsActive { get; set; }

        public virtual ICollection<ClientDetail> ClientDetails { get; set; }
        public virtual ICollection<ServiceDetail> ServiceDetails { get; set; }
    }
}