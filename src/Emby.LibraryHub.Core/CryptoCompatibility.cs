using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Emby.LibraryHub.Core;

/// <summary>Provides token encoding and cryptographic operations using .NET Standard 2.0 APIs.</summary>
internal static class CryptoCompatibility
{
    /// <summary>Returns cryptographically random bytes, without depending on newer static RNG overloads.</summary>
    internal static byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
        return bytes;
    }

    /// <summary>Computes a SHA-256 confirmation-token digest.</summary>
    internal static byte[] Sha256(byte[] bytes)
    {
        using (var hash = SHA256.Create()) return hash.ComputeHash(bytes);
    }

    /// <summary>Computes an HMAC-SHA256 form proof with the persisted secret key.</summary>
    internal static byte[] HmacSha256(byte[] key, byte[] bytes)
    {
        using (var mac = new HMACSHA256(key)) return mac.ComputeHash(bytes);
    }

    /// <summary>Encodes uppercase hexadecimal, matching Convert.ToHexString without separators.</summary>
    internal static string ToHex(byte[] bytes)
    {
        const string digits = "0123456789ABCDEF";
        var characters = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            characters[i * 2] = digits[bytes[i] >> 4];
            characters[i * 2 + 1] = digits[bytes[i] & 15];
        }
        return new string(characters);
    }

    /// <summary>Decodes persisted hexadecimal keys; malformed input fails instead of changing the key.</summary>
    internal static byte[] FromHex(string value)
    {
        if (value.Length % 2 != 0) throw new FormatException("Hexadecimal input must have an even length.");
        var bytes = new byte[value.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)((HexDigit(value[i * 2]) << 4) | HexDigit(value[i * 2 + 1]));
        return bytes;
    }

    private static int HexDigit(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        throw new FormatException("Invalid hexadecimal character.");
    }

    /// <summary>Compares equal-length secrets without exiting early on different byte values.</summary>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        var difference = 0;
        for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
        return difference == 0;
    }
}
