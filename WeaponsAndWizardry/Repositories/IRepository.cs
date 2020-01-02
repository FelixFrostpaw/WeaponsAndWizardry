using System.Threading.Tasks;

namespace WeaponsAndWizardry.Repositories
{
    interface IRepository<T>
    {
        // Gets the Resource with the corresponding Id.
        // Returns null if the Resource does not exist.
        Task<T> Get(string id);

        // Creates a Resource with the corresponding Id, and returns true.
        // If and only if a Resource with the Id already exists, 
        // the resource is NOT created, and this returns false.
        Task<bool> Create(T item);
        
        // Optimistically tries to update a Resource 
        // with the corresponding Id. 
        // If and only if we fail to update the item because
        // we were not updating the latest version of the Resource,
        // then the resource is NOT updated, and this returns false.
        Task<bool> Update(T item);
    }
}
