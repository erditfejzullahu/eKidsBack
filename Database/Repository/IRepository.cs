using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IRepository<T> where T : BaseModel
    {
        Task<T> Get(int id, CancellationToken token);
        Task<T> Delete(int id, CancellationToken token);
        Task<RefreshToken> GetToken(string token, CancellationToken cancellationToken);
        Task<RefreshToken> RemoveToken(string refreshToken, CancellationToken cancellationToken);
        IQueryable<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        Task SaveAsync(CancellationToken token);

    }
}
