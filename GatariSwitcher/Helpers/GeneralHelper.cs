using System.Net;
using System.Threading.Tasks;

namespace GatariSwitcher
{
    static class GeneralHelper
    {
        public async static Task<string> GetGatariAddressAsync()
        {
            using (var webClient = new WebClient())
            {
                string result = string.Empty;
                try
                {
                    var line = await webClient.DownloadStringTaskAsync(Constants.GatariIpApiAddress);
                    result = line;
                }
                catch { }
                return result.Trim();
            }
        }
    }
}