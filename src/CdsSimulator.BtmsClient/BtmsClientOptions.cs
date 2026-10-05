using System;

namespace CdsSimulator.BtmsClient
{
    public class BtmsClientOptions
    {
        /// <summary>
        /// Base URL of the BTMS gateway (e.g. https://btms.example)
        /// </summary>
        public required string GatewayBaseUrl { get; init; }

        /// <summary>
        /// Optional username token used in the SOAP header.
        /// </summary>
        public required string UsernameToken { get; init; }

        /// <summary>
        /// Optional password used in the SOAP header.
        /// </summary>
        public required string Password { get; init; }

        public required Dictionary<string, BtmsClientRoute> Routes { get; init; }
    }

    public class BtmsClientRoute
    {
        public required string Path { get; init; }
    }
}
