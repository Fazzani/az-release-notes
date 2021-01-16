using System;
using System.IO;
using System.Text;

namespace ReleaseNotes
{
    public static class Extensions
    {
        public static Stream ConvertToBase64(this Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return new MemoryStream(Encoding.UTF8.GetBytes(base64));
        }
    }
}
