using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zlib;

namespace pngsteg.PNG_Decoder
{
    public class CompressionMethodZero
    {
        public static void lol()
        {
            using (var stream = Utils.GenerateStreamFromString("hi ther"))
            {
                using (var zis = new ZInputStream(stream))
                {

                }
            }
        }
    }
}
