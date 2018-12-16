using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Itec.Promises
{
    public class Ajax
    {
        public Ajax(Type responseType) {
            this.ResponseType = responseType;
            //this.contentTypes = MineType.MineTypes;
        }
        //Dictionary<string, MineType> contentTypes;

        public Type ResponseType { get; private set; }

        Func<string, string> _ResponseHeaderGetter;

        public static object Request(AjaxOptions opts,Ajax ajax=null) {

            if (opts.Urls != null)
            {
                if (opts.IsMulticast)
                {
                    return MulticastWaitResult(opts);
                }
                else
                {
                    Dictionary<string, Exception> exs = new Dictionary<string, Exception>();
                    bool reqSuccess = false;
                    foreach (var url in opts.Urls)
                    {
                        opts.url = url;
                        var visit = MakeVisitFromOptions(opts,ajax);
                        var retObj = InternalRequest(visit, (re, v) =>
                        {
                            reqSuccess = true;
                            if (opts.success != null) opts.success(re,v);
                        }, (re, v) =>
                        {
                            exs.Add(url, re);
                            return true;
                        });
                        if (reqSuccess)
                        {

                            return retObj;
                        }
                    }
                    var ex = new MultiUrlException(opts, exs);
                    if (opts.error != null)
                    {
                        if (opts.error(ex, new Visit(opts,ajax))) throw ex;
                        return null;
                    }
                    else throw ex;
                }
            }
            else
            {

                return InternalRequest(MakeVisitFromOptions(opts,ajax), opts.success, opts.error);
            }
        }

        public static async Task<object> RequestAsync(AjaxOptions opts,Ajax ajax=null)
        {
            if (opts.Urls != null)
            {
                if (opts.IsMulticast)
                {
                    if (opts.IsMulticastWaitable)
                    {
                        return await MulticastWaitResultAsync(opts);
                    }
                    else
                    {
                        MulticastUseAsync(opts,ajax);
                        return null;
                    }
                }
                else {
                    Dictionary<string, Exception> exs = new Dictionary<string, Exception>();
                    foreach (var url in opts.Urls) {
                        opts.url = url;
                        var visit = MakeVisitFromOptions(opts,ajax);
                        bool isSuccess = false;
                        var rs = await InternalRequestAsync(visit, (ret,v)=> {
                            isSuccess = true;
                            if (opts.success != null) opts.success(ret, v);
                        }, (ret, v) => {
                            exs.Add(url,ret);
                            return true;
                        });
                        
                        if (isSuccess) return rs;
                    }
                    var ex = new MultiUrlException(opts, exs);
                    if (opts.error != null) {
                        if (!opts.error(ex, new Visit(opts,ajax))) throw ex;
                        return null;
                    }else throw ex;
                }
            }
            else return await InternalRequestAsync(MakeVisitFromOptions(opts,ajax),opts.success,opts.error);
        }

        static void MulticastUseAsync(AjaxOptions opts,Ajax ajax) {
            var results = new MulticastResults(ajax, opts);
            int count = 0;
            int total = opts.Urls.Count();
            Action<string,Visit,MulticastResults> req = (url,visit,rs)=>{
                opts.url = url;
                InternalRequestAsync(visit, (ret, v) => {

                    v.Result = ret;
                    results.AddSuccess(v);
                    lock(results) count++;
                    if (count == total)
                    {
                        if (results.Errors.Count > 0)
                        {
                            if (opts.error != null)
                            {
                                if (!opts.error(results, v)) throw results;
                            }
                            else
                            {
                                throw results;
                            }
                        }
                        else {
                            if (opts.success != null) opts.success(results,v);
                        }
                    }
                }, (err, v) => {

                    visit.Exception = err;
                    results.AddError(visit);
                    lock(results) count++;
                    if (count == total)
                    {
                        if (opts.error != null)
                        {
                            if (!opts.error(results, v)) throw results;
                        }
                        else
                        {
                            throw results;
                        }
                    }
                    return true;
                }).Start();
            };
            foreach (var url in opts.Urls) {
                opts.url = url;
                var visit = MakeVisitFromOptions(opts,ajax);
                results.AddVisit(visit);
                req(url, visit, results);
            }
        }

        static MulticastResults MulticastWaitResult(AjaxOptions opts,Ajax ajax=null)
        {
            var results = new MulticastResults(ajax, opts);
            foreach (var url in opts.Urls)
            {
                opts.url = url;
                var visit = MakeVisitFromOptions(opts,ajax);
                results.AddVisit(visit);
                var ret = InternalRequest(visit, (ret1, v) => {
                    visit.Result = ret1;
                    results.AddSuccess(visit);
                }, (err, v) => {
                    visit.Exception = err;
                    results.AddError(visit);
                    return true;
                });
            }
            if (results.Errors.Count > 0)
            {
                if (opts.error != null)
                {
                    if (!opts.error(results, new Visit(opts,ajax))) throw results;
                }
                else
                {
                    throw results;
                }
            }
            else
            {
                if (opts.success != null) opts.success(results, new Visit(opts,ajax));
            }
            return results;
        }

        static async Task<MulticastResults> MulticastWaitResultAsync(AjaxOptions opts,Ajax ajax= null) {
            var results = new MulticastResults(ajax,opts);
            foreach (var url in opts.Urls)
            {
                opts.url = url;
                var visit = MakeVisitFromOptions(opts,ajax);
                results.AddVisit(visit);
                var es = await InternalRequestAsync(visit, (ret, v) => {
                    visit.Result = ret;
                    results.AddSuccess(visit);
                }, (err, v) => {
                    visit.Exception = err;
                    results.AddError(visit);
                    return true;
                });

            }
            if (results.Errors.Count > 0)
            {
                if (opts.error != null)
                {
                    if (!opts.error(results, new Visit(opts,ajax))) throw results;
                }
                else
                {
                    throw results;
                }
            }
            else
            {
                if (opts.success != null) opts.success(results, new Visit(opts,ajax));
            }
            return results;
        }

        static Visit MakeVisitFromOptions(AjaxOptions opts,Ajax ajax) {
            var visit = new Visit(opts,ajax);
            
            var method = visit.Method = (opts.method ?? "GET").ToUpper();
            
            var type = opts.type;
            MineType requestContentType = null;
            if (!MineType.MineTypes.TryGetValue(type, out requestContentType))
            {
                requestContentType = MineType.Request;
            }
            visit.Content = requestContentType.Serialize(opts.data);

            var url = opts.url ?? string.Empty;
            if (method == "GET")
            {
                if (url.Contains("?")) url += "&";
                else url += "?";
            }
            visit.RequestUrl = url;
            return visit;
        }

        internal static object InternalRequest(Visit visit, Action<object, Visit> success, Func<Exception, Visit,bool> error) {
            
            var _webClient = new WebClient();

            string responseText = null;
            try {
                if (visit.Method == "POST" || visit.Method == "PUT")
                {
                    responseText = _webClient.UploadString(visit.Method, visit.Content);
                }
                else
                {
                    responseText = _webClient.DownloadString(visit.Url);
                }
            } catch (Exception ex) {
                if (error != null)
                {
                    if (!error(ex, visit)) throw ex;
                }
                else throw ex;
            }
            
            MineType responseContentType = null;
            var opts = visit.Options;
            if (!MineType.MineTypes.TryGetValue(opts.dataType, out responseContentType))
            {
                responseContentType = MineType.Response;
            }
            var result =visit.Result=  responseContentType.Deserialize(responseText,visit.AjaxObject.ResponseType);
            if (success != null) success(result, visit);
            return result;
        }

        static internal async Task<object> InternalRequestAsync(Visit visit,Action<object, Visit> success, Func<Exception, Visit,bool> error)
        {

            var opts = visit.Options;
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            

            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(GetMethod(visit.Method), visit.RequestUrl);
                if (visit.RequstHeaders != null)
                {
                    foreach (var pair in visit.RequstHeaders)
                    {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
                request.Content = new StringContent(visit.Content);
                //request.Headers.Add("Content-Type", requestContentType.Value);
                HttpResponseMessage response = null;
                try {
                    response = await client.SendAsync(request);
                } catch (Exception ex) {
                    if (error != null)
                    {
                        if (!error(ex, visit)) throw ex;
                        return null;
                    }
                    else throw ex;
                    
                }
                

                visit.AjaxObject._ResponseHeaderGetter = (key) => {
                    var header = response.Content.Headers.GetValues(key);
                    return header?.FirstOrDefault();
                };

                MineType responseContentType = null;
                if (!MineType.MineTypes.TryGetValue(opts.dataType, out responseContentType))
                {
                    responseContentType = MineType.Response;
                }

                if (responseContentType.ResponseKind == MineTypeKinds.Bytes)
                {
                    var result = visit.Result = await response.Content.ReadAsByteArrayAsync();
                    if (success != null) success(result, visit);

                    return result;
                }
                else if (responseContentType.ResponseKind == MineTypeKinds.Stream)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (responseContentType.ResponseAsync)
                        {
                            var result =visit.Result = await responseContentType.DeserializeAsync(stream);
                            if (success != null) success(result, visit);
                            return result;
                        }
                        else {
                            var result = visit.Result = responseContentType.Deserialize(stream,visit.AjaxObject.ResponseType);
                            if (success != null) success(result, visit);
                            return result;
                        }
                        
                    }
                }
                else {
                    
                    var result=visit.Result=  responseContentType.Deserialize(await response.Content.ReadAsStringAsync(),null);
                    if (success != null) success(result, visit);
                    return result;
                }
            }
            
            
        }

        static HttpMethod GetMethod(string method) {
            switch (method)
            {
                case "GET":
                    return HttpMethod.Get;
                case "POST":
                    return HttpMethod.Post;
                case "PUT":
                    return HttpMethod.Put;
                case "DELETE":
                    return HttpMethod.Delete;
                case "OPTIONS":
                    return HttpMethod.Options;
                case "HEAD":
                    return HttpMethod.Head;
                case "TRACE":
                    return HttpMethod.Trace;
            }
            return HttpMethod.Get;
        }


        public string GetResponseHeader(string key) {
            //return _webClient.ResponseHeaders[key];
            return null;
        }
        
    }
}
