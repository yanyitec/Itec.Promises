using System;
using System.Collections.Generic;
using System.Text;

namespace Itec.Promises
{
    public class AjaxOptions
    {
        public string url;
        public string Url {
            get { return url; }
            set { url = value; }
        }

        public IEnumerable<string> Urls { get; set; }

        public bool IsMulticast { get; set; }

        public bool IsMulticastWaitable { get; set; }

        public Action<object, Visit> success;
        /// <summary>
        /// 只接受连接错误的异常，其他异常由Ajax对象自己丢出
        /// </summary>

        public Func<Exception, Visit,bool> error;

        public string method;
        public string Method {
            get { return method; }
            set { method = value; }
        }
        public IDictionary<string, string> headers;
        public IDictionary<string, string> Headers
        {
            get { return headers; }
            set { headers = value; }
        }

        public string type;

        public string Type {
            get { return type; }
            set { type = value; }
        }

        public string dataType;
        public string DataType {
            get { return dataType; }
            set { dataType = value; }
        }

        public object data;
        public object Data {
            get { return data; }
            set { data = value; }
        }
    }
}
