// -----------------------------------------------------------------------
// <copyright file="GrpcUIContainerImageTags.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting.Grpc;

/// <summary>
/// The <c>grpcui</c> container image tags.
/// </summary>
internal static class GrpcUIContainerImageTags
{
    /// <summary>
    /// The registry.
    /// </summary>
    public const string Registry = "docker.io";

    /// <summary>
    /// The image.
    /// </summary>
    public const string Image = "fullstorydev/grpcui";

    /// <summary>
    /// The tag.
    /// </summary>
    public const string Tag = "v1.4.3";
}