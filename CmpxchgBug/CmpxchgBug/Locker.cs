namespace CmpxchgBug
{
    using System;

    /// <summary>
    /// Raven locker.
    /// </summary>
    internal sealed record Locker
    {
        /// <summary>
        /// Unique identifier of the locker.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Locker creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; init; }

        /// <summary>
        /// Locker name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Locker transforemd name.
        /// </summary>
        public string TransformedName { get; init; }

        /// <summary>
        /// Initializes a new instance of <see cref="Locker"/>.
        /// </summary>
        public Locker()
        {
            Name = default!;
            TransformedName = default!;
        }
    }
}
