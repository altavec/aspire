// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var minio = builder
    .AddMinIO("minio")
    .WithDataVolume();

builder.AddProject<Projects.MinIO_ApiService>("minio-apiservice")
    .WithReference(minio)
    .WaitFor(minio);

await builder.Build().RunAsync().ConfigureAwait(false);