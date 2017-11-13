using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pngsteg.PNG_Decoder
{
    public class Palette : IReadOnlyList<Tuple<byte, byte, byte>>
    {
        private byte[] paletteData;

        public Palette(ref byte[] pdata)
        {
            paletteData = pdata;
        }

        public Tuple<byte, byte, byte> this[int index] => new Tuple<byte,byte,byte>(paletteData[index*3], paletteData[index * 3+1], paletteData[index * 3+2]);

        public int Count => paletteData.Length / 3;

        public class PaletteEnumerator : IEnumerator<Tuple<byte, byte, byte>>
        {
            private int pos = 0;
            private Palette palette;

            public Tuple<byte, byte, byte> Current => palette[pos];

            object IEnumerator.Current => Current;

            public PaletteEnumerator(Palette palette)
            {
                this.palette = palette;
            }

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                pos++;
                return pos < palette.Count;
            }

            public void Reset()
            {
                pos = 0;
            }
        }
        
        public IEnumerator<Tuple<byte, byte, byte>> GetEnumerator()
        {
            return new PaletteEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
