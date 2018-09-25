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
                "178.62.255.17" + " osu.ppy.sh",
                "178.62.255.17" + " c.ppy.sh",
                "178.62.255.17" + " c1.ppy.sh",
                "178.62.255.17" + " a.ppy.sh",
                "178.62.255.17" + " i.ppy.sh",
                "194.135.85.107" + " i.ppy.sh"
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
            bool isGatari = HostsFile.ReadAllLines().Any(x => x.Contains("osu.ppy.sh") && !x.Contains("#") && x.Contains("178.62.255.17"));
            return isGatari ? Server.Gatari : Server.Official;
        }
    }

    public enum Server
    {
        Official,
        Gatari
    }
}
