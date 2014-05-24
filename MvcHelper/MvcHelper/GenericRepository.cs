using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Dynamic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace MvcHelper
{
    public class GenericRepository<TDC, TEntity>
        where TDC : DbContext, new()
        where TEntity : class
    {
        protected TDC dc = new TDC();

        public GenericRepository()
        {
        }
        public GenericRepository(TDC dc)
        {
            this.dc = dc;
        } 

        public virtual void Add(TEntity entity)
        {
            dc.Set<TEntity>().Add(entity);
        }
        public virtual void Remove(TEntity entity)
        {
            dc.Set<TEntity>().Remove(entity);
        }

        public virtual void Add<T>(T entity)
            where T : class
        {
            dc.Set<T>().Add(entity);
        }
        public virtual void Remove<T>(T entity)
            where T : class
        {
            dc.Set<T>().Remove(entity);
        }

        public void Submit()
        {
            dc.SaveChanges();
        }

        interface IIdentityId
        {
            int Id { get; set; }
        }

        public TEntity Find(int id)
        {
            return dc.Set<TEntity>()
                .Find(id);
        }

        public virtual IQueryable<TEntity> GetQuery()
        {
            return dc.Set<TEntity>()
                .AsQueryable()
                .OrderBy("Id");
        }

        public IQueryable<T> GetQuery<T>()
            where T : class
        {
            return dc.Set<T>()
                .AsQueryable();
        }
        public DbQuery<T> GetDbQuery<T>()
            where T : class
        {
            return dc.Set<T>();
        }

    }
}