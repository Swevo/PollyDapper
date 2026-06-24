// <copyright file="PollyDapperServiceCollectionExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace PollyDapper.Tests;

public class PollyDapperServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPollyDapper_WithBuilder_RegistersResiliencePipelineSingleton()
    {
        var services = new ServiceCollection();
        services.AddPollyDapper(pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(5)));

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetService<ResiliencePipeline>();

        Assert.NotNull(pipeline);
    }

    [Fact]
    public void AddPollyDapper_WithPrebuiltPipeline_RegistersResiliencePipelineSingleton()
    {
        var services = new ServiceCollection();
        var prebuilt = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        services.AddPollyDapper(prebuilt);

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetService<ResiliencePipeline>();

        Assert.Same(prebuilt, pipeline);
    }

    [Fact]
    public void AddPollyDapper_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddPollyDapper(_ => { }));
    }

    [Fact]
    public void AddPollyDapper_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Action<ResiliencePipelineBuilder>? configure = null;
        Assert.Throws<ArgumentNullException>(() => services.AddPollyDapper(configure!));
    }
}
