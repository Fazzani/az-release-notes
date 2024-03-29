﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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

        internal static WorkItemType WorkItemTypeFromString(string type)
        {
            return type switch
            {
                "User Story" => WorkItemType.Us,
                "Bug" => WorkItemType.Bug,
                _ => WorkItemType.Us,
            };
        }

        public static bool IsSprintRelease(this string version) => !string.IsNullOrEmpty(version) && new Regex(@"v\d+\.\d+\.0").IsMatch(version);
    }
}
