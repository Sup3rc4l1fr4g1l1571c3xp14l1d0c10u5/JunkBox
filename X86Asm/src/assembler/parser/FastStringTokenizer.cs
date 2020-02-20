using System;
using System.IO;
using System.Text;

namespace X86Asm.parser {

    public class FastStringTokenizer : Tokenizer {

        private static string Read(Stream file) {
            if (file == null) {
                throw new ArgumentNullException();
            }
            return new StreamReader(file).ReadToEnd() + "\n";
        }

        private string SourceCode { get; }

        private int Offset { get; set; }

        public FastStringTokenizer(Stream file) {
            if (file == null) {
                throw new ArgumentNullException();
            }
            SourceCode = Read(file);
            Offset = 0;
        }


        public FastStringTokenizer(string sourceCode) {
            if (sourceCode == null) {
                throw new ArgumentNullException();
            }
            SourceCode = sourceCode;
            Offset = 0;
        }

        /// <summary>
        /// Returns the next token from this tokenizer. </summary>
        /// <returns> the next token from this tokenizer </returns>
        public override Token Next() {
            int start = Offset;

            switch (PeekChar()) {
                case -1:
                    return new Token(TokenType.END_OF_FILE, "");

                case '$':
                    Offset++;
                    return new Token(TokenType.DOLLAR, "$");

                case '(':
                    Offset++;
                    return new Token(TokenType.LEFT_PAREN, "(");

                case ')':
                    Offset++;
                    return new Token(TokenType.RIGHT_PAREN, ")");

                case ',':
                    Offset++;
                    return new Token(TokenType.COMMA, ",");

                case '+':
                    Offset++;
                    return new Token(TokenType.PLUS, "+");

                case '-':
                    Offset++;
                    if (IsDecimal(PeekChar())) {
                        Offset++;
                        while (IsDecimal(PeekChar())) {
                            Offset++;
                        }
                        return new Token(TokenType.DECIMAL, SourceCode.Substring(start, Offset - start));
                    } else {
                        return new Token(TokenType.MINUS, "-");
                    }

                case '\n':
                case '\r':
                    Offset++;
                    while (PeekChar() == '\r' || PeekChar() == '\n') {
                        Offset++;
                    }
                    return new Token(TokenType.NEWLINE, SourceCode.Substring(start, Offset - start));

                case ' ': // Whitespace
                case '\t':
                    Offset++;
                    while (PeekChar() == ' ' || PeekChar() == '\t') {
                        Offset++;
                    }
                    return Next();

                case '#': // Comment
                    Offset++;
                    while (PeekChar() != '\r' && PeekChar() != '\n') {
                        Offset++;
                    }
                    return Next();

                case '%':
                    Offset++;
                    if (!IsNameStart(PeekChar())) {
                        throw new Exception("No token pattern match");
                    }
                    Offset++;
                    while (IsNameContinuation(PeekChar())) {
                        Offset++;
                    }
                    return new Token(TokenType.REGISTER, SourceCode.Substring(start, Offset - start));

                case '0':
                    Offset++;
                    if (PeekChar() == -1) {
                        return new Token(TokenType.DECIMAL, "0");
                    }
                    if (PeekChar() != 'x' && PeekChar() != 'X') {
                        while (IsDecimal(PeekChar())) {
                            Offset++;
                        }
                        return new Token(TokenType.DECIMAL, SourceCode.Substring(start, Offset - start));
                    } else {
                        Offset++;
                        while (isHexadecimal(PeekChar())) {
                            Offset++;
                        }
                        return new Token(TokenType.HEXADECIMAL, SourceCode.Substring(start, Offset - start));
                    }

                default:
                    if (IsNameStart(PeekChar())) {
                        Offset++;
                        while (IsNameContinuation(PeekChar())) {
                            Offset++;
                        }

                        if (PeekChar() == ':') {
                            Offset++;
                            return new Token(TokenType.LABEL, SourceCode.Substring(start, Offset - start));
                        } else {
                            return new Token(TokenType.NAME, SourceCode.Substring(start, Offset - start));
                        }

                    } else if (IsDecimal(PeekChar())) {
                        Offset++;
                        while (IsDecimal(PeekChar())) {
                            Offset++;
                        }
                        return new Token(TokenType.DECIMAL, SourceCode.Substring(start, Offset - start));

                    } else {
                        throw new Exception("No token pattern match");
                    }
            }
        }


        private int PeekChar() {
            if (Offset == SourceCode.Length) {
                return -1;
            } else {
                return SourceCode[Offset];
            }
        }


        private static bool IsNameStart(int c) {
            return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_';
        }


        private static bool IsNameContinuation(int c) {
            return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_';
        }


        private static bool IsDecimal(int c) {
            return c >= '0' && c <= '9';
        }


        private static bool isHexadecimal(int c) {
            return c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f' || c >= '0' && c <= '9';
        }

    }

}