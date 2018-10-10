using PersonalOrganizer.Common.DataModel;
using PersonalOrganizer.Model;

namespace PersonalOrganizer.ContactContextDataModel
{
    /// <summary>
    /// A ContactContextDesignTimeUnitOfWork instance that represents the design-time implementation of the IContactContextUnitOfWork interface.
    /// </summary>
    public class ContactContextDesignTimeUnitOfWork : DesignTimeUnitOfWork, IContactContextUnitOfWork
    {
        /// <summary>
        /// Initializes a new instance of the ContactContextDesignTimeUnitOfWork class.
        /// </summary>
        public ContactContextDesignTimeUnitOfWork()
        {
        }

        IRepository<Contact, int> IContactContextUnitOfWork.Contacts
        {
            get { return GetRepository((Contact x) => x.Id); }
        }
    }
}