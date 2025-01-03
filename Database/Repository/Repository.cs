using Database.Context;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class Repository<T> : IRepository<T> where T : BaseModel
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(T entity)
        {
            _context.Set<T>().Add(entity);
        }

        public async Task<T> Delete(int id, CancellationToken token)
        {
            var entity = await _context.Set<T>().FirstOrDefaultAsync(f => f.ID == id, token);
            _context.Set<T>().Remove(entity);
            return entity;
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _context.UpdateRange(entities);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _context.RemoveRange(entities);
        }

        public async Task<T> Get(int id, CancellationToken token, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            foreach(var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(c => c.ID == id, token);
        }

        public async Task<RefreshToken?> GetToken(string token, CancellationToken cancellationToken)
        {
            return await _context.Set<RefreshToken>().FirstOrDefaultAsync(f => f.Token == token, cancellationToken);
        }
        public async Task<RefreshToken> RemoveToken(string refreshToken, CancellationToken cancellationToken)
        {
            var entity = await _context.RefreshToken.FirstOrDefaultAsync(f => f.Token == refreshToken, cancellationToken);

            if(entity != null)
            {
                _context.RefreshToken.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

            }
            return entity;

        }

        public IQueryable<Lessons> GetLessonsByCourseId(int courseId)
        {
            return _context.Set<Lessons>().Where(c => c.CourseID == courseId);
        }

        public void AddRange(IEnumerable<T> allEntries)
        {
            _context.Set<T>().AddRange(allEntries);
        }

        public IQueryable<T> GetAll()
        {
            return _context.Set<T>().AsQueryable();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null ,CancellationToken token = default)
        {
            return predicate == null
                ? await _context.Set<T>().CountAsync(token)
                : await _context.Set<T>().CountAsync(predicate, token);
        }

        public async Task<bool> IsExist(Expression<Func<T, bool>> predicate, CancellationToken token)
        {
            return await _context.Set<T>().AnyAsync(predicate, token);
        }


        public async Task SaveAsync(CancellationToken token = default)
        {
            await _context.SaveChangesAsync(token);
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }
    }
}
