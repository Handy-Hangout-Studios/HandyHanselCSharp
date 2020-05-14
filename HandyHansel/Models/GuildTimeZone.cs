﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HandyHansel.Models
{
    public class GuildTimeZone
    {
        [Key]
        [Column("id")]
        public int Id { get; set;  }

        [Column("guild")]
        public string Guild { get; set; }
        
        [Column("timezone_id")]
        public string TimeZoneId { get; set; }

        [Column("operating_system")]
        public string OperatingSystem { get; set; }

    }
}