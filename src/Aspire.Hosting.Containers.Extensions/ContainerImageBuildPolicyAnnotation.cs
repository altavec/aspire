// -----------------------------------------------------------------------
// <copyright file="ContainerImageBuildPolicyAnnotation.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting;

/// <summary>
/// Annotation that controls the image build policy for a container resource.
/// </summary>
public class ContainerImageBuildPolicyAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the image build policy for the container resource.
    /// </summary>
    public required ImageBuildPolicy ImageBuildPolicy { get; set; }
}