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
            this.contentTypes = MineType.MineTypes;
        }
        Dictionary<string, MineType> contentTypes;

        public Type ResponseType { get; private set; }

        Func<string, string> _ResponseHeaderGetter;

        public object Request(AjaxOptions opts) {

            if (opts.Urls != null)
            {
                if (opts.IsMulticast)
                {
                    return this.MulticastWaitResult(opts);
                }
                else
                {
                    Dictionary<string, Exception> exs = new Dictionary<string, Exception>();
                    bool reqSuccess = false;
                    foreach (var url in opts.Urls)
                    {
                        var retObj = this.InternalRequest(opts,(re,o,aurl,me)=> {
                            reqSuccess = true;
                            if (opts.success != null) opts.success(re, o, aurl, me);
                        }, (re, o, aurl, me) => {
                            exs.Add(url,re);
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
                        if (opts.error(ex, opts, null, this)) throw ex;
                        return null;
                    }
                    else throw ex;
                }
            }
            else return this.InternalRequest(opts,opts.success,opts.error);
        }

        public async Task<object> RequestAsync(AjaxOptions opts)
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
                        MulticastUseAsync(opts);
                        return null;
                    }
                }
                else {
                    Dictionary<string, Exception> exs = new Dictionary<string, Exception>();
                    foreach (var url in opts.Urls) {
                        opts.url = url;
                        bool isSuccess = false;
                        var rs = await this.InternalRequestAsync(opts, (ret,o,aurl,me)=> {
                            isSuccess = true;
                            if (opts.success != null) opts.success(ret, o, aurl, me);
                        }, (ret, o, aurl, me) => {
                            exs.Add(url,ret);
                            return true;
                        });
                        
                        if (isSuccess) return rs;
                    }
                    var ex = new MultiUrlException(opts, exs);
                    if (opts.error != null) {
                        if (!opts.error(ex, opts, null, this)) throw ex;
                        return null;
                    }else throw ex;
                }
            }
            else return await this.InternalRequestAsync(opts,opts.success,opts.error);
        }

        void MulticastUseAsync(AjaxOptions opts) {
            var results = new MulticastResults(this, opts);
            int count = 0;
            int total = opts.Urls.Count();
            Action<string,Visit,MulticastResults> req = (url,visit,rs)=>{
                opts.url = url;
                this.InternalRequestAsync(opts, (ret, options, actualUrl, ajax) => {

                    visit.Result = ret;
                    results.AddSuccess(visit);
                    lock(this) count++;
                    if (count == total)
                    {
                        if (results.Errors.Count > 0)
                        {
                            if (opts.error != null)
                            {
                                if (!opts.error(results, opts, null, this)) throw results;
                            }
                            else
                            {
                                throw results;
                            }
                        }
                        else {
                            if (opts.success != null) opts.success(results,opts,null,this);
                        }
                    }
                }, (err, options, actualUrl, ajax) => {

                    visit.Exception = err;
                    results.AddError(visit);
                    lock(this)count++;
                    if (count == total)
                    {
                        if (opts.error != null)
                        {
                            if (!opts.error(results, opts, null, this)) throw results;
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
                var visit = new Visit(url);
                results.AddVisit(visit);
                req(url, visit, results);
            }
        }

        MulticastResults MulticastWaitResult(AjaxOptions opts)
        {
            var results = new MulticastResults(this, opts);
            foreach (var url in opts.Urls)
            {
                var visit = new Visit(opts.url = url);
                results.AddVisit(visit);
                var ret = InternalRequest(opts, (ret1, opt, aurl, me) => {
                    visit.Result = ret1;
                    results.AddSuccess(visit);
                }, (err, opt, aurl, me) => {
                    visit.Exception = err;
                    results.AddError(visit);
                    return true;
                });
            }
            if (results.Errors.Count > 0)
            {
                if (opts.error != null)
                {
                    if (!opts.error(results, opts, null, this)) throw results;
                }
                else
                {
                    throw results;
                }
            }
            else
            {
                if (opts.success != null) opts.success(results, opts, null, this);
            }
            return results;
        }

        async Task<MulticastResults> MulticastWaitResultAsync(AjaxOptions opts) {
            var results = new MulticastResults(this,opts);
            foreach (var url in opts.Urls)
            {
                opts.url = url;
                var visit = new Visit(url);
                results.AddVisit(visit);
                var es = await this.InternalRequestAsync(opts, (ret, option, actualUrl, me) => {
                    visit.Result = ret;
                    results.AddSuccess(visit);
                }, (err, option, actualUrl, me) => {
                    visit.Exception = err;
                    results.AddError(visit);
                    return true;
                });

            }
            if (results.Errors.Count > 0)
            {
                if (opts.error != null)
                {
                    if (!opts.error(results, opts, null, this)) throw results;
                }
                else
                {
                    throw results;
                }
            }
            else
            {
                if (opts.success != null) opts.success(results, opts, null, this);
            }
            return results;
        }

        object InternalRequest(AjaxOptions opts, Action<object, AjaxOptions, string, Ajax> success, Func<Exception, AjaxOptions, string, Ajax,bool> error) {
            
            
            //method
            var method = (opts.method ?? "GET").ToUpper();

            //type
            var type = opts.type;
            MineType requestContentType = null;
            if (!contentTypes.TryGetValue(type, out requestContentType)) {
                requestContentType = MineType.Request;
            }

            var data = requestContentType.Serialize(opts.data);

            var url = opts.url ?? string.Empty;
            if (method == "GET") {
                if (url.Contains("?")) url += "&";
                else url += "?";
            }
            var _webClient = new WebClient();

            string responseText = null;
            try {
                if (method == "POST" || method == "PUT")
                {
                    responseText = _webClient.UploadString(url, data);
                }
                else
                {
                    responseText = _webClient.DownloadString(url);
                }
            } catch (Exception ex) {
                if (error != null)
                {
                    if (!error(ex, opts, url, this)) throw ex;
                }
                else throw ex;
            }
            
            MineType responseContentType = null;
            if (!contentTypes.TryGetValue(opts.dataType, out responseContentType))
            {
                responseContentType = MineType.Response;
            }
            var result =  responseContentType.Deserialize(responseText,this.ResponseType);
            if (success != null) success(result, opts, url, this);
            return result;
        }

        async Task<object> InternalRequestAsync(AjaxOptions opts, Action<object, AjaxOptions, string, Ajax> success, Func<Exception, AjaxOptions, string, Ajax,bool> error)
        {


            //method
            var method = (opts.method ?? "GET").ToUpper();

            //type
            var type = opts.type;
            MineType requestContentType = null;
            if (!contentTypes.TryGetValue(type, out requestContentType))
            {
                requestContentType = MineType.Request;
            }

            var data = requestContentType.Serialize(opts.data);

            var url = opts.url ?? string.Empty;
            if (method == "GET")
            {
                if (url.Contains("?")) url += "&";
                else url += "?";
            }
            
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            

            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(GetMethod(method), url);
                if (opts.headers != null)
                {
                    foreach (var pair in opts.headers)
                    {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
                request.Headers.Add("Content-Type", requestContentType.Value);
                HttpResponseMessage response = null;
                try {
                    response = await client.SendAsync(request);
                } catch (Exception ex) {
                    if (error != null)
                    {
                        if (!error(ex, opts, url, this)) throw ex;
                        return null;
                    }
                    else throw ex;
                    
                }
                

                this._ResponseHeaderGetter = (key) => {
                    var header = response.Content.Headers.GetValues(key);
                    return header?.FirstOrDefault();
                };

                MineType responseContentType = null;
                if (!contentTypes.TryGetValue(opts.dataType, out responseContentType))
                {
                    responseContentType = MineType.Response;
                }

                if (responseContentType.ResponseKind == MineTypeKinds.Bytes)
                {
                    var result = await response.Content.ReadAsByteArrayAsync();
                    if (success != null) success(result, opts, url, this);

                    return result;
                }
                else if (responseContentType.ResponseKind == MineTypeKinds.Stream)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (responseContentType.ResponseAsync)
                        {
                            var result = await responseContentType.DeserializeAsync(stream);
                            if (success != null) success(result, opts, url, this);
                            return result;
                        }
                        else {
                            var result = responseContentType.Deserialize(stream,this.ResponseType);
                            if (success != null) success(result, opts, url, this);
                            return result;
                        }
                        
                    }
                }
                else {
                    
                    var result=  responseContentType.Deserialize(await response.Content.ReadAsStringAsync(),null);
                    if (success != null) success(result, opts, url, this);
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
