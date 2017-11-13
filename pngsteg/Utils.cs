using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pngsteg
{
    public static class Utils
    {
        public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                return CompareFromBeginning(x, y);
            }

            public int GetHashCode(byte[] obj)
            {
                return obj.Select((b) => b.GetHashCode()).Sum();
            }
        }

        public static bool CompareFromBeginning<T>(T[] self, T[] other)
        {
            return CompareFromBeginning<T>(self, other, (a, b) => EqualityComparer<T>.Default.Equals(a, b));
        }
        public static bool CompareFromBeginning<T> (T[] self, T[] other, Func<T, T, bool> comparer)
        {
            long min = Math.Min(self.LongLength, other.LongLength);

            for (long i = 0; i < min; i++)
            {
                bool res = comparer(self[i], other[i]);
                if (res) continue;
                else return false;
            }

            return true;
        }

        public static void CopyFrom<T>(T[] src, T[] dst, long count, long offset = 0)
        {
            for (long i = offset; i < offset + count; i++)
            {
                dst[i-offset] = src[i];
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
