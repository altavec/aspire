// -----------------------------------------------------------------------
// <copyright file="ZScaler.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aspire.Hosting;

/// <summary>
/// Helpers for <see href="https://www.zscaler.com/" />.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API")]
public static class ZScaler
{
    /// <summary>
    /// Gets a value indicating whether <see cref="ZScaler" /> is running.
    /// </summary>
    /// <returns><see langword="true" /> if <see cref="ZScaler" /> is running; otherwise <see langword="false" />.</returns>
    public static bool IsRunning => OperatingSystem.IsWindows() && System.Diagnostics.Process.GetProcesses().Any(process => process is { ProcessName: "ZSAService" or "ZSATunnel" });

    /// <summary>
    /// Gets the docker file lines to insert the <see cref="ZScaler" /> certificate.
    /// </summary>
    /// <returns>The docker file lines.</returns>
    public static IEnumerable<string> GetContainerfileLines()
    {
        var first = true;
        string? lineToWrite = default;

        foreach (var line in GetLines("zscaler.sh").Skip(1))
        {
            if (lineToWrite is not null)
            {
                yield return GetLine(lineToWrite, ref first);
                lineToWrite = null;
            }

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            lineToWrite = line;
        }

        if (lineToWrite is not null)
        {
            if (first)
            {
                yield return "RUN " + lineToWrite;
            }
            else
            {
                yield return "    " + lineToWrite;
            }
        }

        static string GetLine(string line, ref bool first)
        {
            if (first)
            {
                first = false;
                return "RUN " + AppendLineContinuation(line);
            }

            return "    " + AppendLineContinuation(line);

            static string AppendLineContinuation(string line)
            {
                return line.StartsWith('#') || line.EndsWith("&& \\", StringComparison.Ordinal)
                    ? line
                    : line + " && \\";
            }
        }
    }

    /// <summary>
    /// Exports the certificates to the file glob.
    /// </summary>
    /// <param name="path">The file format, with placeholders for the certificate index.</param>
    /// <returns>The asynchronous task.</returns>
    public static async Task ExportCertificateTo(string path)
    {
        var index = 0;
        await foreach (string pem in GetCertificatesAsync().ConfigureAwait(continueOnCapturedContext: false))
        {
            await File.WriteAllTextAsync(string.Format(System.Globalization.CultureInfo.CurrentCulture, path, ++index), pem).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    /// <summary>
    /// Gets the root certificate.
    /// </summary>
    /// <returns>The root certificate if found; otherwise <see langword="null" />.</returns>
    public static async IAsyncEnumerable<string> GetCertificatesAsync()
    {
        if (await GetCertificateAsync("zscaler.com").ConfigureAwait(continueOnCapturedContext: false) is { } certificate)
        {
            var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
            chain.Build(certificate);

            foreach (var item in chain.ChainElements.Skip(1))
            {
                yield return item.Certificate.ExportCertificatePem();
            }
        }
    }

    private static async Task<System.Security.Cryptography.X509Certificates.X509Certificate2?> GetCertificateAsync(string domain, int port = 443)
    {
        using var client = new System.Net.Sockets.TcpClient(domain, port);
        var sslStream = GetSslStream(client);
        await using (sslStream.ConfigureAwait(false))
        {
            await sslStream.AuthenticateAsClientAsync(domain).ConfigureAwait(continueOnCapturedContext: false);
            return sslStream.RemoteCertificate is { } serverCertificate ? new(serverCertificate) : null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "Checked")]
        static bool CertCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate? certificate, System.Security.Cryptography.X509Certificates.X509Chain? chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "Checked")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Vulnerability", "S4830:Server certificates should be verified during SSL/TLS connections", Justification = "Checked")]
        static System.Net.Security.SslStream GetSslStream(System.Net.Sockets.TcpClient tcpClient)
        {
            return new(tcpClient.GetStream(), leaveInnerStreamOpen: true, CertCallback);
        }
    }

    private static IEnumerable<string> GetLines(string name)
    {
        using StreamReader reader = new StreamReader(GetManifestResourceStream(name));
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    private static Stream GetManifestResourceStream(string name) => typeof(ZScaler).Assembly.GetManifestResourceStream(typeof(ZScaler), name) ?? throw new InvalidOperationException();
}