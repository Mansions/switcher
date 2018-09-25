namespace GatariSwitcher
{
    public class Constants
    {
        // Use this address if we cannot grab from ips.txt
        public const string GatariHardcodedIp = "178.62.255.17";
        public const string MirrorIP = "194.135.85.107";

        // Grab address from here when possible
        public const string GatariIpApiAddress = "https://dl.themansions.nl/ip.txt";

        public const string UiInstallCertificate = "Install Certificate";

        public const string UiUninstallCertificate = "Delete Certificate";

        public const string UiYouArePlayingOnGatari = "You're connected to TheMansions!";

        public const string UiYouArePlayingOnOfficial = "You're playing on Bancho";

        public const string UiSwitchToGatari = "Connect to TheMansions";

        public const string UiSwitchToOfficial = "Disconnect from TheMansions";

        public const string UiUpdatingStatus = "Retrieving addresses..";
    }
}
