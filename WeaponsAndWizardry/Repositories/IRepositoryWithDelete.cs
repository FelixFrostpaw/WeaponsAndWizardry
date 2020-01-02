using System.Threading.Tasks;

namespace WeaponsAndWizardry.Repositories
{
    interface IRepositoryWithDelete<T> : IRepository<T>
    {
        // Deletes a Resource with the corresponding Id, and returns true.
        // If and only if a Resource with the Id DOES NOT EXIST, 
        // the resource is NOT deleted, and this returns false.
        Task<bool> Delete(string id);
    }
}
