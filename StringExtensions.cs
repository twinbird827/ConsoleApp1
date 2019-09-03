using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public static class StringExtensions
    {
        /// <summary>
        /// 左辺から指定した長さの文字を取得します。
        /// </summary>
        /// <param name="s">対象文字</param>
        /// <param name="length">長さ</param>
        /// <param name="padding">長さが足りない場合に埋める文字(ﾃﾞﾌｫﾙﾄ空白)</param>
        /// <returns></returns>
        public static string Left(this string s, int length, char padding = ' ')
        {
            return s.Mid(0, length, padding);
        }

        /// <summary>
        /// 取得する位置と長さを指定して文字を取得します。
        /// </summary>
        /// <param name="s">対象文字</param>
        /// <param name="start">取得する開始位置</param>
        /// <param name="length">取得する文字の長さ</param>
        /// <param name="padding">長さが足りない場合に埋める文字(ﾃﾞﾌｫﾙﾄ空白)</param>
        /// <returns></returns>
        public static string Mid(this string s, int start, int length, char padding = ' ')
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            else if (s.Length < (start + length))
            {
                return s.PadRight(start + length, padding).Mid(start, length, padding);
            }
            else
            {
                return s.Substring(start, length);
            }
        }

        /// <summary>
        /// 右辺から指定した長さの文字を取得します。
        /// </summary>
        /// <param name="s">対象文字</param>
        /// <param name="length">長さ</param>
        /// <param name="padding">長さが足りない場合に埋める文字(ﾃﾞﾌｫﾙﾄ空白)</param>
        /// <returns></returns>
        public static string Right(this string s, int length, char padding = ' ')
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            else if (s.Length < (length))
            {
                return s.PadLeft(length, padding).Right(length, padding);
            }
            else
            {
                return s.Substring(s.Length - length, length);
            }
        }

        /// <summary>
        /// 文字をﾊﾞｲﾄ配列に変換します。
        /// </summary>
        /// <param name="s">対象文字</param>
        /// <param name="encoding">ﾊﾞｲﾄ配列に変換する際に使用するｴﾝｺｰﾃﾞｨﾝｸﾞ</param>
        /// <returns></returns>
        public static byte[] ToBytes(this string s, Encoding encoding)
        {
            return encoding.GetBytes(s);
        }

        /// <summary>
        /// 数値を16進数文字に変換します。
        /// </summary>
        /// <param name="i">対象数値</param>
        /// <param name="length">16進数文字の長さ(ﾃﾞﾌｫﾙﾄ2)</param>
        /// <returns></returns>
        public static string ToHex(this int i, int length = 2)
        {
            return Convert.ToString(i, 16).Right(length, '0');
        }

        /// <summary>
        /// ﾊﾞｲﾄ配列を文字に変換します。
        /// </summary>
        /// <param name="bytes">対象ﾊﾞｲﾄ配列</param>
        /// <param name="encoding">文字に変換する際に使用するｴﾝｺｰﾃﾞｨﾝｸﾞ</param>
        /// <returns></returns>
        public static string ToString(this byte[] bytes, Encoding encoding)
        {
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// 文字を16進数文字に変換します。
        /// </summary>
        /// <param name="s">対象文字</param>
        /// <param name="encoding">16進数文字に変換する際に使用するｴﾝｺｰﾃﾞｨﾝｸﾞ</param>
        /// <param name="length">16進数文字の長さ(ﾃﾞﾌｫﾙﾄ4)</param>
        /// <returns></returns>
        public static string ToHex(this string s, Encoding encoding, int length = 4)
        {
            return BitConverter.ToString(s.ToBytes(encoding))
                .Replace("-", "")
                .PadRight(length, '0');
        }

        /// <summary>
        /// 2進数文字を16進数文字に変換します。
        /// </summary>
        /// <param name="s">2進数文字</param>
        /// <param name="length">16進数文字変換後の文字長さ</param>
        /// <returns></returns>
        public static string Bin2Hex(this string s, int length = 4)
        {
            return Convert.ToInt32(s, 2).ToHex(length);
        }
    }
}
