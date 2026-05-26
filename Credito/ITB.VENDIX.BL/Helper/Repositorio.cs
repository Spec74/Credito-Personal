using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using ITB.VENDIX.DA;
using System.Data.Entity.Migrations;
using EntityState = System.Data.Entity.EntityState;

namespace ITB.VENDIX.BL
{
    public class Repositorio<T1> where T1 : class
    {
        //public static List<T1> Listar()
        //{
        //    using (var db = new VENDIXEntities())
        //    {
        //        return db.Set<T1>().ToList();
        //    }
        //}

        public static List<T1> Listar(
            Expression<Func<T1, bool>> filter = null,
            Func<IQueryable<T1>,IOrderedQueryable<T1>> orderBy = null, 
            string includeProperties = "")
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;

                IQueryable<T1> query = db.Set<T1>();
                if (filter != null)
                    query = query.Where(filter);
               
                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }

                if (orderBy != null)
                {
                    return orderBy(query).ToList();
                }

                return query.ToList();
            }
        }

        public static int Contar(Expression<Func<T1, bool>> filter = null, string includeProperties = "")
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;

                IQueryable<T1> query = db.Set<T1>();
                if (filter != null)
                    query = query.Where(filter);

                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
                return query.Count();
            }
        }
        
        public static T1 Obtener(DbContext dbContext, int id)
        {
            return dbContext.Set<T1>().Find(id);
        }

        public static T1 Obtener(int id)
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;

                return db.Set<T1>().Find(id);
            }
        }
        public static T1 Obtener(Expression<Func<T1, bool>> filter = null, string includeProperties = "")
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                
                IQueryable<T1> query = db.Set<T1>();
                if (filter != null)
                    query = query.Where(filter);

                foreach (var includeProperty in includeProperties.Split
                    (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }

                return query.FirstOrDefault();
            }
        }

        public static void Crear(DbContext dbContext, T1 entity)
        {
            DbEntityEntry dbEntityEntry = dbContext.Entry(entity);
            if (dbEntityEntry.State != EntityState.Detached)
            {
                dbEntityEntry.State = EntityState.Added;
            }
            else
            {
                dbContext.Set<T1>().Add(entity);
            }
            
        }

        public static T1 Crear(T1 entity)
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;

                Crear( db, entity);
                if (db.SaveChanges() > 0)
                    return entity;
            }
            return null;
        }
        public static T1 Guardar(T1 entity)
        {
            using (var db = new VENDIXEntities())
            {                
                db.Set<T1>().AddOrUpdate(entity);
                if (db.SaveChanges() > 0)
                    return entity;
            }
            return null;
        }
        public static void Guardar(List<T1> entities)
        {
            using (var db = new VENDIXEntities())
            {
                foreach (var e in entities)
                    db.Set<T1>().AddOrUpdate(e);
                db.SaveChanges() ;
            }        
        }

        public static void Actualizar(DbContext dbContext, T1 entity)
        {
            DbEntityEntry dbEntityEntry = dbContext.Entry(entity);
            if (dbEntityEntry.State == EntityState.Detached)
            {
                dbContext.Set<T1>().Attach(entity);
            }
            dbEntityEntry.State = EntityState.Modified;
        }

        public static bool Actualizar(T1 entity)
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;

                Actualizar(db, entity);
                if (db.SaveChanges() > 0)
                    return true;
            }
            return false;
        }
        public static void ActualizarParcial(T1 entity, params Expression<Func<T1, object>>[] properties)
        {
            using (var db = new VENDIXEntities())
            {
                var entry = db.Entry(entity);
                if (entry.State == EntityState.Detached)
                    db.Set<T1>().Attach(entity);

                if (properties == null)
                    foreach (var p in properties) entry.Property(p).IsModified = true;
                else
                    entry.State = EntityState.Modified;
                
               db.SaveChanges();
            }            
        }


        public static void Eliminar(DbContext dbContext, T1 entity)
        {
            DbEntityEntry dbEntityEntry = dbContext.Entry(entity);
            if (dbEntityEntry.State != EntityState.Deleted)
            {
                dbEntityEntry.State = EntityState.Deleted;
            }
            else
            {
                dbContext.Set<T1>().Attach(entity);
                dbContext.Set<T1>().Remove(entity);
            }
        }

        public static void Eliminar(DbContext dbContext, int id)
        {
            var entity = Obtener(dbContext, id);
            if (entity == null) return;
            Eliminar(dbContext, entity);
        }

        public static bool Eliminar(int pId)
        {
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;

                Eliminar(db, pId);
                if (db.SaveChanges() > 0)
                    return true;
            }
            return false;
        }

        public static bool EjecutarSql(string query)
        {
            using (var db = new VENDIXEntities())
            {
                db.Database.ExecuteSqlCommand(query);
                return true;
            }
        }
    }
}
