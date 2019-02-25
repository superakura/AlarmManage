using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlarmManage.Models
{
    public class ViewAlarm
    {
        public string AlarmValue { get; set; }
        public string TagName { get; set; }
        public DateTime? AlarmTime { get; set; }
        public string HH { get; set; }
        public string LL { get; set; }
        public string PH { get; set; }
        public string PL { get; set; }
        public string PV { get; set; }
        public string TagDiscriptionEN { get; set; }
        public string TagDiscriptionCN { get; set; }
    }
}