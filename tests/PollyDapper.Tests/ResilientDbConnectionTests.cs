// <copyright file="ResilientDbConnectionTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyDapper.Tests;

public class ResilientDbConnectionTests
{
    private readonly Mock<IDbConnection> _connection = new();
    private readonly ResiliencePipeline _pipeline = ResiliencePipeline.Empty;

    [Fact]
    public void WithPolly_NullConnection_ThrowsArgumentNullException()
    {
        IDbConnection? connection = null;
        Assert.Throws<ArgumentNullException>(() => connection!.WithPolly(_pipeline));
    }

    [Fact]
    public void WithPolly_NullPipeline_ThrowsArgumentNullException()
    {
        ResiliencePipeline? pipeline = null;
        Assert.Throws<ArgumentNullException>(() => _connection.Object.WithPolly(pipeline!));
    }

    [Fact]
    public void WithPolly_ValidArguments_ReturnsResilientDbConnection()
    {
        var result = _connection.Object.WithPolly(_pipeline);
        Assert.NotNull(result);
        Assert.IsType<ResilientDbConnection>(result);
    }
}
