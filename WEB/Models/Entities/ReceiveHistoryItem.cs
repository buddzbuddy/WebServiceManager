using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WEB.Models.Entities
{
    public class ReceiveHistoryItem : HistoryItemBase
    {

        [ForeignKey("Client")]
        public Guid? ClientId { get; set; }
        public virtual ClientDetail Client { get; set; }

    }
}