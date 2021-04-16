﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YOSHI.CommunityData.MetricData
{
    /// <summary>
    /// This class is used to store values for metrics used to compute whether a community exhibits a structure or not.
    /// </summary>
    public class Structure
    {
        public bool CommonProjects { get; set; }
        public bool PullReqInteraction { get; set; }
        public bool Followers { get; set; }
    }
}
