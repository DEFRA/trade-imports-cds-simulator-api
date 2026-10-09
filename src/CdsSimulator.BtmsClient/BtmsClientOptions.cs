using System;

namespace CdsSimulator.BtmsClient
{
    public class BtmsClientOptions
    {
        public required string GatewayBaseUrl { get; init; }

        public required string UsernameToken { get; init; }

        public required string Password { get; init; }

        public required Dictionary<string, BtmsClientRoute> Routes { get; init; }
    }

    public class BtmsClientRoute
    {
        public required string Path { get; init; }
    }
}
