using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pngsteg.PNG_Decoder
{
    public class CRC
    {
        private static ulong[] crc_table = new ulong[256];

        static CRC()
        { // Initialize the CRC table
            ulong c = 0;

            for (int n = 0; n < 256; n++)
            {
                c = (ulong)n;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) > 0) c = 0xedb88320L ^ (c >> 1);
                    else c = c >> 1;
                }
                crc_table[n] = c;
            }
        }

        /*
unsigned long update_crc(unsigned long crc, unsigned char *buf,
                            int len)
   {
     unsigned long c = crc;
     int n;
   
     if (!crc_table_computed)
       make_crc_table();
     for (n = 0; n < len; n++) {
       c = crc_table[(c ^ buf[n]) & 0xff] ^ (c >> 8);
     }
     return c;
   }
   
   /* Return the CRC of the bytes buf[0..len-1]. *
        unsigned long crc(unsigned char* buf, int len)
        {
            return update_crc(0xffffffffL, buf, len) ^ 0xffffffffL;
        }*/

        public static ulong UpdateCRC(ulong crc, byte[] data)
        {
            for (long n = 0; n < data.LongLength; n++)
            {
                crc = crc_table[(crc ^ data[n]) & 0xff] ^ (crc >> 8);
            }

            return crc;
        }

        public static ulong CalculateCRC(byte[] data)
        {
            return UpdateCRC(0xffffffffL, data) ^ 0xffffffffL;
        }

    }
}
