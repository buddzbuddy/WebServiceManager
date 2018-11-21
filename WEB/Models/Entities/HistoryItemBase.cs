using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public abstract class HistoryItemBase
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Дата и время операции")]
        public DateTime EntryTime { get; set; }

        [Required]
        [Display(Name = "Длительность операции")]
        public double OperationDuration { get; set; }

        [NotMapped]
        [Display(Name = "Дата и время конца операции")]
        public DateTime EndTime { get { return EntryTime.AddMilliseconds(OperationDuration); } }

        [Required]
        [Display(Name = "Успешно ли выполнена операция?")]
        public bool IsSuccess { get; set; }

        [Display(Name = "Текст ошибки")]
        public string ErrorMessage { get; set; }

        [Required]
        [Display(Name = "Размер входящих данных")]
        public double? InputSize { get; set; }

        [Required]
        [Display(Name = "Размер исходящих данных")]
        public double? OutputSize { get; set; }
    }
}