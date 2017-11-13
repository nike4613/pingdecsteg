using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pngsteg.PNG_Decoder
{
    public static class FilterMethodZero
    {
        public enum Type
        {
            None=0,Sub=1,Up=2,Average=3,Paeth=4
        }

        public delegate byte Filter(byte x, byte a, byte b, byte c);
        public static IReadOnlyDictionary<Type, Filter> FilterFunctions = new Dictionary<Type, Filter>()
        {
            { Type.None, (x,a,b,c)=>x },
            { Type.Sub, FilterSub },
            { Type.Up, FilterUp },
            { Type.Average, FilterAverage },
            { Type.Paeth, FilterPaeth },
        };
        public static IReadOnlyDictionary<Type, Filter> ReconstructFunctions = new Dictionary<Type, Filter>()
        {
            { Type.None, (x,a,b,c)=>x },
            { Type.Sub, ReconSub },
            { Type.Up, ReconUp },
            { Type.Average, ReconAverage },
            { Type.Paeth, ReconPaeth },
        };

        public static byte FilterSub(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self - left);
        }
        public static byte FilterUp(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self - up);
        }
        public static byte FilterAverage(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self - ((left + up) / 2));
        }
        public static byte FilterPaeth(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self - Paeth(up, left, upleft));
        }

        public static byte ReconSub(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self + left);
        }
        public static byte ReconUp(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self + up);
        }
        public static byte ReconAverage(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self + ((left + up) / 2));
        }
        public static byte ReconPaeth(byte self, byte left, byte up, byte upleft)
        {
            return (byte)(self + Paeth(up, left, upleft));
        }

        private static byte Paeth(byte a, byte b, byte c)
        {
            byte p = (byte)(a + b - c);
            byte pa = (byte)Math.Abs((byte)(p - a));
            byte pb = (byte)Math.Abs((byte)(p - b));
            byte pc = (byte)Math.Abs((byte)(p - c));
            if (pa <= pb && pa <= pc) return a;
            else if (pb <= pc) return b;
            else return c;
        }
    }
}
