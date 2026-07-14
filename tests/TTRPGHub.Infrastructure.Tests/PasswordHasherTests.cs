using TTRPGHub.Auth;

namespace TTRPGHub.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_Succeeds()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        Assert.True(_hasher.Verify("correct-horse-battery-staple", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var hash1 = _hasher.Hash("same-password");
        var hash2 = _hasher.Hash("same-password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_ProducesBCryptFormattedString()
    {
        var hash = _hasher.Hash("some-password");

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Verify_AgainstHashOfDifferentPassword_Fails()
    {
        var hashOfA = _hasher.Hash("password-a");
        var hashOfB = _hasher.Hash("password-b");

        Assert.False(_hasher.Verify("password-a", hashOfB));
        Assert.False(_hasher.Verify("password-b", hashOfA));
    }
}
