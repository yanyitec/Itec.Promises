using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Itec.Promises
{
    public class Promise
    {
        public Promise(IPromiseStore promiseStore) {
            this.Store = promiseStore;
            
            var notCompletes = this.Store.ListWaitingPromises();
            this._NotCompletes = new Queue<PromiseEntry>();
            foreach (var entity in notCompletes) {
                var visit = entity.ToVisit();
                var entry = new PromiseEntry() {
                    Visit = visit,
                    Id = entity.Id,
                    LastRequestTime = entity.LastRequestTime,
                    CreateTime = entity.CreateTime,
                    RetryInternvals = entity.RetryInternvals,
                    RequestCount = entity.RequestCount
                };
                this._NotCompletes.Enqueue(entry);
            }
        }
        Queue<PromiseEntry> _NotCompletes;
        public IPromiseStore Store { get; private set; }
        public void Request(AjaxOptions opts, AdditionInfo info=null) {
            opts.error = (ex, visit) => {
                this.AddToQueueAsync(visit,info).Start();
                return true;
            };
            Ajax.Request(opts);
        }

        Thread _Thread;

        async Task AddToQueueAsync(Visit visit, AdditionInfo info = null) {
            
            var entity = new PromiseEntity(visit);
            var entry = new PromiseEntry()
            {
                Visit = visit,
                Id= entity.Id = Guid.NewGuid(),
                LastRequestTime = entity.LastRequestTime = entity.CreateTime = DateTime.Now,
                RetryInternvals = entity.RetryInternvals = 1000,
                RequestCount = entity.RequestCount = 1
            };
            if (info != null) {
                entity.Category = info.Category;
                entity.BusinessId = info.BusinessId;
            }
            await this.Store.CreateAsync(entity);

            lock (this)
            {
                _NotCompletes.Enqueue(entry);
                if (_Thread == null)
                {
                    _Thread = new Thread(new ThreadStart(this.Loop));
                    _Thread.Start();
                }
            }
            
            
        }
        public bool RetryASAP(Guid id) {
            lock (this)
            {
                
                for (int i = 0, j = this._NotCompletes.Count; i < j; i++)
                {
                    var promise = this._NotCompletes.Dequeue();
                    if (promise.Id == id) {
                        promise.RetryInternvals = 0;
                        return true;
                    }
                    this._NotCompletes.Enqueue(promise);
                }
                return false;
            }
        }

        int _EnptyCount;

        void Loop() {
            while (true) {
                var toRuns = this.Dequeues();
                if (_EnptyCount > 10) {
                    lock (this) {
                        _Thread = null;
                        break;
                    }
                }
                Retry(toRuns);
                Thread.Sleep(1000);
            }
        }

        List<PromiseEntry> Dequeues() {
            var result = new List<PromiseEntry>();
            
            lock (this) {
                DateTime now = DateTime.Now;
                for (int i = 0, j = this._NotCompletes.Count; i < j; i++) {
                    var promise = this._NotCompletes.Dequeue();
                    if (CheckRunable(promise,now))
                    {
                        result.Add(promise);
                        if (result.Count == 16) break;
                    }
                    else {
                        this._NotCompletes.Enqueue(promise);
                    }
                }
                if (this._NotCompletes.Count == 0)
                {
                    ++_EnptyCount;
                }
                else _EnptyCount = 0;
            }
            return result;
        }

        void Retry(List<PromiseEntry> promises) {
            
            foreach (var promise in promises) {
                Retry(promise).Start();
            }
        }
        async Task Retry(PromiseEntry entry) {
            
            
            await Ajax.InternalRequestAsync(entry.Visit,(r,v)=> {
                var entity = PromiseEntity.FromVisit(v);
                entity.Id = entry.Id;
                entity.Id = entry.Id;
                entity.LastRequestTime = DateTime.Now;
                entity.RequestCount++;
                entity.IsSuccess = true;
                entity.Content = r.ToString();

                this.Store.UpdateAsync(entity).Start();

            },(e,v)=> {
                Task.Run(async()=> {
                    var entity = PromiseEntity.FromVisit(v);
                    entity.Id = entry.Id;
                    entity.LastRequestTime = DateTime.Now;
                    entity.RequestCount++;
                    entity.IsSuccess = false;
                    entity.Exception = e.ToString();

                    await this.Store.UpdateAsync(entity);
                    lock (this) {
                        _NotCompletes.Enqueue(entry);
                    }
                });
                
                return false;
            });
        }

        void ComputeIntervals(PromiseEntry entity) {
            var start = entity.CreateTime;
            var last = entity.LastRequestTime;
            var d = (last - start);
            if (d.Days > 1)
            {
                //一天以后，一小时重发一次
                entity.RetryInternvals = 1000 * 60 * 60;
            }
            else if (d.Hours > 1)
            {
                //一小时之后，30分钟重发一次
                entity.RetryInternvals = 1000 * 60 * 30;
            }
            else if (d.Minutes > 20) {
                //20分钟后，每15分钟重发一次
                entity.RetryInternvals = 1000 * 60 * 15;
            }
            if (d.Minutes > 10)
            {
                //20分钟后，每5分钟重发一次
                entity.RetryInternvals = 1000 * 60 * 5;
            }
            else if (d.Minutes > 3)
            {
                //3分钟后，每1分半钟重发一次
                entity.RetryInternvals = 1000 * 90;
            }
            else
            {
                //每30秒重发一次
                entity.RetryInternvals = 1000 * 30;
            }
        }
        bool CheckRunable(PromiseEntry entity,DateTime now) {
            
            var escaped = now - entity.LastRequestTime;
            if (escaped.Milliseconds > entity.RetryInternvals) return true;

            else return false;
        }
    }
}
