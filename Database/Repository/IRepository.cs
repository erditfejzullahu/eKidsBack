using Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IRepository<T> where T : BaseModel
    {
        Task<T> Get(int id, CancellationToken token, params Expression<Func<T, object>>[] includes);
        Task<T> Delete(int id, CancellationToken token);
        void UpdateRange(IEnumerable<T> entities);
        void DeleteRange(IEnumerable<T> entities);
        Task<RefreshToken> GetToken(string token, CancellationToken cancellationToken);
        Task<RefreshToken> RemoveToken(string refreshToken, CancellationToken cancellationToken);
        IQueryable<T> GetAll();
        void AddRange(IEnumerable<T> allEntries);
        IQueryable<Lessons> GetLessonsByCourseId(int courseId);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, CancellationToken token = default);
        void Add(T entity);
        void Update(T entity);
        Task SaveAsync(CancellationToken token = default);
        Task<bool> IsExist(Expression<Func<T, bool>> predicate, CancellationToken token);

        //Task UpdateEnrolledLessons()

    }
}
