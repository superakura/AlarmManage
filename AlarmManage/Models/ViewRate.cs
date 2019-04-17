using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlarmManage.Models
{
    public class ViewRate
    {
        /// <summary>
        /// 位号名
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// 设备、工艺
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 报警级别
        /// </summary>
        public int? TagLevel { get; set; }

        /// <summary>
        /// 中文描述
        /// </summary>
        public string TagDiscriptionCN { get; set; }

        /// <summary>
        /// 英文描述
        /// </summary>
        public string TagDiscriptionEN { get; set; }

        /// <summary>
        /// 报警时长（秒）
        /// </summary>
        public int? TimeFrequency { get; set; }

        /// <summary>
        /// 报警率
        /// </summary>
        public double? Rate { get; set; }

        /// <summary>
        /// 时间范围秒数
        /// </summary>
        public double Range { get; set; }
    }
}