// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Unit tests for the <see cref="CertificateValidationFlags"/> class.
/// Tests certificate validation flag configuration.
/// </summary>
public class CertificateValidationFlagsTests
{
    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Arrange & Act
        var flags = new CertificateValidationFlags();

        // Assert
        Assert.Equal(X509RevocationMode.Online, flags.RevocationMode);
        Assert.Equal(X509RevocationFlag.ExcludeRoot, flags.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, flags.VerificationFlags);
    }

    [Fact]
    public void RevocationMode_CanBeSetAndRetrieved()
    {
        // Arrange
        var flags = new CertificateValidationFlags();

        // Act
        flags.RevocationMode = X509RevocationMode.NoCheck;

        // Assert
        Assert.Equal(X509RevocationMode.NoCheck, flags.RevocationMode);
    }

    [Fact]
    public void RevocationFlag_CanBeSetAndRetrieved()
    {
        // Arrange
        var flags = new CertificateValidationFlags();

        // Act
        flags.RevocationFlag = X509RevocationFlag.EntireChain;

        // Assert
        Assert.Equal(X509RevocationFlag.EntireChain, flags.RevocationFlag);
    }

    [Fact]
    public void VerificationFlags_CanBeSetAndRetrieved()
    {
        // Arrange
        var flags = new CertificateValidationFlags();

        // Act
        flags.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

        // Assert
        Assert.Equal(X509VerificationFlags.AllowUnknownCertificateAuthority, flags.VerificationFlags);
    }

    [Fact]
    public void Reset_RestoresDefaultValues()
    {
        // Arrange
        var flags = new CertificateValidationFlags
        {
            RevocationMode = X509RevocationMode.NoCheck,
            RevocationFlag = X509RevocationFlag.EntireChain,
            VerificationFlags = X509VerificationFlags.AllFlags
        };

        // Act
        flags.Reset();

        // Assert
        Assert.Equal(X509RevocationMode.Online, flags.RevocationMode);
        Assert.Equal(X509RevocationFlag.ExcludeRoot, flags.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, flags.VerificationFlags);
    }

    [Theory]
    [InlineData(X509RevocationMode.NoCheck)]
    [InlineData(X509RevocationMode.Online)]
    [InlineData(X509RevocationMode.Offline)]
    public void RevocationMode_AllValues_CanBeSet(X509RevocationMode mode)
    {
        // Arrange
        var flags = new CertificateValidationFlags();

        // Act
        flags.RevocationMode = mode;

        // Assert
        Assert.Equal(mode, flags.RevocationMode);
    }

    [Theory]
    [InlineData(X509RevocationFlag.EndCertificateOnly)]
    [InlineData(X509RevocationFlag.EntireChain)]
    [InlineData(X509RevocationFlag.ExcludeRoot)]
    public void RevocationFlag_AllValues_CanBeSet(X509RevocationFlag flag)
    {
        // Arrange
        var flags = new CertificateValidationFlags();

        // Act
        flags.RevocationFlag = flag;

        // Assert
        Assert.Equal(flag, flags.RevocationFlag);
    }

    [Fact]
    public void VerificationFlags_CombinedFlags_CanBeSet()
    {
        // Arrange
        var flags = new CertificateValidationFlags();
        var combinedFlags = X509VerificationFlags.AllowUnknownCertificateAuthority |
                           X509VerificationFlags.IgnoreNotTimeValid;

        // Act
        flags.VerificationFlags = combinedFlags;

        // Assert
        Assert.Equal(combinedFlags, flags.VerificationFlags);
        Assert.True(flags.VerificationFlags.HasFlag(X509VerificationFlags.AllowUnknownCertificateAuthority));
        Assert.True(flags.VerificationFlags.HasFlag(X509VerificationFlags.IgnoreNotTimeValid));
    }
}
