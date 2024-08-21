// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithHealthCheck();

builder.AddProject<Projects.RabbitMQ_ApiService>("rabbitmq-apiservice")
    .WithReference(rabbitmq, wait: true);

await builder.Build().RunAsync().ConfigureAwait(false);