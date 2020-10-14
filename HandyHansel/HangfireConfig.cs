using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel
{
    public class HangfireConfig
    {
        public static readonly string Section = "HangfireConfig";
        public DatabaseConfig Database { get; set; }
    }
}
