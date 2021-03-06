﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TargetHubApi.Models
{
    public class TargetRequestType
    {
        public int Id { get; set; }
        public string Type { get; set; }

        public virtual ICollection<TargetRequest> TargetRequests { get; set; }
    }
}