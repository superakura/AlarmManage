using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Aspose.Cells;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data.Entity.SqlServer;

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

        public ViewResult ListHistory()
        {
            return View();
        }

        public ViewResult ListRate()
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
        /// 根据单位名称，返回单位ID
        /// </summary>
        /// <param name="deptName"></param>
        /// <returns></returns>
        public int GetDeptID(string deptName)
        {
            var deptID = 0;
            switch (deptName)
            {
                case "一套聚丙烯":
                    deptID = 1;
                    break;
                case "二套聚丙烯":
                    deptID = 2;
                    break;
            }
            return deptID;
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
                var deptName = Request.Form["deptName"];
                var deptID = GetDeptID(deptName);
                
                var result = from o in db.alarm_origin_view
                                 //where o.alarm_value != "NR"&&o.dept_id==deptID
                             where (new string[] { "LL", "HH", "LO", "HI" }).Contains(o.alarm_value) && o.dept_id == deptID
                             select new
                             {
                                 DeptID=o.dept_id,
                                 AlarmValue = o.alarm_value,
                                 TagName = o.tag_name,
                                 AlarmTime = o.time,
                                 AlarmTimeEnd = o.end_time,
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
                var deptName = Request.Form["deptName"];//装置信息

                var result = GetHistoryIQueryable(tbxTagNameSearch,tbxAlarmTimeBeginSearch,tbxAlarmTimeEndSearch,ddlTeamSearch,deptName);

                //选择班组
                if (ddlTeamSearch != string.Empty)
                {
                    //日期范围不能为空
                    if (string.IsNullOrEmpty(tbxAlarmTimeBeginSearch) || string.IsNullOrEmpty(tbxAlarmTimeEndSearch))
                    {
                        return Json("ErrorDateRange");
                    }

                    //获取日期范围列表
                    var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
                    var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);
                    var dateRange = GetAllDays(dateStart, dateEnd);

                    //如果日期范围大于31天，不能查询,返回错误信息。
                    if (dateRange.Length > 31)
                    {
                        return Json("ErrorDateRange");
                    }

                    var listDateRange = GetHistoryTeamList(dateRange, result, ddlTeamSearch,deptName);

                    return Json(new {
                        total = listDateRange.Count(),
                        rows = listDateRange.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList()
                    });
                }

                return Json(new {
                    total = result.Count(),
                    rows = result.OrderByDescending(o => o.AlarmTime).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList()
                });
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        /// <summary>
        /// 获取历史报警信息方法
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="alarmTimeBegin"></param>
        /// <param name="alarmTimeEnd"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public IQueryable<Models.ViewAlarm> GetHistoryIQueryable(string tagName, string alarmTimeBegin, string alarmTimeEnd,string team,string deptName)
        {
            var deptID = GetDeptID(deptName);

            var result = from o in db.alarm_origin_view
                         where (new string[] { "LL", "HH", "LO", "HI" }).Contains(o.alarm_value) && o.dept_id==deptID
                         select new Models.ViewAlarm
                         {
                             DeptID = o.dept_id,
                             AlarmValue = o.alarm_value,
                             TagName = o.tag_name,
                             AlarmTime = o.time,
                             AlarmTimeEnd = o.end_time,
                             HH = o.HH,
                             LL = o.LL,
                             PH = o.PH,
                             PL = o.PL,
                             PV = o.PV,
                             TagLevel = o.tag_level,
                             Type = o.type,
                             TagDiscriptionEN = o.tag_discription,
                             TagDiscriptionCN = o.mes_tag_name
                         };

            //按位号名称精确查询
            if (tagName != string.Empty)
            {
                result = result.Where(w => w.TagName == (tagName + ".ALRM"));
            }

            //选择日期范围
            if (!string.IsNullOrEmpty(alarmTimeBegin) & !string.IsNullOrEmpty(alarmTimeEnd) & team == string.Empty)
            {
                var dateStart = Convert.ToDateTime(alarmTimeBegin);
                var dateEnd = Convert.ToDateTime(alarmTimeEnd);
                result = result.Where(w => DbFunctions.DiffSeconds(w.AlarmTime, dateStart) <= 0 && DbFunctions.DiffSeconds(w.AlarmTime, dateEnd) >= 0);
            }

            return result;
        }

        /// <summary>
        /// 获取按班组查询历史报警信息方法
        /// </summary>
        /// <param name="dateRange"></param>
        /// <param name="result"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public List<Models.ViewAlarm> GetHistoryTeamList(DateTime[] dateRange, IQueryable<Models.ViewAlarm> result, string team, string deptName)
        {
            var listDateRange = new List<Models.ViewAlarm>();

            //获取装置班组排班信息列表
            var teamTimeList = db.team_time.Where(w => w.TeamName == team && w.DeviceName ==deptName).ToList();

            for (int i = 0; i < dateRange.Length; i++)
            {
                foreach (var item in teamTimeList)
                {
                    if (dateRange[i].DayOfWeek.ToString() == item.WeekName)
                    {
                        var dateStartTime = Convert.ToDateTime(dateRange[i].ToShortDateString() + " " + item.TimeStart);
                        var dateEndTime = Convert.ToDateTime(dateRange[i].ToShortDateString() + " " + item.TimeEnd);
                        var listTeam = result.Where(w => DbFunctions.DiffSeconds(w.AlarmTime, dateStartTime) <= 0 && DbFunctions.DiffSeconds(w.AlarmTime, dateEndTime) >= 0).ToList();
                        listDateRange.AddRange(listTeam);
                    }
                }
            }
            return listDateRange;
        }

        /// <summary>
        /// 频次统计列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
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
                var deptName = Request.Form["deptName"];//装置信息

                var result = GetHistoryIQueryable(tbxTagNameSearch, tbxAlarmTimeBeginSearch, tbxAlarmTimeEndSearch, ddlTeamSearch, deptName);

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

                    //var listDateRange = new List<Models.ViewAlarm>();

                    //获取日期范围列表
                    var dateRange = GetAllDays(dateStart, dateEnd);

                    //如果日期范围大于31天，不能查询,返回错误信息。
                    if (dateRange.Length > 31)
                    {
                        return Json("ErrorDateRange");
                    }

                    var listDateRange = GetHistoryTeamList(dateRange, result, ddlTeamSearch, deptName);

                    var frequencyTeam = from p in listDateRange
                                        group p by new { p.TagName, p.AlarmValue, p.TagDiscriptionCN, p.TagDiscriptionEN, p.Type, p.TagLevel } into g
                                        select new
                                        {
                                            AlarmValue = g.Key.AlarmValue,
                                            TagName = g.Key.TagName,
                                            Type = g.Key.Type,
                                            TagLevel = g.Key.TagLevel,
                                            TagDiscriptionCN = g.Key.TagDiscriptionCN,
                                            TagDiscriptionEN = g.Key.TagDiscriptionEN,
                                            AlarmFrequency = g.Count()
                                        };

                    return Json(new
                    {
                        total = frequencyTeam.Count(),
                        rows = frequencyTeam.OrderByDescending(o => o.AlarmFrequency).ThenBy(t => t.TagName).Skip(offset).Take(limit).ToList()
                    });
                }

                var frequency = from p in result
                                group p by new { p.TagName, p.AlarmValue, p.TagDiscriptionCN, p.TagDiscriptionEN, p.Type, p.TagLevel } into g
                                select new
                                {
                                    AlarmValue = g.Key.AlarmValue,
                                    TagName = g.Key.TagName,
                                    Type = g.Key.Type,
                                    TagLevel = g.Key.TagLevel,
                                    TagDiscriptionCN = g.Key.TagDiscriptionCN,
                                    TagDiscriptionEN = g.Key.TagDiscriptionEN,
                                    AlarmFrequency = g.Count()
                                };

                return Json(new
                {
                    total = frequency.Count(),
                    rows = frequency.OrderByDescending(o => o.AlarmFrequency).ThenByDescending(t => t.TagName).Skip(offset).Take(limit).ToList()
                });
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        /// <summary>
        /// 报警率计算列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
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
                var deptName = Request.Form["deptName"];//装置信息

                var result = GetRateIQueryable(tbxTagNameSearch, tbxAlarmTimeBeginSearch, tbxAlarmTimeEndSearch, deptName);

                var count = result.Count();

                //计算所有位号的报警时间总和
                var timeCount = result.Sum(s => s.TimeFrequency);

                //计算时间范围内的总秒数
                var secondRange = ExecDateDiff(Convert.ToDateTime(tbxAlarmTimeBeginSearch), Convert.ToDateTime(tbxAlarmTimeEndSearch));
                var rateAll =timeCount/ (count * secondRange);

                return Json(new
                {
                    total = count,
                    rows = result.OrderByDescending(o => o.Rate).Skip(offset).Take(limit).ToList(),
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        /// <summary>
        /// 报警率计算方法
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <param name="tagName"></param>
        /// <param name="tbxAlarmTimeBeginSearch"></param>
        /// <param name="alarmTimeEnd"></param>
        /// <returns></returns>
        public IQueryable<Models.ViewRate> GetRateIQueryable(string tagName, string alarmTimeBegin, string alarmTimeEnd,string deptName)
        {
            var dateStart = Convert.ToDateTime(alarmTimeBegin);
            var dateEnd = Convert.ToDateTime(alarmTimeEnd);
            var deptID = GetDeptID(deptName);

            //计算时间范围内的总秒数
            var secondRange = ExecDateDiff(dateStart, dateEnd);

            var frequency = from p in db.alarm_origin_view
                            where (new string[] { "LL", "HH", "LO", "HI" }).Contains(p.alarm_value) && (DbFunctions.DiffSeconds(p.time, dateStart) <= 0 && DbFunctions.DiffSeconds(p.time, dateEnd) >= 0) && p.dept_id == deptID
                            group p by new { p.tag_name, p.mes_tag_name, p.tag_discription, p.type, p.tag_level } into g
                            select new Models.ViewRate
                            {
                                TagName = g.Key.tag_name,
                                Type = g.Key.type,
                                TagLevel = g.Key.tag_level,
                                TagDiscriptionCN = g.Key.mes_tag_name,
                                TagDiscriptionEN = g.Key.tag_discription,
                                TimeFrequency = g.Sum(s => DbFunctions.DiffSeconds(s.end_time, dateEnd) <= 0 ? DbFunctions.DiffSeconds(s.start_time, dateEnd) : DbFunctions.DiffSeconds(s.start_time, s.end_time)),
                                Rate = g.Sum(s => DbFunctions.DiffSeconds(s.end_time, dateEnd) <= 0 ? DbFunctions.DiffSeconds(s.start_time, dateEnd) : DbFunctions.DiffSeconds(s.start_time, s.end_time)) / secondRange,
                                Range = secondRange
                            };
            if (tagName != string.Empty)
            {
                frequency = frequency.Where(w => w.TagName == (tagName + ".ALRM"));
            }
            return frequency;
        }

        /// <summary>
        /// 获取总报警率
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetRateAll()
        {
            try
            {
                var tbxTagNameSearch = Request.Form["tbxTagNameSearch"];//位号
                var tbxAlarmTimeBeginSearch = Request.Form["tbxAlarmTimeBeginSearch"];//报警时间范围开始
                var tbxAlarmTimeEndSearch = Request.Form["tbxAlarmTimeEndSearch"];//报警时间范围结束
                var deptName = Request.Form["deptName"];//装置信息

                var result = GetRateIQueryable(tbxTagNameSearch, tbxAlarmTimeBeginSearch, tbxAlarmTimeEndSearch, deptName);

                var count = result.Count(c=>c.TimeFrequency>0);

                //计算所有位号的报警时间总和
                var timeCount = result.Sum(s => s.TimeFrequency);

                //计算时间范围内的总秒数
                var secondRange = ExecDateDiff(Convert.ToDateTime(tbxAlarmTimeBeginSearch), Convert.ToDateTime(tbxAlarmTimeEndSearch));
                var rateAll = timeCount / (count * secondRange);
                return Json(new { rateAll=rateAll, timeCount = timeCount , allCount= count * secondRange });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        /// <summary>
        /// 导出报警率Excel
        /// </summary>
        /// <returns></returns> 
        public FileResult ToExcelRate()
        {
            var tbxTagNameSearch = Request.QueryString["tbxTagNameSearch"];//位号
            var tbxAlarmTimeBeginSearch = Request.QueryString["tbxAlarmTimeBeginSearch"];//报警时间范围开始
            var tbxAlarmTimeEndSearch = Request.QueryString["tbxAlarmTimeEndSearch"];//报警时间范围结束
            var deptName = Request.QueryString["deptName"];//装置信息

            var result = GetRateIQueryable(tbxTagNameSearch, tbxAlarmTimeBeginSearch, tbxAlarmTimeEndSearch, deptName)
                .OrderByDescending(o => o.Rate).ToList();

            var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
            var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);
            var filename = deptName+"-报警率信息" + dateStart.ToString("yyyyMMdd") + "-" + dateEnd.ToString("yyyyMMdd");

            string path = System.IO.Path.Combine(Server.MapPath("/"), "Template/ExportRate.xls");
            Workbook workbook = new Workbook();
            workbook.Open(path);
            Cells cells = workbook.Worksheets[0].Cells;
            Worksheet ws = workbook.Worksheets[0];

            #region 表格样式
            StyleFlag sf = new StyleFlag();
            sf.HorizontalAlignment = true;
            sf.VerticalAlignment = true;
            sf.WrapText = true;
            sf.Borders = true;

            Style style1 = workbook.Styles[workbook.Styles.Add()];//新增样式  
            style1.HorizontalAlignment = TextAlignmentType.Center;//文字居中 
            style1.VerticalAlignment = TextAlignmentType.Center;
            style1.IsTextWrapped = true;//单元格内容自动换行  
            style1.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin; //应用边界线 左边界线  
            style1.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin; //应用边界线 右边界线  
            style1.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin; //应用边界线 上边界线  
            style1.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin; //应用边界线 下边界线  
            #endregion

            var row = 1;
            foreach (var info in result)
            {
                cells[row, 0].PutValue(info.TagName.Substring(0, (info.TagName).Length - 5));
                cells[row, 0].SetStyle(style1);
                cells[row, 1].PutValue(info.Type);
                cells[row, 1].SetStyle(style1);
                cells[row, 2].PutValue(info.TimeFrequency);
                cells[row, 2].SetStyle(style1);
                cells[row, 3].PutValue(info.Range);
                cells[row, 3].SetStyle(style1);
                cells[row, 4].PutValue(info.Rate * 100);
                cells[row, 4].SetStyle(style1);
                cells[row, 5].PutValue(info.TagDiscriptionCN);
                cells[row, 5].SetStyle(style1);
                cells[row, 6].PutValue(info.TagDiscriptionEN);
                cells[row, 6].SetStyle(style1);
                row++;
            }

            string fileToSave = System.IO.Path.Combine(Server.MapPath("/"), "ExcelOutPut/" + filename + ".xls");
            if (System.IO.File.Exists(fileToSave))
            {
                System.IO.File.Delete(fileToSave);
            }
            workbook.Save(fileToSave, FileFormatType.Excel97To2003);
            return File(fileToSave, "application/vnd.ms-excel", filename + ".xls");
        }

        /// <summary>
        /// 导出报警历史信息Excel
        /// </summary>
        /// <returns></returns>
        public FileResult ToExcelHistory()
        {
            var tbxTagNameSearch = Request.QueryString["tbxTagNameSearch"];//位号
            var tbxAlarmTimeBeginSearch = Request.QueryString["tbxAlarmTimeBeginSearch"];//报警时间范围开始
            var tbxAlarmTimeEndSearch = Request.QueryString["tbxAlarmTimeEndSearch"];//报警时间范围结束
            var deptName = Request.QueryString["deptName"];//装置信息
            var ddlTeamSearch = Request.QueryString["ddlTeamSearch"];//班组信息

            var resultQueryable = GetHistoryIQueryable(tbxTagNameSearch, tbxAlarmTimeBeginSearch, tbxAlarmTimeEndSearch, ddlTeamSearch, deptName);

            var excelHistory = new List<Models.ViewAlarm>();

            //选择班组
            if (ddlTeamSearch != string.Empty)
            {
                //获取日期范围列表
                var dateRange = GetAllDays(Convert.ToDateTime(tbxAlarmTimeBeginSearch), Convert.ToDateTime(tbxAlarmTimeEndSearch));
                excelHistory = GetHistoryTeamList(dateRange, resultQueryable, ddlTeamSearch, deptName);
            }
            else
            {
                excelHistory = resultQueryable.ToList();
            }

            var frequencyTeam = from p in excelHistory
                                group p by new { p.TagName, p.AlarmValue, p.TagDiscriptionCN, p.TagDiscriptionEN, p.Type, p.TagLevel } into g
                                select new
                                {
                                    AlarmValue = g.Key.AlarmValue,
                                    TagName = g.Key.TagName,
                                    Type = g.Key.Type,
                                    TagLevel = g.Key.TagLevel,
                                    TagDiscriptionCN = g.Key.TagDiscriptionCN,
                                    TagDiscriptionEN = g.Key.TagDiscriptionEN,
                                    AlarmFrequency = g.Count()
                                };

            var dateStart = Convert.ToDateTime(tbxAlarmTimeBeginSearch);
            var dateEnd = Convert.ToDateTime(tbxAlarmTimeEndSearch);
            var filename = deptName+ "-报警历史信息" + dateStart.ToString("yyyyMMdd") + "-" + dateEnd.ToString("yyyyMMdd");

            string path = System.IO.Path.Combine(Server.MapPath("/"), "Template/ExportHistory.xls");
            Workbook workbook = new Workbook();
            workbook.Open(path);
            Cells cells0 = workbook.Worksheets[0].Cells;
            Cells cells1 = workbook.Worksheets[1].Cells;

            var styleExcel = GetAsposeStyle(workbook);//表格样式

            var rowCells0 = 1;
            foreach (var info in excelHistory)
            {
                cells0[rowCells0, 0].PutValue(info.TagName.Substring(0, (info.TagName).Length - 5));
                cells0[rowCells0, 0].SetStyle(styleExcel);
                cells0[rowCells0, 1].PutValue(info.Type);
                cells0[rowCells0, 1].SetStyle(styleExcel);
                cells0[rowCells0, 2].PutValue(info.AlarmValue);
                cells0[rowCells0, 2].SetStyle(styleExcel);
                cells0[rowCells0, 3].PutValue(info.PV);
                cells0[rowCells0, 3].SetStyle(styleExcel);
                rowCells0++;
            }

            var rowCells1 = 1;
            foreach (var info in frequencyTeam)
            {
                cells1[rowCells1, 0].PutValue(info.TagName.Substring(0, (info.TagName).Length - 5));
                cells1[rowCells1, 0].SetStyle(styleExcel);
                cells1[rowCells1, 1].PutValue(info.Type);
                cells1[rowCells1, 1].SetStyle(styleExcel);
                cells1[rowCells1, 2].PutValue(info.AlarmValue);
                cells1[rowCells1, 2].SetStyle(styleExcel);
                cells1[rowCells1, 3].PutValue(info.AlarmFrequency);
                cells1[rowCells1, 3].SetStyle(styleExcel);
                rowCells1++;
            }

            string fileToSave = System.IO.Path.Combine(Server.MapPath("/"), "ExcelOutPut/" + filename + ".xls");
            if (System.IO.File.Exists(fileToSave))
            {
                System.IO.File.Delete(fileToSave);
            }
            workbook.Save(fileToSave, FileFormatType.Excel97To2003);
            return File(fileToSave, "application/vnd.ms-excel", filename + ".xls");
        }

        /// <summary>
        /// 设置Aspose Excel表格样式
        /// </summary>
        /// <param name="workbook"></param>
        /// <returns></returns>
        public Style GetAsposeStyle(Workbook workbook)
        {
            Style style1 = workbook.Styles[workbook.Styles.Add()];//新增样式  
            style1.HorizontalAlignment = TextAlignmentType.Center;//文字居中 
            style1.VerticalAlignment = TextAlignmentType.Center;
            style1.IsTextWrapped = true;//单元格内容自动换行  
            style1.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin; //应用边界线 左边界线  
            style1.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin; //应用边界线 右边界线  
            style1.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin; //应用边界线 上边界线  
            style1.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin; //应用边界线 下边界线  
            return style1;
        }
    }
}