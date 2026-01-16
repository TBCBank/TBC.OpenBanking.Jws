// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

/// <summary>
/// Unit tests for the <see cref="JwsConstants"/> class.
/// Tests JWS constant values.
/// </summary>
public class JwsConstantsTests
{
    [Fact]
    public void RequestIDHeaderName_HasExpectedValue()
    {
        // Assert
        Assert.Equal("x-request-id", JwsConstants.RequestIDHeaderName);
    }

    [Fact]
    public void DigestHeadertName_HasExpectedValue()
    {
        // Assert
        Assert.Equal("digest", JwsConstants.DigestHeadertName);
    }

    [Fact]
    public void SignatureHeaderName_HasExpectedValue()
    {
        // Assert
        Assert.Equal("x-jws-signature", JwsConstants.SignatureHeaderName);
    }

    [Fact]
    public void HeadersNecessity_Enum_ContainsExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(JwsConstants.HeadersNecessity.Mandatory));
        Assert.True(Enum.IsDefined(JwsConstants.HeadersNecessity.IfExists));
    }

    [Fact]
    public void HeadersNecessity_Mandatory_HasCorrectValue()
    {
        // Assert
        Assert.Equal(0, (int)JwsConstants.HeadersNecessity.Mandatory);
    }

    [Fact]
    public void HeadersNecessity_IfExists_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)JwsConstants.HeadersNecessity.IfExists);
    }
}
