using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class Visit
    {
        public Visit(AjaxOptions opts,Ajax ajax) {
            this.Url = opts.url;
            this.Options = opts;
            this.AjaxObject = ajax;
        }

        public Ajax AjaxObject { get; set; }

        public AjaxOptions Options { get; private set; }
        

        public string Url { get; private set; }

        public string RequestUrl { get; set; }

        public string Method { get; set; }

        public Dictionary<string, string> RequstHeaders { get; set; }

        public byte[] Bytes { get; set; }

        public string Content { get; set; }

        public object Result { get; set; }

        public Exception Exception { get; set; }
    }
}
