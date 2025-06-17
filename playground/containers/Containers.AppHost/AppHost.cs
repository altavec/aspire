// -----------------------------------------------------------------------
// <copyright file="AppHost.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

_ = builder.AddContainer("mycontainer", "myimage")
    .WithContainerfile("Context");

_ = builder
    .AddPostgres16("database")
    .WithDotnet()
    .WithImageBuildPolicy(ImageBuildPolicy.Default);

_ = builder.AddContainerBuildEnvironment("container-build");

await builder.Build().RunAsync(CancellationToken.None).ConfigureAwait(false);