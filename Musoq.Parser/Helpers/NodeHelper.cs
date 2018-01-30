using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Helpers
{
    public static class NodeHelper
    {
        public static IReadOnlyDictionary<(Type, Type), Type> BinaryTypes { get; }

        static NodeHelper()
        {
            var dict = new Dictionary<(Type, Type), Type>();
            BinaryTypes = dict;

            dict.Add((typeof(decimal), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(decimal), typeof(long)), typeof(decimal));
            dict.Add((typeof(decimal), typeof(int)), typeof(decimal));
            dict.Add((typeof(decimal), typeof(short)), typeof(decimal));

            dict.Add((typeof(long), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(long), typeof(long)), typeof(long));
            dict.Add((typeof(long), typeof(int)), typeof(long));
            dict.Add((typeof(long), typeof(short)), typeof(long));

            dict.Add((typeof(int), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(int), typeof(long)), typeof(long));
            dict.Add((typeof(int), typeof(int)), typeof(long));
            dict.Add((typeof(int), typeof(short)), typeof(long));

            dict.Add((typeof(short), typeof(decimal)), typeof(decimal));
            dict.Add((typeof(short), typeof(long)), typeof(long));
            dict.Add((typeof(short), typeof(int)), typeof(long));
            dict.Add((typeof(short), typeof(short)), typeof(long));

            dict.Add((typeof(string), typeof(string)), typeof(string));

            dict.Add((typeof(bool), typeof(bool)), typeof(bool));

            dict.Add((typeof(DateTimeOffset), typeof(DateTimeOffset)), typeof(DateTimeOffset));
        }

        public static Type GetReturnTypeMap(Type left, Type right)
            => BinaryTypes[(left, right)];

        /// <summary>
        ///     Calculates unique identifier for accessed column.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Column unique identifier.</returns>
        public static string CalculateColumnAccessUniqueIdentifier(AccessColumnNode node)
        {
            return $"{node.Name}{node.Span.Start}{node.Span.End}";
        }

        public static string ComputeHash(string content)
        {
            using (var hash = SHA1.Create())
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(content);
                writer.Flush();
                stream.Position = 0;

                var hashedContent = hash.ComputeHash(stream);

                var sb = new StringBuilder(hashedContent.Length * 2);
                foreach (var b in hashedContent)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}