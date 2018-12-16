using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class Visit
    {
        public Visit(string url) {
            this.Url = url;
        }
        

        public string Url { get; set; }

        public object Result { get; set; }

        public Exception Exception { get; set; }
    }
}
