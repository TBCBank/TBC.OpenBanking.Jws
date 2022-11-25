/*
 * Copyright (c) 2021 JSC TBC Bank
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#nullable enable

namespace TBC.OpenBanking.Jws;

using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

/// <summary>
///     Provides ability to have X.509 certificate URLs in options classes.
/// </summary>
/// <example>
///     <para>File URL: <c>pfx://Password123@localhost/C:\Secrets\Certificate.pfx</c></para>
///     <para>Passwordless File URL: <c>pfx:///C:\Secrets\Certificate.pfx</c></para>
///     <para>Store URL: <c>cert:///CurrentUser/My/e1d1185caf20dd16359f99cb8f99ca6b60e3c1a6</c></para>
/// </example>
[TypeConverter(typeof(Converter))]
public sealed class X509CertificateLocator : IDisposable
{
    private static volatile uint IsRegistered;
    private static readonly char[] TrimSlashes = new[] { '\\', '/' };

    private const string CertScheme = "cert";
    private const string PfxScheme = "pfx";
    private const string StoreScheme = "store";

    private readonly bool _isMissing;
    private readonly bool _isPfxFile;
    private readonly bool _isCertStore;

    private Uri? _uri;
    private X509Certificate2? _certificate;

    /// <summary>
    ///     Initializes a new instance of the <see cref="X509CertificateLocator"/> class.
    /// </summary>
    /// <param name="uri">
    ///     The X.509 certificate URL.
    /// </param>
    public X509CertificateLocator(Uri? uri)
    {
        if (uri is null)
        {
            // Allow null URL; this means we aren't using certificate
            // (This is NOT the same as invalid URL)
            _isMissing = true;
            return;
        }

        string scheme = uri.Scheme;

        if (string.Equals(scheme, PfxScheme, StringComparison.OrdinalIgnoreCase))
        {
            _isPfxFile = true;
        }
        else if (string.Equals(scheme, CertScheme, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, StoreScheme, StringComparison.OrdinalIgnoreCase))
        {
            _isCertStore = true;
        }
        else
        {
            throw new UriFormatException("Incorrect X.509 certificate URL scheme. Must be either cert://, store:// or pfx://");
        }

        _uri = uri;

        // TODO: Move more validations here from GetCertificate() method

        if (_isPfxFile)
        {
            //
            // Get a normalized full local path:
            // (If path is relative, this will use current directory as a base path)
            //
            var fullPath = Path.GetFullPath(_uri.LocalPath.Substring(1));
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The PFX file was not found", fullPath);
            }
        }
        else if (_isCertStore)
        {
            if (uri.Segments is null || uri.Segments.Length != 4)
            {
                throw new UriFormatException("X.509 certificate URL is not formatted correctly");
            }
        }
    }

    public Uri? Uri => _uri;

    public static X509CertificateLocator Create(Uri uri) => new(uri);

    /// <summary>
    ///     Performs the actual lookup and retrieval of the X.509 certificate.
    /// </summary>
    /// <returns>
    ///     The instance of <see cref="X509Certificate2"/> class.
    /// </returns>
    public X509Certificate2? GetCertificate()
    {
        if (_isMissing)
        {
            return null;
        }

        if (_certificate != null)
        {
            return _certificate;
        }

        if (_isPfxFile)
        {
            //
            // SChannel (TLS infrastructure on Windows) does not support ephemeral keys ("in-memory");
            // keys must be persisted somewhere; put them in current user's store:
            //
            const X509KeyStorageFlags keyFlags = X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet;

            //
            // Get a normalized full local path:
            // (If path is relative, this will use current directory as the base path)
            //
            var fullPath = Path.GetFullPath(_uri!.LocalPath.Substring(1));
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The PFX file was not found", fullPath);
            }

            var userInfo = _uri.UserInfo;
            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                // It is URL-encoded; decode:
                userInfo = WebUtility.UrlDecode(userInfo);
                string? password;

#if NETCOREAPP3_1_OR_GREATER
                if (userInfo.Contains(':', StringComparison.Ordinal))
#else
                if (userInfo.Contains(":"))
#endif
                {
                    // 2nd element is the password
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
                    password = userInfo.Split(':', StringSplitOptions.None)[1];
#else
                    password = userInfo.Split(new char[] { ':' }, StringSplitOptions.None)[1];
#endif
                }
                else
                {
                    // Treat entire UserInfo as password
                    password = userInfo;
                }

                _certificate = new X509Certificate2(fullPath, password, keyFlags);
            }
            else
            {
                _certificate = new X509Certificate2(fullPath, (string?)null, keyFlags);
            }
        }
        else if (_isCertStore)
        {
            if (_uri!.Segments is null || _uri.Segments.Length != 4)
            {
                throw new UriFormatException("X.509 certificate URL is not formatted correctly");
            }

            var storeName = WebUtility.UrlDecode(_uri.Segments[2].Trim(TrimSlashes).Trim());

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            var storeLocation = Enum.Parse<StoreLocation>(WebUtility.UrlDecode(_uri.Segments[1].Trim(TrimSlashes).Trim()), true);
            using var store = new X509Store(storeName, storeLocation, OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
#else
            var storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), WebUtility.UrlDecode(_uri.Segments[1].Trim(TrimSlashes).Trim()), true);
            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
#endif
            var certs = store.Certificates;  // Getting this property creates a new copy of the entire collection
            try
            {
                string thumbprint = WebUtility.UrlDecode(_uri!.Segments[3].Trim(TrimSlashes).Trim());

                var found = certs.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                try
                {
                    if (found is null || found.Count == 0)
                    {
                        throw new InvalidOperationException("Certificate was not found");  // TODO: Better exception message
                    }

                    if (found.Count > 1)
                    {
                        // This can not happen if we use thumbprint
                        throw new InvalidOperationException("Multiple certificates were found");  // TODO: Better exception message
                    }

                    // TODO: Validate HasPrivateKey and throw error if false?

                    // Create a new instance because the original certificate will be reset
                    _certificate = new X509Certificate2(found[0]);
                }
                finally
                {
                    Reset(found);
                }
            }
            finally
            {
                Reset(certs);
            }
        }

        return _certificate;
    }

    public override string ToString() => _isMissing ? string.Empty : _uri!.ToString();

    public void Dispose()
    {
        _certificate?.Dispose();
        _certificate = null;
        _uri = null;
    }

    private static void Reset(X509Certificate2Collection? collection)
    {
        if (collection != null)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].Dispose();
            }

            collection.Clear();
        }
    }

    /// <summary>
    ///     Registers <see cref="UriParser"/>s and <see cref="TypeConverter"/> for X.509 certificate URLs.
    /// </summary>
    /// <remarks>
    ///     Should be called only once per application lifetime. Call from Program.cs or Startup.cs.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Synchronized)]
    public static void RegisterUriParsers()
    {
        if (IsRegistered == 0u)
        {
            UriParser.Register(new CertUriParser(), CertScheme, -1);
            UriParser.Register(new PfxUriParser(), PfxScheme, -1);
            UriParser.Register(new CertUriParser(), StoreScheme, -1);

            IsRegistered = 1u;
        }
    }

    private sealed class PfxUriParser : GenericUriParser
    {
        public PfxUriParser()
            : base(GenericUriParserOptions.AllowEmptyAuthority
                  | GenericUriParserOptions.NoFragment
                  | GenericUriParserOptions.NoPort)
        {
        }
    }

    private sealed class CertUriParser : GenericUriParser
    {
        public CertUriParser()
            : base(GenericUriParserOptions.AllowEmptyAuthority
                  | GenericUriParserOptions.DontCompressPath
                  | GenericUriParserOptions.DontConvertPathBackslashes
                  | GenericUriParserOptions.DontUnescapePathDotsAndSlashes
                  | GenericUriParserOptions.NoFragment
                  | GenericUriParserOptions.NoPort)
        {
        }
    }

    private sealed class Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(string) || sourceType == typeof(Uri);

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
            destinationType == typeof(string) || destinationType == typeof(Uri);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            value switch
            {
                string s => Create(new Uri(s, UriKind.Absolute)),
                Uri u => Create(u),
                _ => throw new FormatException("Unable to parse X.509 certificate URL"),
            };

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type? destinationType)
        {
            if (value is X509CertificateLocator c)
            {
                return destinationType switch
                {
                    Type st when st == typeof(string) => c.ToString(),
                    Type ut when ut == typeof(Uri) => c._isMissing ? null : c._uri!,
                    _ => throw new FormatException(),
                };
            }

            throw new FormatException($"Unable to format X.509 URL as '{destinationType?.Name ?? "<unknown>"}'");
        }
    }
}
