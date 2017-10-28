using System.Threading.Tasks;
using Realms;

namespace KokoroIO.XamarinForms.Models.Data
{
    internal static class RealmServices
    {
        public static Task<Realm> GetInstanceAsync()
            => Realm.GetInstanceAsync(new RealmConfiguration()
            {
                ShouldDeleteIfMigrationNeeded = true
            });
    }
}