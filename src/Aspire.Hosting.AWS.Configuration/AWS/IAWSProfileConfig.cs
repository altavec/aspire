// -----------------------------------------------------------------------
// <copyright file="IAWSProfileConfig.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting.AWS;

/// <summary>
/// The AWS configuration file.
/// </summary>
public interface IAWSProfileConfig : ApplicationModel.IResource
{
    /// <summary>
    /// Gets the profiles.
    /// </summary>
    IList<AWSProfile> Profiles { get; }
}