using System.Security.Cryptography;

namespace SakilaRentalApp.Utils
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Genera un hash criptográfico PBKDF2 a partir de una contraseña en texto plano y retorna el Hash y Salt en Base64.
        /// </summary>
        public static (string Hash, string Salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithm,
                KeySize
            );

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        /// <summary>
        /// Verifica si una contraseña ingresada coincide con el hash y salt almacenados en la base de datos.
        /// </summary>
        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(storedSalt);
                byte[] expectedHash = Convert.FromBase64String(storedHash);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    enteredPassword,
                    salt,
                    Iterations,
                    HashAlgorithm,
                    KeySize
                );

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
