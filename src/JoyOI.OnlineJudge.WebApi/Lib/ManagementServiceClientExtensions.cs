using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JoyOI.ManagementService.SDK
{
    public static class ManagementServiceClientExtensions
    {
        public static async Task<string> ReadBlobAsStringAsync(this ManagementServiceClient mgmt, Guid blobId, CancellationToken token)
        {
            var blob = await mgmt.GetBlobAsync(blobId, token);
            return Encoding.UTF8.GetString(blob.Body);
        }

        public static async Task<T> ReadBlobAsObjectAsync<T>(this ManagementServiceClient mgmt, Guid blobId, CancellationToken token)
        {
            var jsonString = await ReadBlobAsStringAsync(mgmt, blobId, token);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
