using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Aspose.Cells;
using System.Data.Entity;

namespace AlarmManage.Controllers
{
    public class AlarmInfoController : Controller
    {
        private Models.alarm_manage_systemEntities db = new Models.alarm_manage_systemEntities();

        // GET: AlarmInfo

        public ViewResult List()
        {
            return View();
        }

        public ViewResult HistoryList()
        {
            return View();
        }

        public ViewResult RateList()
        {
            return View();
        }

        /// <summary> 
        /// 获取某段日期范围内的所有日期，以数组形式返回  
        /// </summary>  
        /// <param name="dt1">开始日期</param>  
        /// <param name="dt2">结束日期</param>  
        /// <returns></returns>  
        public DateTime[] GetAllDays(DateTime dt1, DateTime dt2)
        {
            List<DateTime> listDays = new List<DateTime>();
            DateTime dtDay = new DateTime();
            for (dtDay = dt1; dtDay.CompareTo(dt2) <= 0; dtDay = dtDay.AddDays(1))
            {
                listDays.Add(dtDay);
            }
            return listDays.ToArray();
        }

        /// <summary>
        /// 返回时间范围秒数
        /// </summary>
        /// <param name="dateBegin">开始时间</param>
        /// <param name="dateEnd">结束时间</param>
        /// <returns>返回(秒)单位，比如: 0.00239秒</returns>
        public double ExecDateDiff(DateTime dateBegin, DateTime dateEnd)
        {
            TimeSpan ts1 = new TimeSpan(dateBegin.Ticks);
            TimeSpan ts2 = new TimeSpan(dateEnd.Ticks);
            TimeSpan ts3 = ts1.Subtract(ts2).Duration();
            //你想转的格式
            return ts3.TotalSeconds;
        }

        /// <summary>
        /// 实时报警列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetListNow()
        {
            try
            {
                var result = from o in db.alarm_origin_view
                             where o.alarm_value != "NR"
                             select new
                             {
                                 AlarmValue = o.alarm_value,
                                 TagName = o.tag_name,
                                 AlarmTime = o.time,
                                 o.HH,
                                 o.LL,
                                 o.PH,
                                 o.PL,
                                 o.PV,
                                 TagLevel=o.tag_level,
                                 Type=o.type,
                                 TagDiscriptionEN = o.tag_discription,
                                 TagDiscriptionCN = o.mes_tag_name
                             };
                return Json(result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Take(15));
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        /// <summary>
        /// 历史报警列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetListHistory()
        {
            try
            {
                var limit = 0;
                int.TryParse(Request.Form["limit"], out limit);
                var offset = 0;
                int.TryParse(Request.Form["offset"], out offset);

                var tbxTagNameSearch = Request.Form["tbxTagNameSearch"];//位号
                var tbxAlarmTimeBeginSearch = Request.Form["tbxAlarmTimeBeginSearch"];//报警时间范围开始
                var tbxAlarmTimeEndSearch = Request.Form["tbxAlarmTimeEndSearch"];//报警时间范围结束
                var ddlTeamSearch = Request.Form["ddlTeamSearch"];//班次

                var result = from o in db.alarm_origin_view
                             where o.alarm_value != "NR"
                             select new
                             {
                                 AlarmValue = o.alarm_value,
                                 TagName = o.tag_name,
                                 AlarmTime = o.time,
                                 o.HH,
                                 o.LL,
                                 o.PH,
                                 o.PL,
                                 o.PV,
                                 TagLevel = o.tag_level,
                                 Type = o.type,
                                 TagDiscriptionEN = o.tag_discription,
                                 TagDiscriptionCN = o.mes_tag_name
                             };
                if (tbxTagNameSearch != string.Empty)
                {
                    result = result.Where(w => w.TagName == (tbxTagNameSearch + ".ALRM"));
                }

                //选择日期范围
                if (!string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) & !string.IsNullOrEmpty(tbxAlarmTimeEndSearch) & ddlTeamSearch == string.Empty)
                {
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch+" 00:00:00");
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch + " 23:59:59");
                    result = result.Where(w => DbFunctions.DiffSeconds(w.AlarmTime, dateStart) <= 0 && DbFunctions.DiffSeconds(w.AlarmTime, dateEnd) >= 0);
                }

                //选择班组
                if (ddlTeamSearch != string.Empty)
                {
                    if (string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) || string.IsNullOrEmpty(tbxAlarmTimeEndSearch))
                    {
                        //日期范围不能为空
                        return Json("ErrorDateRange");
                    }
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);

                    var listDateRange = new List<Models.ViewAlarm>();

                    //获取日期范围列表
                    var dateRange = GetAllDays(dateStart, dateEnd);

                    //如果日期范围大于31天，不能查询,返回错误信息。
                    if (dateRange.Length > 31)
                    {
                        return Json("ErrorDateRange");
                    }
                    switch (ddlTeamSearch)
                    {
                        case "一班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Monday" || dateRange[i].DayOfWeek.ToString() == "Saturday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Friday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Wednesday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "二班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday" || dateRange[i].DayOfWeek.ToString() == "Sunday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Monday" || dateRange[i].DayOfWeek.ToString() == "Saturday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Thursday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "三班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Wednesday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday" || dateRange[i].DayOfWeek.ToString() == "Sunday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Friday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "四班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Wednesday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Monday" || dateRange[i].DayOfWeek.ToString() == "Saturday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "五班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Friday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Thursday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday" || dateRange[i].DayOfWeek.ToString() == "Sunday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            HH = s.HH,
                                            LL = s.LL,
                                            PH = s.PH,
                                            PL = s.PL,
                                            PV = s.PV,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                    }
                    return Json(new { total = listDateRange.Count(), rows = listDateRange.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList() });
                }

                return Json(new { total = result.Count(), rows = result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList() });
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        /// <summary>
        /// 频次统计列表
        /// </summary>
        /// <returns></returns>
        public JsonResult GetListFrequency()
        {
            try
            {
                var limit = 0;
                int.TryParse(Request.Form["limit"], out limit);
                var offset = 0;
                int.TryParse(Request.Form["offset"], out offset);

                var tbxTagNameSearch = Request.Form["tbxTagNameSearch"];//位号
                var tbxAlarmTimeBeginSearch = Request.Form["tbxAlarmTimeBeginSearch"];//报警时间范围开始
                var tbxAlarmTimeEndSearch = Request.Form["tbxAlarmTimeEndSearch"];//报警时间范围结束
                var ddlTeamSearch = Request.Form["ddlTeamSearch"];//班次

                var result = (from o in db.alarm_origin_view
                              where o.alarm_value != "NR"
                              select new
                              {
                                  AlarmValue = o.alarm_value,
                                  TagName = o.tag_name,
                                  AlarmTime = o.time,
                                  TagDiscriptionEN = o.tag_discription,
                                  TagDiscriptionCN = o.mes_tag_name
                              }).AsQueryable();

                if (tbxTagNameSearch != string.Empty)
                {
                    result = result.Where(w => w.TagName == (tbxTagNameSearch + ".ALRM"));
                }

                if (!string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) & !string.IsNullOrEmpty(tbxAlarmTimeEndSearch))
                {
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);
                    result = result.Where(w => DbFunctions.DiffSeconds(w.AlarmTime, dateStart) <= 0 && DbFunctions.DiffSeconds(w.AlarmTime, dateEnd) >= 0);
                }

                //选择班组
                if (ddlTeamSearch != string.Empty)
                {
                    if (string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) || string.IsNullOrEmpty(tbxAlarmTimeEndSearch))
                    {
                        //日期范围不能为空
                        return Json("ErrorDateRange");
                    }
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);

                    var listDateRange = new List<Models.ViewAlarm>();

                    //获取日期范围列表
                    var dateRange = GetAllDays(dateStart, dateEnd);

                    //如果日期范围大于31天，不能查询,返回错误信息。
                    if (dateRange.Length > 31)
                    {
                        return Json("ErrorDateRange");
                    }
                    switch (ddlTeamSearch)
                    {
                        case "一班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Monday" || dateRange[i].DayOfWeek.ToString() == "Saturday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Friday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Wednesday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "二班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday" || dateRange[i].DayOfWeek.ToString() == "Sunday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Monday" || dateRange[i].DayOfWeek.ToString() == "Saturday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Thursday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "三班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Wednesday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday" || dateRange[i].DayOfWeek.ToString() == "Sunday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Friday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "四班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Wednesday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Monday" || dateRange[i].DayOfWeek.ToString() == "Saturday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                        case "五班":
                            for (int i = 0; i < dateRange.Length; i++)
                            {
                                if (dateRange[i].DayOfWeek.ToString() == "Friday")
                                {
                                    var dateOneStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 00:00:00");
                                    var dateOneEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");

                                    var one = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateOneEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(one.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Thursday")
                                {
                                    var dateTwoStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 08:00:00");
                                    var dateTwoEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");

                                    var two = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateTwoEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(two.ToList());
                                }
                                if (dateRange[i].DayOfWeek.ToString() == "Tuesday" || dateRange[i].DayOfWeek.ToString() == "Sunday")
                                {
                                    var dateThreeStart = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 16:00:00");
                                    var dateThreeEnd = Convert.ToDateTime(dateRange[i].ToShortDateString() + " 23:59:59");

                                    var three = result
                                        .Where(w => System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeStart) <= 0 && System.Data.Entity.DbFunctions.DiffSeconds(w.AlarmTime, dateThreeEnd) >= 0)
                                        .Select(s => new Models.ViewAlarm
                                        {
                                            TagName = s.TagName,
                                            AlarmTime = s.AlarmTime,
                                            AlarmValue = s.AlarmValue,
                                            TagDiscriptionCN = s.TagDiscriptionCN,
                                            TagDiscriptionEN = s.TagDiscriptionEN
                                        });
                                    listDateRange.AddRange(three.ToList());
                                }
                            }
                            break;
                    }

                    var frequencyTeam = from p in listDateRange
                                        group p by new { p.TagName, p.AlarmValue, p.TagDiscriptionCN, p.TagDiscriptionEN } into g
                                        select new
                                        {
                                            AlarmValue = g.Key.AlarmValue,
                                            TagName = g.Key.TagName,
                                            TagDiscriptionCN = g.Key.TagDiscriptionCN,
                                            TagDiscriptionEN = g.Key.TagDiscriptionEN,
                                            AlarmFrequency = g.Count()
                                        };

                    return Json(new { total = frequencyTeam.Count(), rows = frequencyTeam.OrderByDescending(o => o.AlarmFrequency).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList() });
                }

                var frequency = from p in result
                                group p by new { p.TagName, p.AlarmValue, p.TagDiscriptionCN, p.TagDiscriptionEN } into g
                                select new
                                {
                                    AlarmValue = g.Key.AlarmValue,
                                    TagName = g.Key.TagName,
                                    TagDiscriptionCN = g.Key.TagDiscriptionCN,
                                    TagDiscriptionEN = g.Key.TagDiscriptionEN,
                                    AlarmFrequency = g.Count()
                                };
                return Json(new { total = frequency.Count(), rows = frequency.OrderByDescending(o => o.AlarmFrequency).ThenByDescending(t => t.TagName).Skip(offset).Take(limit).ToList() });
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        /// <summary>
        /// 报警率计算
        /// </summary>
        /// <returns></returns>
        public JsonResult GetListRate()
        {
            try
            {
                var limit = 0;
                int.TryParse(Request.Form["limit"], out limit);
                var offset = 0;
                int.TryParse(Request.Form["offset"], out offset);

                var tbxTagNameSearch = Request.Form["tbxTagNameSearch"];//位号
                var tbxAlarmTimeBeginSearch = Request.Form["tbxAlarmTimeBeginSearch"];//报警时间范围开始
                var tbxAlarmTimeEndSearch = Request.Form["tbxAlarmTimeEndSearch"];//报警时间范围结束

                var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);

                var secondRange = ExecDateDiff(dateStart,dateEnd);

                var frequency = from p in db.alarm_origin_view
                                where p.alarm_value != "NR" && DbFunctions.DiffSeconds(p.time, dateStart) <= 0 && DbFunctions.DiffSeconds(p.time, dateEnd) >= 0
                                group p by new { p.tag_name,p.mes_tag_name,p.tag_discription,p.type,p.tag_level } into g
                                select new
                                {
                                    TagName = g.Key.tag_name,
                                    Type = g.Key.type,
                                    TagLevel = g.Key.tag_level,
                                    TagDiscriptionCN = g.Key.mes_tag_name,
                                    TagDiscriptionEN = g.Key.tag_discription,
                                    TimeFrequency =g.Sum(s=> DbFunctions.DiffSeconds(s.end_time, dateEnd) <= 0? DbFunctions.DiffSeconds(s.start_time, dateEnd) : DbFunctions.DiffSeconds(s.start_time, s.end_time)),
                                    Rate= (g.Sum(s => DbFunctions.DiffSeconds(s.end_time, dateEnd) <= 0 ? DbFunctions.DiffSeconds(s.start_time, dateEnd) : DbFunctions.DiffSeconds(s.start_time, s.end_time)) / secondRange),
                                    Range=secondRange
                                };
                if (tbxTagNameSearch != string.Empty)
                {
                    frequency = frequency.Where(w => w.TagName == (tbxTagNameSearch + ".ALRM"));
                }

                return Json(new { total = frequency.Count(),
                    rows = frequency.OrderByDescending(o => o.Rate).Skip(offset).Take(limit).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
    }
}