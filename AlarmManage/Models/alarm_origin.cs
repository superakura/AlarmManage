//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace AlarmManage.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class alarm_origin
    {
        public int id { get; set; }
        public Nullable<int> tag_id { get; set; }
        public string alarm_value { get; set; }
        public Nullable<System.DateTime> time { get; set; }
        public Nullable<int> state { get; set; }
        public Nullable<System.DateTime> start_time { get; set; }
        public Nullable<System.DateTime> end_time { get; set; }
    }
}
