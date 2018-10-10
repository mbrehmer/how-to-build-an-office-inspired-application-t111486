using DevExpress.Mvvm;
using PersonalOrganizer.Common.DataModel;
using PersonalOrganizer.Model;

namespace PersonalOrganizer.ContactContextDataModel
{
    /// <summary>
    /// Provides methods to obtain the relevant IUnitOfWorkFactory.
    /// </summary>
    public static class UnitOfWorkSource
    {
        #region inner classes

        private class DbUnitOfWorkFactory : IUnitOfWorkFactory<IContactContextUnitOfWork>
        {
            public static readonly IUnitOfWorkFactory<IContactContextUnitOfWork> Instance = new DbUnitOfWorkFactory();

            private DbUnitOfWorkFactory()
            { }

            IContactContextUnitOfWork IUnitOfWorkFactory<IContactContextUnitOfWork>.CreateUnitOfWork()
            {
                return new ContactContextUnitOfWork(() => new ContactContext());
            }
        }

        private class DesignUnitOfWorkFactory : IUnitOfWorkFactory<IContactContextUnitOfWork>
        {
            public static readonly IUnitOfWorkFactory<IContactContextUnitOfWork> Instance = new DesignUnitOfWorkFactory();

            private DesignUnitOfWorkFactory()
            { }

            IContactContextUnitOfWork IUnitOfWorkFactory<IContactContextUnitOfWork>.CreateUnitOfWork()
            {
                return new ContactContextDesignTimeUnitOfWork();
            }
        }

        #endregion

        /// <summary>
        /// Returns the IUnitOfWorkFactory implementation based on the current mode (run-time or design-time).
        /// </summary>
        public static IUnitOfWorkFactory<IContactContextUnitOfWork> GetUnitOfWorkFactory()
        {
            return GetUnitOfWorkFactory(ViewModelBase.IsInDesignMode);
        }

        /// <summary>
        /// Returns the IUnitOfWorkFactory implementation based on the given mode (run-time or design-time).
        /// </summary>
        /// <param name="isInDesignTime">Used to determine which implementation of IUnitOfWorkFactory should be returned.</param>
        public static IUnitOfWorkFactory<IContactContextUnitOfWork> GetUnitOfWorkFactory(bool isInDesignTime)
        {
            return isInDesignTime ? DesignUnitOfWorkFactory.Instance : DbUnitOfWorkFactory.Instance;
        }
    }
}