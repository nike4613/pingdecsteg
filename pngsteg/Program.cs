﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pngsteg
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] png = File.ReadAllBytes(@"Z:\Users\aaron\Pictures\Lazwardavatar.png");

            PNG_Decoder.PngDecoder decoder = new PNG_Decoder.PngDecoder();

            decoder.Decode(png);

        }
    }
}
