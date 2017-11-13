using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pngsteg.PNG_Decoder
{
    public class PngDecoder
    {
        private static byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        private static Dictionary<ColorType, byte[]> ValidBitDepths = new Dictionary<ColorType, byte[]>()
        {
            { ColorType.Greyscale, new byte[]{ 1,2,3,8,16 } },
            { ColorType.Truecolour, new byte[]{ 8,16 } },
            { ColorType.IndexedColour, new byte[]{ 1,2,4,8 } },
            { ColorType.GreyscaleAlpha, new byte[]{ 8,16 } },
            { ColorType.TruecolourAlpha, new byte[]{ 8,16 } },
        };

        #region IHDR
        /* IHDR data */
        private enum ColorType : byte
        {
            UsePalette = 1, UseTruecolour = 2, UseAlpha = 4,

            Greyscale = 0,
            Truecolour = UseTruecolour,
            IndexedColour = UsePalette | UseTruecolour,
            GreyscaleAlpha = UseAlpha,
            TruecolourAlpha = UseTruecolour | UseAlpha
        }
        private ColorType colorType;
        private uint width;
        private uint height;
        private byte compression; // = 0
        private byte filter; // = 0
        private byte bitDepth;
        private enum InterlacingType : byte
        {
            None=0,Adam7=1
        }
        private InterlacingType interlacing;
        /* END IHDR Data */
        #endregion

        private bool hasTransparency = false;
        private byte[] transparencyValues;

        #region Palette
        private bool hasPalette = false;
        private byte[] paletteData;
        private long paletteLength;
        public Palette Palette;

        private byte[] paletteTransparency;
        #endregion

        #region Raw image data
        private List<byte> nonConstData = new List<byte>();
        private byte[] rawImageData;
        #endregion

        public void Decode(byte[] pngstream)
        {
            bool ispng = Utils.CompareFromBeginning(pngstream, PngSignature);
            if (!ispng) throw new ArgumentException("Input data is not a PNG file!");

            long current_pos = 8;

            while (true)
            { // Read a chunk
                byte[] blength = new byte[4];
                byte[] type = new byte[4];
                byte[] data;
                byte[] crc = new byte[4];

                Utils.CopyFrom(pngstream, blength, 4, current_pos); current_pos += 4;
                Utils.CopyFrom(pngstream, type, 4, current_pos); current_pos += 4;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(blength);
                uint length = BitConverter.ToUInt32(blength, 0);

                data = new byte[length];
                Utils.CopyFrom(pngstream, data, length, current_pos); current_pos += length;
                Utils.CopyFrom(pngstream, crc, 4, current_pos); current_pos += 4;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(crc);

                // Check CRC
                byte[] crcData = new byte[length + 4];
                Array.Copy(type, crcData, 4);
                Array.Copy(data, 0, crcData, 4, length);
                uint calcCRC = (uint)CRC.CalculateCRC(crcData);
                uint refCRC = BitConverter.ToUInt32(crc, 0);

                if (calcCRC != refCRC) throw new PngDecoderException("Chunk CRC check failed");

                ProcessChunk(type, data);

                if (Utils.CompareFromBeginning(type, name_conversion_reverse["IEND"]))
                {
                    if (current_pos != pngstream.LongLength)
                        throw new PngDecoderException("There is data after the IEND block!");

                    break;
                }
            }

            ProcessChunksEnd();
        }

        private enum ChunkPositioning
        {
            None=0, BeforeIDAT=1, BeforePLTE=2, AfterPLTE=4, IsPLTE=8, First=16, Last=32, IsIDAT=64
        }
        private enum ChunkAmount
        {
            Optional=1, One=2, More=4,
            OneOrMore=One|More, ZeroOrMore=Optional|More
        }
        private struct ChunkTypeInfo
        {
            public string name;
            public string[] excludes;
            public ChunkPositioning position;
            public ChunkAmount amount;
        }
        private struct bChunkTypeInfo
        {
            public byte[] name;
            public byte[][] excludes;
            public ChunkPositioning position;
            public ChunkAmount amount;
        }

        private static Dictionary<string, ChunkTypeInfo> chunk_types = new Dictionary<string, ChunkTypeInfo>() {
            { "IHDR", new ChunkTypeInfo(){ name="IHDR", excludes=new string[]{}, position=ChunkPositioning.First, amount=ChunkAmount.One } },
            { "PLTE", new ChunkTypeInfo(){ name="PLTE", excludes=new string[]{}, position=ChunkPositioning.IsPLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "IDAT", new ChunkTypeInfo(){ name="IDAT", excludes=new string[]{}, position=ChunkPositioning.IsIDAT, amount=ChunkAmount.OneOrMore } },
            { "IEND", new ChunkTypeInfo(){ name="IEND", excludes=new string[]{}, position=ChunkPositioning.Last, amount=ChunkAmount.One } },

            { "cHRM", new ChunkTypeInfo(){ name="cHRM", excludes=new string[]{}, position=ChunkPositioning.BeforePLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "gAMA", new ChunkTypeInfo(){ name="gAMA", excludes=new string[]{}, position=ChunkPositioning.BeforePLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "sBIT", new ChunkTypeInfo(){ name="sBIT", excludes=new string[]{}, position=ChunkPositioning.BeforePLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "iCCP", new ChunkTypeInfo(){ name="iCCP", excludes=new string[]{"sRGB"}, position=ChunkPositioning.BeforePLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "sRGB", new ChunkTypeInfo(){ name="sRGB", excludes=new string[]{"iCCP"}, position=ChunkPositioning.BeforePLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },

            { "bKGD", new ChunkTypeInfo(){ name="bKGD", excludes=new string[]{}, position=ChunkPositioning.AfterPLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "hIST", new ChunkTypeInfo(){ name="hIST", excludes=new string[]{}, position=ChunkPositioning.AfterPLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "tRNS", new ChunkTypeInfo(){ name="tRNS", excludes=new string[]{}, position=ChunkPositioning.AfterPLTE|ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            
            { "pHYS", new ChunkTypeInfo(){ name="pHYS", excludes=new string[]{}, position=ChunkPositioning.BeforeIDAT, amount=ChunkAmount.Optional } },
            { "sPLT", new ChunkTypeInfo(){ name="sPLT", excludes=new string[]{}, position=ChunkPositioning.BeforeIDAT, amount=ChunkAmount.ZeroOrMore } },
            
            { "tIME", new ChunkTypeInfo(){ name="tIME", excludes=new string[]{}, position=ChunkPositioning.None, amount=ChunkAmount.Optional } },
            { "iTXt", new ChunkTypeInfo(){ name="iTXt", excludes=new string[]{}, position=ChunkPositioning.None, amount=ChunkAmount.ZeroOrMore } },
            { "tEXt", new ChunkTypeInfo(){ name="tEXt", excludes=new string[]{}, position=ChunkPositioning.None, amount=ChunkAmount.ZeroOrMore } },
            { "zTXt", new ChunkTypeInfo(){ name="zTXt", excludes=new string[]{}, position=ChunkPositioning.None, amount=ChunkAmount.ZeroOrMore } },
        };
        private static Dictionary<byte[], bChunkTypeInfo> bchunk_types = new Dictionary<byte[], bChunkTypeInfo>(new Utils.ByteArrayEqualityComparer());
        private static Dictionary<byte[], string> name_conversion = new Dictionary<byte[], string>(new Utils.ByteArrayEqualityComparer());
        private static Dictionary<string, byte[]> name_conversion_reverse = new Dictionary<string, byte[]>();
        static PngDecoder()
        {
            foreach (var el in chunk_types)
            {
                byte[] name = el.Key.Select((chr)=>Convert.ToByte(chr)).ToArray();
                var bchnkinfo = new bChunkTypeInfo
                {
                    name = name,
                    excludes = el.Value.excludes.Select((s) => s.Select((chr) => Convert.ToByte(chr)).ToArray()).ToArray(),
                    position = el.Value.position,
                    amount = el.Value.amount
                };
                bchunk_types.Add(name, bchnkinfo);
            }

            foreach (var n in chunk_types.Keys)
            {
                name_conversion.Add(n.Select((chr) => Convert.ToByte(chr)).ToArray(), n);
                name_conversion_reverse.Add(n, n.Select((chr) => Convert.ToByte(chr)).ToArray());
            }
        }

        private Dictionary<byte[], int> chunk_occurrences = new Dictionary<byte[], int>(new Utils.ByteArrayEqualityComparer());
        private int chunk_count = 0;

        private byte[] prevChunk = new byte[4];

        private void ProcessChunk(byte[] type, byte[] data)
        {
            bool anc = (type[0] & 0x10) > 0;
            bool prvt = (type[1] & 0x10) > 0;
            bool resrv = (type[2] & 0x10) > 0;
            bool copy = (type[3] & 0x10) > 0;

            IncrementChunkCount(type);

            if (bchunk_types.ContainsKey(type))
            {
                var info = bchunk_types[type];

                // Check ordering rules
                if (info.position.HasFlag(ChunkPositioning.First) && chunk_count != 1)
                    throw new PngDecoderException("The first chunk is not what it is expected to be!");
                if (!info.position.HasFlag(ChunkPositioning.First) && chunk_count == 1)
                    throw new PngDecoderException("The first chunk is not what it is expected to be!");
                if (info.position.HasFlag(ChunkPositioning.Last)) ; // TODO: Check if last

                if (info.position.HasFlag(ChunkPositioning.BeforePLTE) && chunk_occurrences[name_conversion_reverse["PLTE"]] != 0)
                    throw new PngDecoderException("A chunk that needs to go before the PLTE chunk came after!");
                if (info.position.HasFlag(ChunkPositioning.AfterPLTE) && chunk_occurrences[name_conversion_reverse["PLTE"]] != 1)
                    throw new PngDecoderException("A chunk that needs to go after the PLTE chunk came before!");
                if (info.position.HasFlag(ChunkPositioning.BeforeIDAT) && chunk_occurrences[name_conversion_reverse["IDAT"]] != 0)
                    throw new PngDecoderException("A chunk that needs to go before the PLTE chunk came after!");

                // Check amount rules
                if (info.amount == ChunkAmount.Optional && chunk_occurrences[type] > 1)
                    throw new PngDecoderException("An optional one-time chunk occurred more than once!");
                if (info.amount == ChunkAmount.One && chunk_occurrences[type] > 1)
                    throw new PngDecoderException("A one-time chunk occurred more than once!");

                // Check exclusion rules
                foreach (var ex in info.excludes)
                    if (chunk_occurrences.ContainsKey(ex) && chunk_occurrences[ex] > 0)
                        throw new PngDecoderException("Two conflicting chunks found!");
            }

            long position = 0;

            if (!prvt)
            { // Is a public chunk (aka I know what to do with it)

                // Ancillary chunk types
                if (Utils.CompareFromBeginning(type, name_conversion_reverse["IHDR"]))
                {
                    Debug.Assert(Utils.CompareFromBeginning(name_conversion_reverse["IHDR"], new byte[] { 73, 72, 68, 82 }));

                    byte[] width = new byte[4];
                    byte[] height = new byte[4];

                    Utils.CopyFrom(data, width, 4, position); position += 4;
                    Utils.CopyFrom(data, height, 4, position); position += 4;

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(width);
                        Array.Reverse(height);
                    }

                    this.width = BitConverter.ToUInt32(width, 0);
                    this.height = BitConverter.ToUInt32(height, 0);

                    bitDepth = data[position++];
                    colorType = (ColorType)data[position++];
                    compression = data[position++];
                    filter = data[position++];
                    interlacing = (InterlacingType)data[position++];

                    Debug.Assert(data.LongLength == position);
                }
                if (Utils.CompareFromBeginning(type, name_conversion_reverse["PLTE"]))
                {
                    Debug.Assert(Utils.CompareFromBeginning(name_conversion_reverse["PLTE"], new byte[] { 80, 76, 84, 69 }));

                    if (!(colorType.HasFlag(ColorType.UsePalette) || colorType == ColorType.Truecolour || colorType == ColorType.TruecolourAlpha))
                        throw new PngDecoderException("Palette cannot exist for the defined color type");
                    if (data.LongLength % 3 != 0)
                        throw new PngDecoderException("Palette data length is not a multiple of 3!");

                    paletteLength = data.LongLength / 3L;
                    paletteData = data;
                    hasPalette = true;

                    if (Palette.Count > Math.Pow(2, bitDepth))
                        throw new PngDecoderException("The palette length must be less than or equal to the number of values prepresentable by the bit depth!");
                }
                if (Utils.CompareFromBeginning(type, name_conversion_reverse["IDAT"]))
                {
                    Debug.Assert(Utils.CompareFromBeginning(name_conversion_reverse["IDAT"], new byte[] { 73, 68, 65, 84 }));

                    if (chunk_occurrences[name_conversion_reverse["IDAT"]] > 1 && prevChunk != name_conversion_reverse["IDAT"])
                        throw new PngDecoderException("IDAT chunks are not consecutive!");

                    nonConstData.AddRange(data);
                }
                if (Utils.CompareFromBeginning(type, name_conversion_reverse["IEND"]))
                {
                    Debug.Assert(Utils.CompareFromBeginning(name_conversion_reverse["IEND"], new byte[] { 73, 69, 78, 68 }));

                    if (data.LongLength != 0)
                        throw new PngDecoderException("IEND block has nonzero length!");
                }

                // Non-ancillary chunk types
                if (Utils.CompareFromBeginning(type, name_conversion_reverse["tRNS"]))
                {
                    Debug.Assert(Utils.CompareFromBeginning(name_conversion_reverse["tRNS"], new byte[] { 166, 82, 78, 83 }));

                    hasTransparency = true;
                    if (hasPalette)
                    {
                        if (data.Length != Palette.Count)
                            throw new PngDecoderException("Palette transparency is a different length from the palette!");

                        paletteTransparency = data;
                    }
                    else
                    {
                        if (colorType == ColorType.Greyscale && data.LongLength != 2)
                            throw new PngDecoderException("Transparency data for color type Greyscale must be 2 bytes long!");
                        if (colorType == ColorType.Truecolour && data.LongLength != 6)
                            throw new PngDecoderException("Transparency data for color type Truecolor must be 6 bytes long!");
                        
                        transparencyValues = data;
                    }
                }
                if (Utils.CompareFromBeginning(type, name_conversion_reverse["cHRM"]))
                {
                    Debug.Assert(Utils.CompareFromBeginning(name_conversion_reverse["cHRM"], new byte[] { 99, 72, 82, 77 }));


                }
            }

            prevChunk = type;
        }

        private void ProcessChunksEnd()
        {
            // Check amount rules
            foreach (var itm in bchunk_types)
            {
                var name = itm.Key;
                var info = itm.Value;
                int count = chunk_occurrences[name];

                if (info.amount.HasFlag(ChunkAmount.One) && count < 1)
                    throw new PngDecoderException("Required chunk does not exist!");
            }

            if (colorType.HasFlag(ColorType.Greyscale) && chunk_occurrences[name_conversion_reverse["PLTE"]] != 0)
                throw new PngDecoderException("A greyscale image cannot have a palette!");
            if (colorType.HasFlag(ColorType.IndexedColour) && chunk_occurrences[name_conversion_reverse["PLTE"]] != 1)
                throw new PngDecoderException("An IndexedColor image must have a palette!");

            // Finalize other things
            rawImageData = nonConstData.ToArray();
        }

        private void IncrementChunkCount(byte[] chunk)
        {
            if (!chunk_occurrences.ContainsKey(chunk))
                chunk_occurrences.Add(chunk, 0);
            chunk_occurrences[chunk]++;
            chunk_count++;
        }

        public PngDecoder()
        {
            Palette = new Palette(ref paletteData);

            foreach (var k in bchunk_types.Keys)
                chunk_occurrences.Add(k, 0);
        }
    }
}
