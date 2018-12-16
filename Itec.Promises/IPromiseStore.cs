using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Itec.Promises
{
    public interface IPromiseStore
    {
        void Create(PromiseEntity promise);
        Task CreateAsync(PromiseEntity promise);

        void Update(PromiseEntity entity);
        Task UpdateAsync(PromiseEntity entity);

        IList<PromiseEntity> ListWaitingPromises();

        Task<IList<PromiseEntity>> ListWaitingPromisesAsync();


    }
}
