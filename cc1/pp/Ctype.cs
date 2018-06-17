using System;

namespace CSCPP
{
    /// <summary>
    /// C言語の文字識別ルーチン
    /// </summary>
    public static class CType
    {
        public static bool IsDigit(int i)
        {
            return '0' <= i && i <= '9';
        }

        public static bool IsXdigit(int c)
        {
            return ('0' <= c && c <= '9') || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F');
        }

        public static bool IsAlpha(int c)
        {
            return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z');
        }

        public static bool IsAlNum(int c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        public static bool IsOctal(int c)
        {
            return '0' <= c && c <= '7';
        }

        public static bool IsWhiteSpace(int c)
        {
            return c == ' ' || c == '\t' || c == '\f' || c == '\v';
        }

    }
}