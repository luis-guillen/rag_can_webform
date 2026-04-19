using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace rag_can_aspx.Services
{
    public static class DuplicateDetector
    {
        public static string Sha256Hex(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string normalizado = Regex.Replace(text.Trim().ToLowerInvariant(), @"\s+", " ");
            byte[] bytes = Encoding.UTF8.GetBytes(normalizado);

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(64);
                foreach (byte b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        public static string FindCanonical(string sha, IEnumerable<PageMetadata> existing)
        {
            if (string.IsNullOrEmpty(sha))
                return null;

            var canonical = existing.FirstOrDefault(e =>
                e.Sha256 == sha && e.DuplicateOf == null);

            return canonical?.File;
        }
    }
}
