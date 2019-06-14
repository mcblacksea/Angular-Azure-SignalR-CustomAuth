namespace SignalRCustomAuthServer.Extensions {

    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    public static class PasswordHashing {

        static uint ReadNetworkByteOrder(Byte[] buffer, Int32 offset) {
            return ((uint)(buffer[offset + 0]) << 24)
                | ((uint)(buffer[offset + 1]) << 16)
                | ((uint)(buffer[offset + 2]) << 8)
                | ((uint)(buffer[offset + 3]));
        }

        static void WriteNetworkByteOrder(Byte[] buffer, Int32 offset, uint value) {
            buffer[offset + 0] = (Byte)(value >> 24);
            buffer[offset + 1] = (Byte)(value >> 16);
            buffer[offset + 2] = (Byte)(value >> 8);
            buffer[offset + 3] = (Byte)(value >> 0);
        }

        public static String HashPassword(String password) {
            var prf = KeyDerivationPrf.HMACSHA256;
            var rng = RandomNumberGenerator.Create();
            const Int32 iterCount = 10000;
            const Int32 saltSize = 128 / 8;
            const Int32 numBytesRequested = 256 / 8;

            // Produce a version 3 (see comment above) text hash.
            var salt = new Byte[saltSize];
            rng.GetBytes(salt);
            var subkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, numBytesRequested);

            var outputBytes = new Byte[13 + salt.Length + subkey.Length];
            outputBytes[0] = 0x01; // format marker
            WriteNetworkByteOrder(outputBytes, 1, (uint)prf);
            WriteNetworkByteOrder(outputBytes, 5, iterCount);
            WriteNetworkByteOrder(outputBytes, 9, saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);
            return Convert.ToBase64String(outputBytes);
        }

        public static Boolean VerifyHashedPassword(String hashedPassword, String providedPassword) {
            var decodedHashedPassword = Convert.FromBase64String(hashedPassword);

            // Wrong version
            if (decodedHashedPassword[0] != 0x01) {
                return false;
            }

            // Read header information
            var prf = (KeyDerivationPrf)ReadNetworkByteOrder(decodedHashedPassword, 1);
            var iterCount = (Int32)ReadNetworkByteOrder(decodedHashedPassword, 5);
            var saltLength = (Int32)ReadNetworkByteOrder(decodedHashedPassword, 9);

            // Read the salt: must be >= 128 bits
            if (saltLength < 128 / 8) {
                return false;
            }
            var salt = new Byte[saltLength];
            Buffer.BlockCopy(decodedHashedPassword, 13, salt, 0, salt.Length);

            // Read the subkey (the rest of the payload): must be >= 128 bits
            var subkeyLength = decodedHashedPassword.Length - 13 - salt.Length;
            if (subkeyLength < 128 / 8) {
                return false;
            }
            var expectedSubkey = new Byte[subkeyLength];
            Buffer.BlockCopy(decodedHashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

            // Hash the incoming password and verify it
            var actualSubkey = KeyDerivation.Pbkdf2(providedPassword, salt, prf, iterCount, subkeyLength);
            return actualSubkey.SequenceEqual(expectedSubkey);
        }
    }
}
