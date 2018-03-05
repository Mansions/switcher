using System.Linq;
using GatariSwitcher.Extensions;
using GatariSwitcher.Helpers;
using System.Threading.Tasks;

namespace GatariSwitcher
{
    class ServerSwitcher
    {
        private readonly string serverAddress;

        public ServerSwitcher(string gatariIpAddress)
        {
            this.serverAddress = gatariIpAddress;
        }

        public void SwitchToGatari()
        {
            var lines = HostsFile.ReadAllLines();
            var result = lines.Where(x => !x.Contains("ppy.sh")).ToList();
            result.AddRange
            (
                serverAddress + " osu.ppy.sh",
                serverAddress + " c.ppy.sh",
                serverAddress + " c1.ppy.sh",
                serverAddress + " a.ppy.sh",
                serverAddress + " i.ppy.sh"
            );
            HostsFile.WriteAllLines(result);
        }

        public void SwitchToOfficial()
        {
            HostsFile.WriteAllLines(HostsFile.ReadAllLines().Where(x => !x.Contains("ppy.sh")));
        }

        public Task<Server> GetCurrentServerAsync()
        {
            return Task.Run<Server>(() => GetCurrentServer());
        }

        public Server GetCurrentServer()
        {
            bool isGatari = HostsFile.ReadAllLines().Any(x => x.Contains("osu.ppy.sh") && !x.Contains("#"));
            return isGatari ? Server.Gatari : Server.Official;
        }
    }

    public enum Server
    {
        Official,
        Gatari
    }
}
