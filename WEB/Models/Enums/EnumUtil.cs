using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace WEB.Models.Enums
{
    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static List<SelectListItem> ToSelect<T>() where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var enumItems = GetValues<T>();

            var items = new List<SelectListItem>();
            foreach(var eItem in enumItems)
            {
                items.Add(new SelectListItem
                {
                    Text = eItem.ToString(),
                    Value = eItem.ToString()
                });
            }
            return items;
        }
    }
}
