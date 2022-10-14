namespace CmpxchgBug.Configuration
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="RavenLockerStoreOptions"/> validator.
    /// </summary>
    public sealed class RavenLockerStoreOptionsValidator : IValidateOptions<RavenLockerStoreOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string name, RavenLockerStoreOptions options)
        {
            List<string> failures = new();

            if (options.Urls is not { Length: > 0 })
            {
                failures.Add($"{nameof(RavenLockerStoreOptions.Urls)} must not be empty");
            }

            if (string.IsNullOrWhiteSpace(options.Database))
            {
                failures.Add($"{nameof(RavenLockerStoreOptions.Database)} cannot be null or whitespace");
            }

            return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
        }
    }
}
