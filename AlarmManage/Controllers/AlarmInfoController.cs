using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AlarmManage.Controllers
{
    public class AlarmInfoController : Controller
    {
        private Models.alarm_manage_systemEntities db = new Models.alarm_manage_systemEntities();
        // GET: AlarmInfo
        public ActionResult List()
        {
            return View();
        }

        /// <summary>
        /// 历史报警列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetList()
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
                                 PV = o.pv,
                                 TagDiscriptionEN = o.tag_discription,
                                 TagDiscriptionCN = o.mes_tag_name
                             };

                if (tbxTagNameSearch != string.Empty)
                {
                    result = result.Where(w => w.TagName.Contains(tbxTagNameSearch));
                }
                if (!string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) & !string.IsNullOrEmpty(tbxAlarmTimeEndSearch))
                {
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);
                    result = result.Where(w => System.Data.Entity.DbFunctions.DiffDays(w.AlarmTime, dateStart) <= 0 && System.Data.Entity.DbFunctions.DiffMinutes(w.AlarmTime, dateEnd) >= 0);
                }
                return Json(new { total = result.Count(), rows = result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList()});
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
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
                                 PV=o.pv,
                                 TagDiscriptionEN = o.tag_discription,
                                 TagDiscriptionCN = o.mes_tag_name
                             };
                return Json(result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Take(10));
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

                var result = (from o in db.alarm_origin_view
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
                                 PV = o.pv,
                                 TagDiscriptionEN = o.tag_discription,
                                 TagDiscriptionCN = o.mes_tag_name
                             }).AsQueryable();

                if (tbxTagNameSearch != string.Empty)
                {
                    result = result.Where(w => w.TagName.Contains(tbxTagNameSearch));
                }
                if (!string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) & !string.IsNullOrEmpty(tbxAlarmTimeEndSearch))
                {
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);
                    result = result.Where(w => System.Data.Entity.DbFunctions.DiffDays(w.AlarmTime, dateStart) <= 0 && System.Data.Entity.DbFunctions.DiffMinutes(w.AlarmTime, dateEnd) >= 0);
                }

                var frequency = from p in result
                      group p by new { p.TagName ,p.AlarmValue,p.TagDiscriptionCN,p.TagDiscriptionEN} into g
                      select new
                      {
                          AlarmValue=g.Key.AlarmValue,
                          TagName=g.Key.TagName,
                          TagDiscriptionCN = g.Key.TagDiscriptionCN,
                          TagDiscriptionEN = g.Key.TagDiscriptionEN,
                          AlarmFrequency = g.Count()
                      };

                return Json(new { total = frequency.Count(), rows = frequency.OrderByDescending(o => o.AlarmFrequency).ThenByDescending(t=>t.TagName).Skip(offset).Take(limit).ToList() });
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }
    }
}