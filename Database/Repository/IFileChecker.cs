using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IFileChecker
    {
        string ExtractMimeType(string base64Data);
        string RemoveBase64Header(string base64Data);
    }
}
