// -----------------------------------------------------------------------
// <copyright file="ContainerBuildPublishingContext.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Aspire.Hosting.Containers;

using Microsoft.Extensions.Logging;

/// <summary>
/// Represents a context for building containers for a distributed application.
/// </summary>
internal sealed class ContainerBuildPublishingContext(
    DistributedApplicationExecutionContext executionContext,
    Publishing.IResourceContainerImageBuilder imageBuilder,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Builds the images.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The task.</returns>
    internal async Task BuildAsync(DistributedApplicationModel model, ContainerBuildEnvironmentResource environment)
    {
        if (!executionContext.IsPublishMode)
        {
            logger.NotInPublishingMode();
            return;
        }

        logger.StartBuildingContainers();

        ArgumentNullException.ThrowIfNull(model);

        await this.BuildCoreAsync(model, environment).ConfigureAwait(false);

        logger.FinishBuildingContainers();
    }

    private async Task BuildCoreAsync(DistributedApplicationModel model, ContainerBuildEnvironmentResource environment)
    {
        IEnumerable<IResource> resources = model.Resources;

        foreach (var resource in resources)
        {
            if (resource.GetDeploymentTargetAnnotation(environment)?.DeploymentTarget is ContainerBuildServiceResource serviceResource)
            {
                await imageBuilder.BuildImageAsync(serviceResource.TargetResource, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}