using System;
using System.Collections.Generic;

namespace ReleaseNotes
{
    public class SemVer
    {
        public SemVer(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                throw new ArgumentNullException(nameof(tag));

            var _version = tag
                .Replace("refs/tags/v", "", StringComparison.InvariantCultureIgnoreCase)
                .Replace("v", "", StringComparison.InvariantCultureIgnoreCase)
                .Split('.');

            if (_version.Length != 3)
                throw new InvalidOperationException();

            Major = int.Parse(_version[0]);
            Minor = int.Parse(_version[1]);
            Patch = int.Parse(_version[2]);
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
    }

    public class SemVerComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (a == b)
                return 0;

            var aSemVer = new SemVer(a);
            var bSemVer = new SemVer(b);

            if (aSemVer.Major < bSemVer.Major || (aSemVer.Minor < bSemVer.Minor) || aSemVer.Patch < bSemVer.Patch)
                return -1;

            return 1;
        }
    }
}
