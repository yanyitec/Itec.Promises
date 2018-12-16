using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class MultiUrlException:Exception
    {
        public MultiUrlException(AjaxOptions opts,Dictionary<string, Exception> exs) {
            this.Errors = exs;
            this.Options = opts;
        }
        public AjaxOptions Options { get; private set; }

        public IReadOnlyDictionary<string, Exception> Errors { get; private set; }
    }
}
