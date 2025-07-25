﻿// -----------------------------------------------------------------------
// <copyright file="ResourceBuilderExtensions.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// The resource builder extensions.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API")]
public static partial class ResourceBuilderExtensions
{
    private const string DefaultProfileName = "default";

    /// <summary>
    /// Adds the AWS configuration to the application.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>The resource containing the configuration.</returns>
    public static IResourceBuilder<AWS.IAWSProfileConfig> AddAWSProfileConfig(this IDistributedApplicationBuilder builder, string? name = default)
    {
        var profiles = builder
            .AddResource<AWS.IAWSProfileConfig>(new AWS.AWSProfileConfig { Name = name ?? "aws-config" })
            .WithInitialState(new()
            {
                ResourceType = "Configuration",
                Properties = [],
                State = new("Configuring", KnownResourceStates.Starting),
            });

        // add the configuration to the resource
        _ = builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
        {
            if (!profiles.Resource.TryGetLastAnnotation<AWSConfigurationFileAnnotation>(out var annotation)
                || annotation.FileName is not { } fileName)
            {
                return Task.CompletedTask;
            }

            // set the AWS Profiles location
            Amazon.AWSConfigs.AWSProfilesLocation = fileName;

            // set the environment variable
            Environment.SetEnvironmentVariable(Amazon.Runtime.CredentialManagement.SharedCredentialsFile.SharedCredentialsFileEnvVar, fileName, EnvironmentVariableTarget.Process);
            RefreshEnvironmentVariables(builder.Configuration);

            // set the profiles location for the .NET setup
            _ = builder.Configuration.AddInMemoryCollection([new("AWS:ProfilesLocation", fileName)]);

            return Task.CompletedTask;
        });

        return profiles;
    }

    /// <summary>
    /// Sets the AWS config for the builder application.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The application builder.</returns>
    public static IDistributedApplicationBuilder SetAWSConfig(this IDistributedApplicationBuilder builder, AWS.IAWSSDKConfig configuration)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (configuration.Profile is { } profile
            && !string.Equals(builder.Configuration.GetValue<string>("AWS:Profile"), profile, StringComparison.Ordinal))
        {
            dictionary.Add("AWS:Profile", profile);
            Amazon.AWSConfigs.AWSProfileName = profile;
            Environment.SetEnvironmentVariable("AWS_PROFILE", profile, EnvironmentVariableTarget.Process);
        }

        if (configuration.Region is { SystemName: var region }
            && !string.Equals(builder.Configuration.GetValue<string>("AWS:Region"), region, StringComparison.Ordinal))
        {
            dictionary.Add("AWS:Region", region);
            Amazon.AWSConfigs.AWSRegion = region;
            Environment.SetEnvironmentVariable("AWS_REGION", region, EnvironmentVariableTarget.Process);
        }

        if (dictionary.Count is > 0)
        {
            RefreshEnvironmentVariables(builder.Configuration);
            _ = builder.Configuration.AddInMemoryCollection(dictionary);
        }

        return builder;
    }

    /// <summary>
    /// Adds a profile to the <see cref="AWS.IAWSProfileConfig"/>.
    /// </summary>
    /// <typeparam name="T">The type of configuration.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the profile.</param>
    /// <param name="accessKeyId">The Access Key ID parameter.</param>
    /// <param name="secretAccessKey">The Secret Access Key parameter.</param>
    /// <param name="secretToken">The Secret Token parameter.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithProfile<T>(
        this IResourceBuilder<T> builder,
        string name,
        IResourceBuilder<ParameterResource>? accessKeyId = null,
        IResourceBuilder<ParameterResource>? secretAccessKey = null,
        IResourceBuilder<ParameterResource>? secretToken = null)
        where T : AWS.IAWSProfileConfig
    {
        var accessKeyIdParameter = accessKeyId?.Resource ?? RemoveSecret(ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder.ApplicationBuilder, $"{name}-access-key-id"));
        var secretAccessKeyParameter = secretAccessKey?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder.ApplicationBuilder, $"{name}-secret-access-key");
        builder.Resource.Profiles.Add(new() { Name = name, AccessKeyId = accessKeyIdParameter, SecretAccessKey = secretAccessKeyParameter, SessionToken = secretToken?.Resource });
        return builder;

        static ParameterResource RemoveSecret(ParameterResource parameterResource)
        {
            if (GetBackingField(parameterResource.GetType().GetProperty(nameof(parameterResource.Secret))) is { } fieldInfo)
            {
                fieldInfo.SetValue(parameterResource, value: false);
            }

            return parameterResource;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Checked")]
            static System.Reflection.FieldInfo? GetBackingField(System.Reflection.PropertyInfo? pi)
            {
                if (pi?.CanRead is not true || pi.GetGetMethod(nonPublic: true)?.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: true) is not true)
                {
                    return null;
                }

                if (pi.DeclaringType?.GetField($"<{pi.Name}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is { } backingField
                    && backingField.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: true))
                {
                    return backingField;
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Sets the <see cref="AWS.IAWSProfileConfig"/> to be written out as a config file.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> AsConfigurationFile<T>(this IResourceBuilder<T> builder)
        where T : AWS.IAWSProfileConfig
    {
        _ = builder.WithAnnotation(new AWSConfigurationFileAnnotation(builder.Resource), ResourceAnnotationMutationBehavior.Replace);

        _ = builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((e, _) => ProcessProfiles(e.Services, builder.Resource));

        return builder;

        async Task ProcessProfiles(IServiceProvider services, T configuration)
        {
            // get the annotation
            if (configuration.TryGetLastAnnotation<AWSConfigurationFileAnnotation>(out var fileAnnotation))
            {
                var rns = services.GetRequiredService<ResourceNotificationService>();
                var rls = services.GetRequiredService<ResourceLoggerService>();
                var logger = rls.GetLogger(configuration);

                var fileName = fileAnnotation.FileName;
                if (!Path.Exists(fileName))
                {
                    LogCreatingAwsConfiguration(logger, fileName);
                    var sharedCredentialsFile = new Amazon.Runtime.CredentialManagement.SharedCredentialsFile(fileName);
                    foreach (var profile in GetAllProfiles(configuration.Profiles))
                    {
                        LogRegisteringProfile(logger, profile.Name);
                        sharedCredentialsFile.RegisterProfile(
                            new(
                                profile.Name,
                                new()
                                {
                                    AccessKey = profile.AccessKeyId.Value,
                                    SecretKey = profile.SecretAccessKey.Value,
                                    Token = profile.SessionToken?.Value,
                                }));
                    }

                    LogCompleted(logger);
                }

                await rns.PublishUpdateAsync(configuration, s => s with
                {
                    State = new(KnownResourceStates.Finished, KnownResourceStateStyles.Success),
                    Properties = [
                        .. s.Properties,
                        new(CustomResourceKnownProperties.Source, fileName),
                    ],
                }).ConfigureAwait(false);

                static IEnumerable<AWS.AWSProfile> GetAllProfiles(IEnumerable<AWS.AWSProfile> profiles)
                {
                    bool hasDefault = false;
                    foreach (var profile in profiles)
                    {
                        yield return profile;
                        hasDefault |= string.Equals(profile.Name, DefaultProfileName, StringComparison.Ordinal);
                    }

                    if (!hasDefault)
                    {
                        const string Dummy = nameof(Dummy);
                        yield return new()
                        {
                            Name = DefaultProfileName,
                            AccessKeyId = new($"{DefaultProfileName}-dummy-access-key-id", _ => Dummy, secret: true),
                            SecretAccessKey = new($"{DefaultProfileName}-dummy-secret-access-key", _ => Dummy, secret: true),
                        };
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds the reference to the builder.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The input builder.</returns>
    public static IResourceBuilder<T> WithReference<T>(this IResourceBuilder<T> builder, IResourceBuilder<AWS.IAWSProfileConfig> configuration)
        where T : IResourceWithEnvironment => builder.WithReference(configuration.Resource);

    /// <summary>
    /// Adds the reference to the builder.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The input builder.</returns>
    public static IResourceBuilder<T> WithReference<T>(this IResourceBuilder<T> builder, AWS.IAWSProfileConfig configuration)
        where T : IResourceWithEnvironment
    {
        // add the configuration to the resource
        if (configuration.TryGetLastAnnotation<AWSConfigurationFileAnnotation>(out var fileAnnotation))
        {
            if (GetContainerBuilder(builder) is { } containerBuilder)
            {
                // inject the configuration into the container at the appropriate location
                const string TempPath = "/tmp/";
                var name = Path.GetFileName(fileAnnotation.FileName);
                _ = containerBuilder
                    .WithContainerFiles(TempPath, async (_, cancellationToken) => [new ContainerFile { Name = name, Contents = await File.ReadAllTextAsync(fileAnnotation.FileName, cancellationToken).ConfigureAwait(false) }])
                    .WithEnvironment(callback => callback.EnvironmentVariables[Amazon.Runtime.CredentialManagement.SharedCredentialsFile.SharedCredentialsFileEnvVar] = TempPath + name);
            }
            else
            {
                _ = builder.WithEnvironment(callback => callback.EnvironmentVariables[Amazon.Runtime.CredentialManagement.SharedCredentialsFile.SharedCredentialsFileEnvVar] = fileAnnotation.FileName);
            }
        }

        return builder;

        static IResourceBuilder<ContainerResource>? GetContainerBuilder(IResourceBuilder<T> builder)
        {
            return builder switch
            {
                IResourceBuilder<ContainerResource> containerBuilder => containerBuilder,
                { Resource: ContainerResource container } => builder.ApplicationBuilder.CreateResourceBuilder(container),
                _ => default,
            };
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating AWS configuration at '{FileName}'")]
    private static partial void LogCreatingAwsConfiguration(ILogger logger, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Registering Profile '{Name}'")]
    private static partial void LogRegisteringProfile(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "AWS configuration completed")]
    private static partial void LogCompleted(ILogger logger);

    private static void RefreshEnvironmentVariables(IConfigurationRoot configurationRoot)
    {
        foreach (var provider in configurationRoot.Providers.OfType<Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationProvider>())
        {
            provider.Load();
        }
    }

    private sealed class AWSConfigurationFileAnnotation(AWS.IAWSProfileConfig profileConfig) : IResourceAnnotation
    {
        [field: System.Diagnostics.CodeAnalysis.AllowNull]
        [field: System.Diagnostics.CodeAnalysis.MaybeNull]
        public string FileName => field ??= this.GetFileName();

        private string GetFileName()
        {
            return Path.Combine(Path.GetTempPath(), $"{profileConfig.Name}-{ConvertHashToString(profileConfig.GetHashCode())}");

            static string ConvertHashToString(int hash)
            {
                var bytes = BitConverter.GetBytes(hash);
                return Convert.ToHexString(bytes).Replace("-", string.Empty, StringComparison.Ordinal);
            }
        }
    }
}