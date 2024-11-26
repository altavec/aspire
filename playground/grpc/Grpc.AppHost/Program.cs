// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

_ = builder.AddProject<Projects.Grpc_ApiService>("grpc-apiservice", Uri.UriSchemeHttp)
    .WithGrpcHealthCheck(Uri.UriSchemeHttp, Uri.UriSchemeHttp)
    .WithGrpcUI((IResourceBuilder<ProjectResource> api, IResourceBuilder<GrpcUIExecutableResource> grpc) => grpc.WaitFor(api));

await builder.Build().RunAsync().ConfigureAwait(false);