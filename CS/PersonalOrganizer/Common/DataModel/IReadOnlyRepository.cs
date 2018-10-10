using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PersonalOrganizer.Common.DataModel
{
    /// <summary>
    /// The IReadOnlyRepository interface represents the read-only implementation of the Repository pattern
    /// such that it can be used to query entities of a given type.
    /// </summary>
    /// <typeparam name="TEntity">Repository entity type.</typeparam>
    public interface IReadOnlyRepository<TEntity> : IRepositoryQuery<TEntity> where TEntity : class
    {
        /// <summary>
        /// The owner unit of work.
        /// </summary>
        IUnitOfWork UnitOfWork { get; }
    }

    public interface IRepositoryQuery<T> : IQueryable<T>
    {
        IRepositoryQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> path);

        IRepositoryQuery<T> Where(Expression<Func<T, bool>> predicate);
    }

    public abstract class RepositoryQueryBase<T> : IQueryable<T>
    {
        private readonly Lazy<IQueryable<T>> queryable;
        protected IQueryable<T> Queryable { get { return queryable.Value; } }

        protected RepositoryQueryBase(Func<IQueryable<T>> getQueryable)
        {
            queryable = new Lazy<IQueryable<T>>(getQueryable);
        }

        Type IQueryable.ElementType { get { return Queryable.ElementType; } }
        Expression IQueryable.Expression { get { return Queryable.Expression; } }
        IQueryProvider IQueryable.Provider { get { return Queryable.Provider; } }

        IEnumerator IEnumerable.GetEnumerator() { return Queryable.GetEnumerator(); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return Queryable.GetEnumerator(); }
    }

    public static class ReadOnlyRepositoryExtensions
    {
        public static IQueryable<TProjection> GetFilteredEntities<TEntity, TProjection>(this IReadOnlyRepository<TEntity> repository, Expression<Func<TEntity, bool>> filterExpression, Func<IRepositoryQuery<TEntity>, IQueryable<TProjection>> projection = null) where TEntity : class
        {
            var filtered = filterExpression != null ? repository.Where(filterExpression) : repository;
            return projection != null ? projection(filtered) : (IQueryable<TProjection>)filtered;
        }

        public static IQueryable<TEntity> GetFilteredEntities<TEntity>(this IReadOnlyRepository<TEntity> repository, Expression<Func<TEntity, bool>> filterExpression) where TEntity : class
        {
            return repository.GetFilteredEntities(filterExpression, x => x);
        }
    }
}