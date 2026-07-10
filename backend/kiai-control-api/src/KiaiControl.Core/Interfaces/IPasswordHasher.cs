namespace KiaiControl.Core.Interfaces;

public interface IPasswordHasher
{
    string Hash(string input);

    bool Verify(string input, string hash);
}
