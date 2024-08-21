// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

var db1 = builder.AddPostGis("db1");

_ = builder.AddProject<Projects.PostGis_ApiService>("apiservice")
    .WithReference(db1);

await builder.Build().RunAsync().ConfigureAwait(false);