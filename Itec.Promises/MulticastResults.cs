using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class MulticastResults:Exception
    {
        public MulticastResults(Ajax ajaxObj,AjaxOptions opts) {
            this.AjaxObject = ajaxObj;
            this.Options = opts;
            _Visited = new List<Visit>();
            _Successes = new List<Visit>();
        }

        public Ajax AjaxObject { get; private set; }

        public AjaxOptions Options { get;private set; }

        List<Visit> _Visited;
        public IReadOnlyList<Visit> Visited {
            get { return _Visited; }
        }

        internal void AddVisit(Visit visit) {
            lock (this) {
                _Visited.Add(visit);
            }
        }
        List<Visit> _Errors;
        public IReadOnlyList<Visit> Errors {
            get
            {
                if (_Errors == null)
                {
                    lock (this)
                    {
                        _Errors = new List<Visit>();
                    }
                }
                return _Errors;
            }
        }
        internal void AddError(Visit visit)
        {
            lock (this)
            {
                _Errors.Add(visit);
            }
        }

        List<Visit> _Successes;
        public IReadOnlyList<Visit> Successes { get { return _Successes; } }

        internal void AddSuccess(Visit visit)
        {
            lock (this)
            {
                _Successes.Add(visit);
            }
        }
    }
}
