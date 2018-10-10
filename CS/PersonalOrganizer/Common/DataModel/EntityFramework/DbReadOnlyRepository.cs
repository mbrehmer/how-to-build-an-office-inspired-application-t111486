using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace PersonalOrganizer.Common.DataModel.EntityFramework
{
    /// <summary>
    /// A DbReadOnlyRepository is a IReadOnlyRepository interface implementation representing the collection of all entities in the unit of work, or that can be queried from the database, of a given type.
    /// DbReadOnlyRepository objects are created from a DbUnitOfWork using the GetReadOnlyRepository method.
    /// DbReadOnlyRepository provides only read-only operations against entities of a given type.
    /// </summary>
    /// <typeparam name="TEntity">Repository entity type.</typeparam>
    /// <typeparam name="TDbContext">DbContext type.</typeparam>
    public class DbReadOnlyRepository<TEntity, TDbContext> : DbRepositoryQuery<TEntity>, IReadOnlyRepository<TEntity>
        where TEntity : class
        where TDbContext : DbContext
    {
        private readonly Func<TDbContext, DbSet<TEntity>> dbSetAccessor;
        private readonly DbUnitOfWork<TDbContext> unitOfWork;

        /// <summary>
        /// Initializes a new instance of DbReadOnlyRepository class.
        /// </summary>
        /// <param name="unitOfWork">Owner unit of work that provides context for repository entities.</param>
        /// <param name="dbSetAccessor">Function that returns DbSet entities from Entity Framework DbContext.</param>
        public DbReadOnlyRepository(DbUnitOfWork<TDbContext> unitOfWork, Func<TDbContext, DbSet<TEntity>> dbSetAccessor)
            : base(() => dbSetAccessor(unitOfWork.Context))
        {
            this.dbSetAccessor = dbSetAccessor;
            this.unitOfWork = unitOfWork;
        }

        protected DbSet<TEntity> DbSet
        {
            get { return dbSetAccessor(unitOfWork.Context); }
        }

        protected TDbContext Context
        {
            get { return unitOfWork.Context; }
        }

        #region IReadOnlyRepository

        IUnitOfWork IReadOnlyRepository<TEntity>.UnitOfWork
        {
            get { return unitOfWork; }
        }

        #endregion
    }

    public class DbRepositoryQuery<TEntity> : RepositoryQueryBase<TEntity>, IRepositoryQuery<TEntity> where TEntity : class
    {
        public DbRepositoryQuery(Func<IQueryable<TEntity>> getQueryable)
            : base(getQueryable) { }

        IRepositoryQuery<TEntity> IRepositoryQuery<TEntity>.Include<TProperty>(Expression<Func<TEntity, TProperty>> path)
        {
            return new DbRepositoryQuery<TEntity>(() => Queryable.Include(path));
        }

        IRepositoryQuery<TEntity> IRepositoryQuery<TEntity>.Where(Expression<Func<TEntity, bool>> predicate)
        {
            return new DbRepositoryQuery<TEntity>(() => Queryable.Where(predicate));
        }
    }
}