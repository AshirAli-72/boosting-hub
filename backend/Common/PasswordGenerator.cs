using System.Security.Cryptography;

namespace BoostingHub.backend.Common;

public static class PasswordGenerator
{
    public static string Generate(int length = 16)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var all = upper + lower + digits + special;
        var chars = new char[length];
        var rng = RandomNumberGenerator.Create();

        chars[0] = upper[Random.Shared.Next(upper.Length)];
        chars[1] = lower[Random.Shared.Next(lower.Length)];
        chars[2] = digits[Random.Shared.Next(digits.Length)];
        chars[3] = special[Random.Shared.Next(special.Length)];

        for (int i = 4; i < length; i++)
        {
            var bytes = new byte[1];
            rng.GetBytes(bytes);
            chars[i] = all[bytes[0] % all.Length];
        }

        return new string(chars.OrderBy(_ => Random.Shared.Next()).ToArray());
    }

    public static string GenerateOtp(int length = 6)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return string.Concat(bytes.Select(b => (b % 10).ToString()));
    }
}
