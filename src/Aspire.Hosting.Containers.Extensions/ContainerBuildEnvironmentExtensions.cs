// -----------------------------------------------------------------------
// <copyright file="ContainerBuildEnvironmentExtensions.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting;

// ReSharper disable RedundantNameQualifier
using Aspire.Hosting.Containers;
using Aspire.Hosting.Lifecycle;

// ReSharper restore RedundantNameQualifier

/// <summary>
/// The Container Build environment extensions.
/// </summary>
public static class ContainerBuildEnvironmentExtensions
{
    /// <summary>
    /// Adds a Container Build environment to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the Docker Compose environment resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{ContainerBuildEnvironmentResource}"/>.</returns>
    public static IResourceBuilder<ContainerBuildEnvironmentResource> AddContainerBuildEnvironment(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new ContainerBuildEnvironmentResource(name);
        builder.Services.TryAddLifecycleHook<ContainerBuildInfrastructure>();
        return builder.ExecutionContext.IsRunMode

            // Return a builder that isn't added to the top-level application builder so it doesn't surface as a resource.
            ? builder.CreateResourceBuilder(resource)

            // Add the resource.
            : builder.AddResource(resource);
    }
}