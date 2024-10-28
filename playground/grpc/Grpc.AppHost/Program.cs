// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Grpc_ApiService>("grpc-apiservice", Uri.UriSchemeHttp)
    .WithGrpcHealthCheck(Uri.UriSchemeHttp, Uri.UriSchemeHttp)
    .WithGrpcUI();

await builder.Build().RunAsync().ConfigureAwait(false);