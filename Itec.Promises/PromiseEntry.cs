using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class PromiseEntry
    {
        
        public Guid Id { get; set; }
        public Visit Visit { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime LastRequestTime { get; set; }

        public int RequestCount { get; set; }

        public int RetryInternvals { get; set; }

        public bool IsSuccess { get; set; }

    }
}
