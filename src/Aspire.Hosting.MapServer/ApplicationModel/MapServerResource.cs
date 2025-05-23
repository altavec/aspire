﻿// -----------------------------------------------------------------------
// <copyright file="MapServerResource.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// The <c>MapServer</c> <see cref="ContainerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
public sealed class MapServerResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery;