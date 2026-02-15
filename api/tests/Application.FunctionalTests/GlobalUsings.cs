global using Ardalis.GuardClauses;
global using FluentAssertions;
global using NSubstitute;
global using NUnit.Framework;

// All functional tests share a single Testcontainers SQL Server instance and use Respawner
// for database reset between tests. Parallel execution causes cross-test interference.
[assembly: NonParallelizable]
