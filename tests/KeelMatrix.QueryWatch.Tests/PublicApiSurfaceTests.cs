// Copyright (c) KeelMatrix

using System.Reflection;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests;

public sealed class PublicApiSurfaceTests {

    [Fact]
    public void No_Unexpected_Public_Types_Exist() {
        // Arrange
        Assembly asm = typeof(QueryWatchSession).Assembly;

        // Only these namespaces are allowed to expose public types
        string[] allowedNamespaces = [
            "KeelMatrix.QueryWatch",
            "KeelMatrix.QueryWatch.Reporting",
            "KeelMatrix.QueryWatch.Assertions",
            "KeelMatrix.QueryWatch.Redaction"
        ];

        // Act
        var offenders = asm
            .GetExportedTypes()
            .Where(t =>
                t.Namespace is null ||
                !allowedNamespaces.Contains(t.Namespace, StringComparer.Ordinal)
            )
            .Select(t => t.FullName!)
            .Order()
            .ToList();

        // Assert
        offenders.Should().BeEmpty(
            "public API surface must be restricted to explicitly approved namespaces"
        );
    }
}
