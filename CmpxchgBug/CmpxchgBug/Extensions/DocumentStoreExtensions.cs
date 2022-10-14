namespace CmpxchgBug.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations;
    using Raven.Client.Documents.Operations.Expiration;
    using Raven.Client.Exceptions;
    using Raven.Client.Exceptions.Database;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    /// <summary>
    /// <see cref="IDocumentStore"/> extensions.
    /// </summary>
    public static class DocumentStoreExtensions
    {
        /// <summary>
        /// Ensures database exists.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task EnsureDatabaseExistsAsync(this IDocumentStore store,
                                                           CancellationToken cancellationToken = default)
        {
            if (await store.DatabaseExistsAsync(cancellationToken) is true)
            {
                return;
            }

            try
            {
                DatabaseRecord databaseRecord = new(store.Database);

                CreateDatabaseOperation operation = new(databaseRecord);

                await store.Maintenance.Server
                    .SendAsync<DatabasePutResult>(operation, cancellationToken);
            }
            catch (ConcurrencyException)
            {

            }
        }

        /// <summary>
        /// Ensures document expiration is enabled.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="deleteFrequencyInSec"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task EnsureExpirationEnabledAsync(this IDocumentStore store, long deleteFrequencyInSec, CancellationToken cancellationToken = default)
        {
            try
            {
                ExpirationConfiguration expirationConfig = new()
                {
                    Disabled = false,
                    DeleteFrequencyInSec = deleteFrequencyInSec
                };

                await store.Maintenance.SendAsync(new ConfigureExpirationOperation(expirationConfig), cancellationToken);
            }
            catch (ConcurrencyException)
            {

            }
        }

        /// <returns></returns>
        public static async Task<bool> DatabaseExistsAsync(this IDocumentStore store, CancellationToken cancellationToken = default)
        {
            string database = store.Database;

            if (string.IsNullOrWhiteSpace(database))
            {
                throw new ArgumentException("Database name cannot be null or whitespace.", nameof(store));
            }

            try
            {
                await store.Maintenance
                    .ForDatabase(database)
                    .SendAsync(new GetStatisticsOperation(), cancellationToken);

                return true;
            }
            catch (DatabaseDoesNotExistException)
            {
                return false;
            }
        }
    }
}
