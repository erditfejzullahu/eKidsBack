using Database.Context;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<T> Get(int id, CancellationToken token)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(f => f.ID == id, token);
        }

        public async Task<RefreshToken> GetToken(string token, CancellationToken cancellationToken)
        {
            return await _context.Set<RefreshToken>().FirstOrDefaultAsync(f => f.Token == token);
        }
        public async Task<RefreshToken> RemoveToken(string refreshToken, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<RefreshToken>().FirstOrDefaultAsync(f => f.Token == refreshToken);

            if(entity != null)
            {
                _context.Set<RefreshToken>().Remove(entity);
                await _context.SaveChangesAsync(default);
            }
            return entity;

        }

        public IQueryable<T> GetAll()
        {
            return _context.Set<T>().AsQueryable();
        }


        public async Task SaveAsync(CancellationToken token)
        {
            await _context.SaveChangesAsync(token);
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }
    }
}
