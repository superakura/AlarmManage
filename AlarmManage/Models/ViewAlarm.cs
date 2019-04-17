using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlarmManage.Models
{
    public class ViewAlarm
    {
        /// <summary>
        /// 报警值：NR、HH、PH等
        /// </summary>
        public string AlarmValue { get; set; }

        /// <summary>
        /// 位号名
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// 报警时间
        /// </summary>
        public DateTime? AlarmTime { get; set; }

        /// <summary>
        /// 报警时间结束
        /// </summary>
        public DateTime? AlarmTimeEnd { get; set; }

        /// <summary>
        /// HH设置值
        /// </summary>
        public string HH { get; set; }

        /// <summary>
        /// LL设置值
        /// </summary>
        public string LL { get; set; }

        /// <summary>
        /// PH设置值
        /// </summary>
        public string PH { get; set; }

        /// <summary>
        /// PL设置值
        /// </summary>
        public string PL { get; set; }

        /// <summary>
        /// PV的数值
        /// </summary>
        public string PV { get; set; }

        /// <summary>
        /// 英文描述
        /// </summary>
        public string TagDiscriptionEN { get; set; }

        /// <summary>
        /// 中文描述
        /// </summary>
        public string TagDiscriptionCN { get; set; }

        /// <summary>
        /// 报警级别
        /// </summary>
        public int? TagLevel { get; set; }

        /// <summary>
        /// 设备、工艺
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 装置信息
        /// </summary>
        public int? DeptID { get; set; }
    }
}