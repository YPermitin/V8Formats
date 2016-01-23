using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace DevelPlatform.Utils
{
    static class ConversionClass
    {
        public static float HexStringToFloat(string pvVal)
        {
            int discarded = 0;
            return BitConverter.ToSingle(ReverseBytes(GetBytes(pvVal, out discarded)), 0);
        }

        public static ushort HexStringToUInt16(string pvVal)
        {
            ushort num = 0;
            if (pvVal == null)
            {
                return num;
            }
            int discarded = 0;
            if (pvVal.Length == 2)
            {
                pvVal = "00" + pvVal;
            }
            if (pvVal.Length != 4)
            {
                return 0;
            }
            num = BitConverter.ToUInt16(GetBytes(pvVal, out discarded), 0);
            return (ushort)((num >> 8) | (num << 8));
        }

        public static uint HexStringToUInt32(string pvVal)
        {
            int discarded = 0;
            if (pvVal != null)
            {
                while (pvVal.Length < 8)
                {
                    pvVal = "00" + pvVal;
                }
                if (pvVal.Length != 8)
                {
                    return 0;
                }
                return BitConverter.ToUInt32(ReverseBytes(GetBytes(pvVal, out discarded)), 0);
            }
            return 0;
        }




        public static byte[] GetBytes(string hexString, out int discarded)
        {
            discarded = 0;
            if (hexString == null)
            {
                return new byte[0];
            }
            string str = "";
            for (int i = 0; i < hexString.Length; i++)
            {
                char c = hexString[i];
                if (IsHexDigit(c))
                {
                    str = str + c;
                }
                else
                {
                    discarded++;
                }
            }
            if ((str.Length % 2) != 0)
            {
                discarded++;
                str = str.Substring(0, str.Length - 1);
            }
            int num2 = str.Length / 2;
            byte[] buffer = new byte[num2];
            int num3 = 0;
            for (int j = 0; j < buffer.Length; j++)
            {
                string hex = new string(new char[] { str[num3], str[num3 + 1] });
                buffer[j] = HexToByte(hex);
                num3 += 2;
            }
            return buffer;
        }

        public static bool IsHexDigit(char c)
        {
            int num2 = Convert.ToInt32('A');
            int num3 = Convert.ToInt32('0');
            c = char.ToUpper(c);
            int num = Convert.ToInt32(c);
            return (((num >= num2) && (num < (num2 + 6))) || ((num >= num3) && (num < (num3 + 10))));
        }

        public static byte HexToByte(string hex)
        {
            if ((hex.Length > 2) || (hex.Length <= 0))
            {
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            }
            return byte.Parse(hex, NumberStyles.HexNumber);
        }

        public static byte[] ReverseBytes(byte[] lvBytes)
        {
            for (int i = 0; i <= (Math.Round((double)(lvBytes.Length / 2)) - 1.0); i++)
            {
                byte num = lvBytes[lvBytes.Length - (1 + i)];
                lvBytes[lvBytes.Length - (1 + i)] = lvBytes[i];
                lvBytes[i] = num;
            }
            return lvBytes;
        }


        public static String IntToHexString(int n, int len)
        {
            char[] ch = new char[len--];
            for (int i = len; i >= 0; i--)
            {
                ch[len - i] = ByteToHexChar((byte)((uint)(n >> 4 * i) & 15));
            }
            return new String(ch);
        }

        public static char ByteToHexChar(byte b)
        {
            if (b < 0 || b > 15)
                throw new Exception("IntToHexChar: input out of range for Hex value");
            return b < 10 ? (char)(b + 48) : (char)(b + 55);
        }

        public static int HexStringToInt(String str)
        {
            int value = 0;
            for (int i = 0; i < str.Length; i++)
            {
                value += HexCharToInt(str[i]) << ((str.Length - 1 - i) * 4);
            }
            return value;
        }

        public static int HexCharToInt(char ch)
        {
            if (ch < 48 || (ch > 57 && ch < 65) || ch > 70)
                throw new Exception("HexCharToInt: input out of range for Hex value");
            return (ch < 58) ? ch - 48 : ch - 55;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
