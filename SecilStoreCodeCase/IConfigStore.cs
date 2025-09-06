using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecilStoreCodeCase
{
    public interface IConfigStore
    {
        Task<IReadOnlyList<ConfigurationItem>> GetActiveAsync(string applicationName,CancellationToken ct=default); /* IsActive=1*/

        Task<IReadOnlyList<ConfigurationItem>> GetActiveChangedSinceAsync(string applicationName,
            DateTime dateTime, CancellationToken ct = default); /* periyodik refresh */

        Task<int> UpsertAsync(ConfigurationItem item, CancellationToken ct = default); /*update or insert */
        Task<int> DeactivateAsync (string applicationName,string name, CancellationToken ct = default); /* IsActive=0*/


        Task<IReadOnlyList<ConfigurationItem>> GetByApplicationAsync(string applicationName, CancellationToken ct = default);

        Task<IReadOnlyList<ConfigurationItem>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetApplicationsAsync(CancellationToken ct = default);
        Task<int> ActivateAsync(string applicationName, string name, CancellationToken cancellationToken = default);
    }
}
