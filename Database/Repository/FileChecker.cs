using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class FileChecker : IFileChecker
    {
        private static readonly Regex DataUriPattern = new(
            @"^data:(?<mimeType>[a-zA-Z0-9\-\/]+);base64,(?<data>[a-zA-Z0-9+/=]+)$",
            RegexOptions.Compiled);
        public string ExtractMimeType(string base64Data)
        {
            if (string.IsNullOrWhiteSpace(base64Data))
            {
                throw new ArgumentException("Base64 data cannot be null or empty.", nameof(base64Data));
            }

            var match = DataUriPattern.Match(base64Data);
            if (!match.Success)
            {
                throw new ArgumentException("Invalid data URI format.", nameof(base64Data));
            }
            return match.Groups["mimeType"].Value;
        }

        public string RemoveBase64Header(string base64Data)
        {
            if (string.IsNullOrWhiteSpace(base64Data))
            {
                throw new ArgumentException("Base64 data cannot be null or empty.", nameof(base64Data));
            }

            var commaIndex = base64Data.IndexOf(',');
            if (commaIndex < 0)
            {
                throw new ArgumentException("Invalid data URI format; missing comma separator.", nameof(base64Data));
            }

            return base64Data.Substring(commaIndex + 1);
        }
    }
}
