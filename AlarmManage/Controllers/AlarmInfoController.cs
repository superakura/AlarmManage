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

                var result = from o in db.alarm_origin
                             join t in db.tag_info on o.tag_id equals t.id
                             join m in db.mes_tag on t.tag_name equals m.mes_tag1
                             where o.alarm_value != "NR"
                             select new {
                                 AlarmValue = o.alarm_value,
                                 TagName = t.tag_name,
                                 AlarmTime=o.time,
                                 TagDiscriptionEN=t.tag_discription,
                                 TagDiscriptionCN=m.mes_tag_name
                             };

                if (tbxTagNameSearch != string.Empty)
                {
                    result = result.Where(w => w.TagName.Contains(tbxTagNameSearch));
                }
                return Json(new { total = result.Count(), rows = result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList()});
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        [HttpPost]
        public JsonResult GetListNow()
        {
            try
            {
                var result = from o in db.alarm_origin
                             join t in db.tag_info on o.tag_id equals t.id
                             join m in db.mes_tag on t.tag_name equals m.mes_tag1
                             where o.alarm_value != "NR"
                             select new
                             {
                                 AlarmValue = o.alarm_value,
                                 TagName = t.tag_name,
                                 AlarmTime = o.time,
                                 TagDiscriptionEN = t.tag_discription,
                                 TagDiscriptionCN = m.mes_tag_name
                             };
                return Json(result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Take(10));
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }  
        }

        public JsonResult GetListFrequency()
        {
            try
            {
                var result = (from o in db.alarm_origin
                             join t in db.tag_info on o.tag_id equals t.id
                             join m in db.mes_tag on t.tag_name equals m.mes_tag1
                             where o.alarm_value != "NR"
                             select new
                             {
                                 AlarmValue = o.alarm_value,
                                 TagName = t.tag_name,
                                 AlarmTime = o.time,
                                 TagDiscriptionEN = t.tag_discription,
                                 TagDiscriptionCN = m.mes_tag_name
                             }).AsQueryable();
                var x=from p in result
                      group p by new { p.TagName ,p.AlarmValue} into g
                      select new
                      {
                          g.Key.AlarmValue,
                          g.Key.TagName,
                          count = g.Count()
                      };

                return Json(result.ToList());
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }
    }
}