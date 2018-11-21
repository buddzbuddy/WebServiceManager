using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using WEB.Models.Entities;
using WEB.Models.Enums;

namespace WEB.Models
{
    public class RequestByTypeViewModel
    {
        [Display(Name = "Тип подключения")]
        public ConnectionType? ConnectionType { get; set; }

        [Display(Name = "Поток данных")]
        public InteractionType? InteractionType { get; set; }

        public List<ReportItemViewModel> SearchResult { get; set; }
    }

    public class ReportItemViewModel
    {
        public ServiceDescription Connection { get; set; }
    }

    public class RequestByPeriodViewModel
    {
        [Display(Name = "Дата С")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата По")]
        public DateTime? EndDate { get; set; }

        public List<RequestByPeriodItem> SearchResult { get; set; }
    }
    public class RequestByPeriodItem
    {
        public ServiceDescription Connection { get; set; }

        public double _ReceivedDataSize { get; set; }
        [Display(Name = "Размер принятых данных")]
        public string ReceivedDataSizeStr
        {
            get
            {
                var KB = _ReceivedDataSize / 1024;
                string size = "";
                if (KB > 1024)
                {
                    var MB = KB / 1024;
                    size = Math.Round(MB, 1).ToString() + " MB";
                }
                else
                    size = Math.Round(KB, 1).ToString() + " KB";
                return size;
            }
        }

        public double _TransmittedDataSize { get; set; }
        [Display(Name = "Размер переданных данных")]
        public string TransmittedDataSizeStr
        {
            get
            {
                var KB = _TransmittedDataSize / 1024;
                string size = "";
                if (KB > 1024)
                {
                    var MB = KB / 1024;
                    size = Math.Round(MB, 1).ToString() + " MB";
                }
                else
                    size = Math.Round(KB, 1).ToString() + " KB";
                return size;
            }
        }
        [Display(Name = "Кол-во переданных данных (шт.)")]
        public int TransmittedRows { get; set; }
    }


    public class RequestByOrgViewModel
    {
        [Display(Name = "Дата С")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата По")]
        public DateTime? EndDate { get; set; }

        public List<RequestByOrgItem> ReceivedHistoryByOrgs { get; set; }
        public List<RequestByOrgItem> TransmittedHistoryByOrgs { get; set; }
    }
    public class RequestByOrgItem
    {
        [Display(Name = "Подключение")]
        public string ConnectionName { get; set; }

        public TundukOrganization Organization { get; set; }

        public double _ReceivedDataSize { get; set; }
        [Display(Name = "Размер принятых данных")]
        public string ReceivedDataSizeStr
        {
            get
            {
                var KB = _ReceivedDataSize / 1024;
                string size = "";
                if (KB > 1024)
                {
                    var MB = KB / 1024;
                    size = Math.Round(MB, 1).ToString() + " MB";
                }
                else
                    size = Math.Round(KB, 1).ToString() + " KB";
                return size;
            }
        }

        public double _TransmittedDataSize { get; set; }
        [Display(Name = "Размер переданных данных")]
        public string TransmittedDataSizeStr
        {
            get
            {
                var KB = _TransmittedDataSize / 1024;
                string size = "";
                if (KB > 1024)
                {
                    var MB = KB / 1024;
                    size = Math.Round(MB, 1).ToString() + " MB";
                }
                else
                    size = Math.Round(KB, 1).ToString() + " KB";
                return size;
            }
        }
        [Display(Name = "Кол-во переданных данных (шт.)")]
        public int TransmittedRows { get; set; }

    }
}