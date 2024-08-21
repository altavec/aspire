// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

var db1 = builder.AddPostGis("db1").WithHealthCheck();

_ = builder.AddProject<Projects.PostGis_ApiService>("apiservice")
    .WithReference(db1, wait: true);

await builder.Build().RunAsync().ConfigureAwait(false);