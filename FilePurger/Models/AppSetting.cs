using System;
using System.Collections.Generic;
using System.Text;
using DotNetHelper_Application.Interface;
using DotNetHelper_Contracts.Attribute;

namespace FilePurger
{
    public class AppSetting : IConfiguration
    { 
        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;
        [SqlColumnAttritube(SetPrimaryKey = true,SetNullable = false)]
     
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
