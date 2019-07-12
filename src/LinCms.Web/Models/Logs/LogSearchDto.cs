﻿using System;
using LinCms.Web.Data;
using LinCms.Zero.Data;

namespace LinCms.Web.Models.Logs
{
    public class LogSearchDto : PageDto
    {
        public string Keyword { get; set; }
        /// <summary>
        /// 用户昵称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 起始时间
        /// </summary>
        public DateTime? Start { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? End { get; set; }
    }
}
