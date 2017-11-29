using System.Threading.Tasks;
using Realms;

namespace KokoroIO.XamarinForms.Models.Data
{
    internal static class RealmServices
    {
        public static async Task<Realm> GetInstanceAsync()
        {
            var c = new RealmConfiguration()
            {
                ShouldDeleteIfMigrationNeeded = true
            };
            try
            {
                return await Realm.GetInstanceAsync(c).ConfigureAwait(false);
            }
            catch
            {
                Realm.DeleteRealm(c);

                return await Realm.GetInstanceAsync(c).ConfigureAwait(false);
            }
        }
    }
}