using System.Security.Cryptography;
using System.Text;

namespace BlockGame.net;

/** password hashing and verification for server auth */
public static class ServerAuth {
    private const int SaltSize = 16;  // 128 bits
    private const int HashSize = 32;  // 256 bits
    private const int Iterations = 100000;  // PBKDF2 iterations

    /** hash a password using PBKDF2 with a random salt */
    public static string hashPassword(string password) {
        // generate random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // hash password
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        // combine salt + hash and encode as base64
        var combined = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, combined, 0, SaltSize);
        Array.Copy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    /** verify a password against a stored hash */
    public static bool verifyPassword(string password, string storedHash) {
        try {
            var combined = Convert.FromBase64String(storedHash);

            if (combined.Length != SaltSize + HashSize) {
                return false;
            }

            // extract salt and hash
            var salt = new byte[SaltSize];
            var hash = new byte[HashSize];
            Array.Copy(combined, 0, salt, 0, SaltSize);
            Array.Copy(combined, SaltSize, hash, 0, HashSize);

            // hash input password with same salt
            var testHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            // constant-time comparison
            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
        catch {
            return false;
        }
    }
}