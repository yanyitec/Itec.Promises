using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class PromiseEntity
    {
        public PromiseEntity() { }
        public PromiseEntity(Visit visit) { }
        public Guid Id { get; set; }
        public string Category { get; set; }

        public string BusinessId { get; set; }

        public string Url { get; set; }

        public string Method { get; set; }

        public string Headers { get; set; }

        public string Content { get; set; }

        public string ResponseText { get; set; }

        public string Exception { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime LastRequestTime { get; set; }

        public int RequestCount { get; set; }

        public int RetryInternvals { get; set; }

        public bool IsSuccess { get; set; }

        public Visit ToVisit() { return null; }

        public static PromiseEntity FromVisit(Visit visit,PromiseEntity entity=null) {
            return null;
        }

    }
}
