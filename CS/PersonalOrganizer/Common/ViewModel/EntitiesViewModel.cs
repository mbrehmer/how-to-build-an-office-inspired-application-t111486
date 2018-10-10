using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using PersonalOrganizer.Common.DataModel;
using PersonalOrganizer.Common.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PersonalOrganizer.Common.ViewModel
{
    public abstract partial class EntitiesViewModel<TEntity, TProjection, TUnitOfWork> :
        EntitiesViewModelBase<TEntity, TProjection, TUnitOfWork>
        where TEntity : class
        where TProjection : class
        where TUnitOfWork : IUnitOfWork
    {
        protected EntitiesViewModel(
            IUnitOfWorkFactory<TUnitOfWork> unitOfWorkFactory,
            Func<TUnitOfWork, IReadOnlyRepository<TEntity>> getRepositoryFunc,
            Func<IRepositoryQuery<TEntity>, IQueryable<TProjection>> projection)
            : base(unitOfWorkFactory, getRepositoryFunc, projection)
        {
        }
    }

    [POCOViewModel]
    public abstract class EntitiesViewModelBase<TEntity, TProjection, TUnitOfWork> : IEntitiesViewModel<TProjection>
        where TEntity : class
        where TProjection : class
        where TUnitOfWork : IUnitOfWork
    {
        #region inner classes

        protected interface IEntitiesChangeTracker
        {
            void RegisterMessageHandler();

            void UnregisterMessageHandler();
        }

        protected class EntitiesChangeTracker<TPrimaryKey> : IEntitiesChangeTracker
        {
            private readonly EntitiesViewModelBase<TEntity, TProjection, TUnitOfWork> owner;

            private ObservableCollection<TProjection> Entities
            { get { return owner.Entities; } }

            private IRepository<TEntity, TPrimaryKey> Repository
            { get { return (IRepository<TEntity, TPrimaryKey>)owner.ReadOnlyRepository; } }

            public EntitiesChangeTracker(EntitiesViewModelBase<TEntity, TProjection, TUnitOfWork> owner)
            {
                this.owner = owner;
            }

            void IEntitiesChangeTracker.RegisterMessageHandler()
            {
                Messenger.Default.Register<EntityMessage<TEntity, TPrimaryKey>>(this, x => OnMessage(x));
            }

            void IEntitiesChangeTracker.UnregisterMessageHandler()
            {
                Messenger.Default.Unregister(this);
            }

            public TProjection FindLocalProjectionByKey(TPrimaryKey primaryKey)
            {
                var primaryKeyEqualsExpression = RepositoryExtensions.GetProjectionPrimaryKeyEqualsExpression<TEntity, TProjection, TPrimaryKey>(Repository, primaryKey);
                return Entities.AsQueryable().FirstOrDefault(primaryKeyEqualsExpression);
            }

            public TProjection FindActualProjectionByKey(TPrimaryKey primaryKey)
            {
                var projectionEntity = Repository.FindActualProjectionByKey(owner.Projection, primaryKey);
                if (projectionEntity != null && ExpressionHelper.IsFitEntity(Repository.Find(primaryKey), owner.GetFilterExpression()))
                {
                    owner.OnEntitiesLoaded(GetUnitOfWork(Repository), new TProjection[] { projectionEntity });
                    return projectionEntity;
                }
                return null;
            }

            private void OnMessage(EntityMessage<TEntity, TPrimaryKey> message)
            {
                if (!owner.IsLoaded)
                    return;
                switch (message.MessageType)
                {
                    case EntityMessageType.Added:
                        OnEntityAdded(message.PrimaryKey);
                        break;

                    case EntityMessageType.Changed:
                        OnEntityChanged(message.PrimaryKey);
                        break;

                    case EntityMessageType.Deleted:
                        OnEntityDeleted(message.PrimaryKey);
                        break;
                }
            }

            private void OnEntityAdded(TPrimaryKey primaryKey)
            {
                var projectionEntity = FindActualProjectionByKey(primaryKey);
                if (projectionEntity != null)
                    Entities.Add(projectionEntity);
            }

            private void OnEntityChanged(TPrimaryKey primaryKey)
            {
                var existingProjectionEntity = FindLocalProjectionByKey(primaryKey);
                var projectionEntity = FindActualProjectionByKey(primaryKey);
                if (projectionEntity == null)
                {
                    Entities.Remove(existingProjectionEntity);
                    return;
                }
                if (existingProjectionEntity != null)
                {
                    Entities[Entities.IndexOf(existingProjectionEntity)] = projectionEntity;
                    owner.RestoreSelectedEntity(existingProjectionEntity, projectionEntity);
                    return;
                }
                OnEntityAdded(primaryKey);
            }

            private void OnEntityDeleted(TPrimaryKey primaryKey)
            {
                Entities.Remove(FindLocalProjectionByKey(primaryKey));
            }
        }

        #endregion

        protected readonly IUnitOfWorkFactory<TUnitOfWork> unitOfWorkFactory;
        protected readonly Func<TUnitOfWork, IReadOnlyRepository<TEntity>> getRepositoryFunc;
        protected Func<IRepositoryQuery<TEntity>, IQueryable<TProjection>> Projection { get; private set; }

        protected EntitiesViewModelBase(
            IUnitOfWorkFactory<TUnitOfWork> unitOfWorkFactory,
            Func<TUnitOfWork, IReadOnlyRepository<TEntity>> getRepositoryFunc,
            Func<IRepositoryQuery<TEntity>, IQueryable<TProjection>> projection
            )
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
            this.getRepositoryFunc = getRepositoryFunc;
            Projection = projection;
            ChangeTracker = CreateEntitiesChangeTracker();
            if (!this.IsInDesignMode())
                OnInitializeInRuntime();
        }

        protected IEntitiesChangeTracker ChangeTracker { get; private set; }

        private ObservableCollection<TProjection> entities = new ObservableCollection<TProjection>();
        protected IReadOnlyRepository<TEntity> ReadOnlyRepository { get; private set; }
        protected bool IsLoaded { get { return ReadOnlyRepository != null; } }
        public virtual bool IsLoading { get; protected set; }

        /// <summary>
        /// The collection of entities loaded from the unit of work.
        /// </summary>
        public ObservableCollection<TProjection> Entities
        {
            get
            {
                if (!IsLoaded)
                    LoadEntities(false);
                return entities;
            }
        }

        private CancellationTokenSource loadCancellationTokenSource;

        protected void LoadEntities(bool forceLoad)
        {
            if (forceLoad)
            {
                if (loadCancellationTokenSource != null)
                    loadCancellationTokenSource.Cancel();
            }
            else if (IsLoading)
            {
                return;
            }
            loadCancellationTokenSource = LoadCore();
        }

        private void CancelLoading()
        {
            if (loadCancellationTokenSource != null)
                loadCancellationTokenSource.Cancel();
            IsLoading = false;
        }

        private CancellationTokenSource LoadCore()
        {
            IsLoading = true;
            var cancellationTokenSource = new CancellationTokenSource();
            var selectedEntityCallback = GetSelectedEntityCallback();
            Task.Factory.StartNew(() =>
            {
                var repository = CreateReadOnlyRepository();
                var entities = new ObservableCollection<TProjection>(repository.GetFilteredEntities(GetFilterExpression(), Projection));
                OnEntitiesLoaded(GetUnitOfWork(repository), entities);
                return new Tuple<IReadOnlyRepository<TEntity>, ObservableCollection<TProjection>>(repository, entities);
            }).ContinueWith(x =>
            {
                if (!x.IsFaulted)
                {
                    ReadOnlyRepository = x.Result.Item1;
                    entities = x.Result.Item2;
                    this.RaisePropertyChanged(y => y.Entities);
                    OnEntitiesAssigned(selectedEntityCallback);
                }
                IsLoading = false;
            }, cancellationTokenSource.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            return cancellationTokenSource;
        }

        private static TUnitOfWork GetUnitOfWork(IReadOnlyRepository<TEntity> repository)
        {
            return (TUnitOfWork)repository.UnitOfWork;
        }

        protected virtual void OnEntitiesLoaded(TUnitOfWork unitOfWork, IEnumerable<TProjection> entities)
        {
        }

        protected virtual void OnEntitiesAssigned(Func<TProjection> getSelectedEntityCallback)
        {
        }

        protected virtual Func<TProjection> GetSelectedEntityCallback()
        {
            return null;
        }

        protected virtual void RestoreSelectedEntity(TProjection existingProjectionEntity, TProjection projectionEntity)
        {
        }

        protected virtual Expression<Func<TEntity, bool>> GetFilterExpression()
        {
            return null;
        }

        protected virtual void OnInitializeInRuntime()
        {
            if (ChangeTracker != null)
                ChangeTracker.RegisterMessageHandler();
        }

        protected virtual void OnDestroy()
        {
            CancelLoading();
            if (ChangeTracker != null)
                ChangeTracker.UnregisterMessageHandler();
        }

        protected virtual void OnIsLoadingChanged()
        {
        }

        protected IReadOnlyRepository<TEntity> CreateReadOnlyRepository()
        {
            return getRepositoryFunc(CreateUnitOfWork());
        }

        protected TUnitOfWork CreateUnitOfWork()
        {
            return unitOfWorkFactory.CreateUnitOfWork();
        }

        protected virtual IEntitiesChangeTracker CreateEntitiesChangeTracker()
        {
            return null;
        }

        protected IDocumentOwner DocumentOwner { get; private set; }

        #region IDocumentContent

        object IDocumentContent.Title { get { return null; } }

        void IDocumentContent.OnClose(CancelEventArgs e) { }

        void IDocumentContent.OnDestroy()
        {
            OnDestroy();
        }

        IDocumentOwner IDocumentContent.DocumentOwner
        {
            get { return DocumentOwner; }
            set { DocumentOwner = value; }
        }

        #endregion

        #region IEntitiesViewModel

        ObservableCollection<TProjection> IEntitiesViewModel<TProjection>.Entities { get { return Entities; } }
        bool IEntitiesViewModel<TProjection>.IsLoading { get { return IsLoading; } }

        #endregion
    }

    public interface IEntitiesViewModel<TEntity> : IDocumentContent where TEntity : class
    {
        ObservableCollection<TEntity> Entities { get; }
        bool IsLoading { get; }
    }
}