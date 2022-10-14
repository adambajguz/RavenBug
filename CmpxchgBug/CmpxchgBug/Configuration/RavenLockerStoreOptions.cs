namespace CmpxchgBug.Configuration
{
    using System.Security.Cryptography.X509Certificates;
    using Raven.Client.Documents.Conventions;

    /// <summary>
    /// RavenDb locker store options.
    /// </summary>
    public sealed class RavenLockerStoreOptions
    {
        /// <summary>
        /// The urls used to connect to a server.
        /// </summary>
        public string[]? Urls { get; set; }

        /// <summary>
        /// The database used when no specific database is specified.
        /// </summary>
        public string? Database { get; set; }

        /// <summary>
        /// Optional client certificate to use for authentication.
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Optional conventions used to determine Client API behavior.
        /// </summary>
        public DocumentConventions? Conventions { get; set; }
    }
}
