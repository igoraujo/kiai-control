using KiaiControl.Api.Configuration;

namespace KiaiControl.UnitTests;

public sealed class JwtOptionsTests
{
    [Fact]
    public void ResolveSigningKey_ShouldRejectMissingProductionKey()
    {
        var options = new JwtOptions { Issuer = "issuer", Audience = "audience" };

        var exception = Assert.Throws<InvalidOperationException>(() => options.ResolveSigningKey(isDevelopment: false));

        Assert.Contains("Jwt:SigningKey", exception.Message);
    }

    [Fact]
    public void ResolveSigningKey_ShouldUseDevelopmentKeyOnlyWhenDevelopment()
    {
        var options = new JwtOptions();

        var key = options.ResolveSigningKey(isDevelopment: true);

        Assert.True(System.Text.Encoding.UTF8.GetByteCount(key) >= 32);
    }

    [Fact]
    public void ResolveSigningKey_ShouldRejectShortKeys()
    {
        var options = new JwtOptions { SigningKey = "too-short" };

        Assert.Throws<InvalidOperationException>(() => options.ResolveSigningKey(isDevelopment: true));
    }
}