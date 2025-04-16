using Integration.Tests.Fixtures;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Collection definition for integration tests to ensure they run sequentially
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}