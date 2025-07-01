// -----------------------------------------------------------------------
// <copyright file="AppHost.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

_ = builder.AddContainer("mycontainer", "myimage")
    .WithContainerfile("Context");

_ = builder
    .AddPostGis("database", PostgresVersion.V16, PostGisVersion.V3_5)
    .WithTle()
    .WithRust()
    .WithImageBuildPolicy(ImageBuildPolicy.Default);

_ = builder.AddContainerBuildEnvironment("container-build");

_ = builder.UpdateDockerfileBuildSymLinks();

await builder.Build().RunAsync(CancellationToken.None).ConfigureAwait(false);