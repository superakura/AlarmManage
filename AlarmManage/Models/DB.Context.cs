﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class alarm_manage_systemEntities : DbContext
    {
        public alarm_manage_systemEntities()
            : base("name=alarm_manage_systemEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<alarm_info> alarm_info { get; set; }
        public virtual DbSet<alarm_origin> alarm_origin { get; set; }
        public virtual DbSet<alarm_pv_value> alarm_pv_value { get; set; }
        public virtual DbSet<mes_tag> mes_tag { get; set; }
        public virtual DbSet<tag_info> tag_info { get; set; }
        public virtual DbSet<team_time> team_time { get; set; }
        public virtual DbSet<alarm_origin_view> alarm_origin_view { get; set; }
        public virtual DbSet<alarm_pv_info_view> alarm_pv_info_view { get; set; }
        public virtual DbSet<tag_info_view> tag_info_view { get; set; }
    }
}
