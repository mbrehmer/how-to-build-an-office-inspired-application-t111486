using PersonalOrganizer.Common.DataModel;
using PersonalOrganizer.Model;

namespace PersonalOrganizer.ContactContextDataModel
{
    /// <summary>
    /// IContactContextUnitOfWork extends the IUnitOfWork interface with repositories representing specific entities.
    /// </summary>
    public interface IContactContextUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// The Contact entities repository.
        /// </summary>
        IRepository<Contact, int> Contacts { get; }
    }
}