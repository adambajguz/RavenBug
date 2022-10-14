namespace CmpxchgBug
{
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CmpxchgBug.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Raven.Client;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Conventions;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;
    using Raven.Client.Exceptions;
    using Raven.Client.Json.Serialization.NewtonsoftJson;

    public sealed class DemoHostedService : BackgroundService
    {
        private int _value;
        private readonly Guid _runId = Guid.NewGuid();

        private readonly ILogger _logger;

        public DemoHostedService(ILogger<DemoHostedService> logger) :
            base()
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            using IDocumentStore documentStore = await CreateDocumentStoreAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int value = Interlocked.Increment(ref _value);

                    string lockName = $"bug-{_runId}--{value}";
                    _logger.LogInformation("Acquiring lock {LockName}", lockName);

                    Stopwatch stopwatch = Stopwatch.StartNew();

                    if (await TryAcquireAsync(documentStore, lockName, stoppingToken) is true)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation("Lock acquired {LockName} after {Elapsed}", lockName, stopwatch.Elapsed);
                        await Task.Delay(5_000, stoppingToken);
                        stopwatch.Restart();
                    }
                    else
                    {
                        throw new ApplicationException("Failed to acquire lock.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "An error occurred");

                    throw;
                }
            }
        }

        private async Task<IDocumentStore> CreateDocumentStoreAsync(CancellationToken cancellationToken = default)
        {
            DocumentStore documentStore = new()
            {
                Urls = new[]
                {
                    "http://192.168.0.201:8080"
                },
                Database = "RavenDBLockingBugIsolated",
                Conventions = new DocumentConventions
                {
                    Serialization = new NewtonsoftJsonSerializationConventions(),
                    SaveEnumsAsIntegers = false,
                }
            };

            documentStore.Initialize();

            await documentStore.EnsureDatabaseExistsAsync(cancellationToken);
            await documentStore.EnsureExpirationEnabledAsync(60, cancellationToken);

            return documentStore;
        }

        public async Task<bool> TryAcquireAsync(IDocumentStore documentStore, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                string transformedName = ToSha256Hex(name);
                Locker locker = new()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Name = name,
                    TransformedName = transformedName,
                };

                using IAsyncDocumentSession session = OpenAsyncSession(documentStore);

                CompareExchangeValue<Locker> cmpxchg = session.Advanced.ClusterTransaction.CreateCompareExchangeValue(transformedName, locker);
                cmpxchg.Metadata[Constants.Documents.Metadata.Expires] = DateTimeOffset.UtcNow.AddMinutes(1).UtcDateTime;

                await session.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (ConcurrencyException)
            {
                return false;
            }

            /// <inheritdoc/>
            static IAsyncDocumentSession OpenAsyncSession(IDocumentStore documentStore)
            {
                bool useOptimisticConcurrency = documentStore.Conventions.UseOptimisticConcurrency;

                SessionOptions sessionOptions = useOptimisticConcurrency
                    ? new SessionOptions()
                    : new SessionOptions { TransactionMode = TransactionMode.ClusterWide };

                IAsyncDocumentSession session = documentStore.OpenAsyncSession(sessionOptions);

                if (useOptimisticConcurrency)
                {
                    session.Advanced.UseOptimisticConcurrency = false;
                    session.Advanced.SetTransactionMode(TransactionMode.ClusterWide);
                }

                return session;
            }

            /// <summary>
            /// Converts lock name to SHA256-based name encoded in HEX.
            /// </summary>
            /// <param name="name"></param>
            static string ToSha256Hex(string name)
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                byte[] data = Encoding.UTF8.GetBytes(name);

                using SHA256 hasher = SHA256.Create();
                byte[] hash = hasher.ComputeHash(data);

                string transformedName = Convert.ToHexString(hash)
                                                .ToLowerInvariant();

                return transformedName;
            }
        }
    }
}
