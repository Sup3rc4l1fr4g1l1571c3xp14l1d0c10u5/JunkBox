using System;

namespace AnsiCParser {
    /// <summary>
    /// 字句解析の結果得られるトークン
    /// </summary>
    public class Token {
        /// <summary>
        /// トークンの種別を示す列挙型
        /// </summary>
        [Flags]
        public enum TokenKind {
            EOF = -1,
            // ReserveWords
            AUTO = 256,
            BREAK,
            CASE,
            CHAR,
            CONST,
            CONTINUE,
            DEFAULT,
            DO,
            DOUBLE,
            ELSE,
            ENUM,
            EXTERN,
            FLOAT,
            FOR,
            GOTO,
            IF,
            INT,
            LONG,
            REGISTER,
            RETURN,
            SHORT,
            SIGNED,
            SIZEOF,
            STATIC,
            STRUCT,
            SWITCH,
            TYPEDEF,
            UNION,
            UNSIGNED,
            VOID,
            VOLATILE,
            WHILE,
            // C99
            INLINE,
            RESTRICT,
            // Special
            NEAR,
            FAR,
            __ASM__,
            __VOLATILE__,
            // Identifiers
            IDENTIFIER,
            //TYPE_NAME,
            // Constants
            STRING_CONSTANT,
            HEXIMAL_CONSTANT,
            OCTAL_CONSTANT,
            DECIAML_CONSTANT,
            FLOAT_CONSTANT,
            DOUBLE_CONSTANT,
            // StringLiteral
            STRING_LITERAL,
            // Symbols
            ELLIPSIS,
            RIGHT_ASSIGN,
            LEFT_ASSIGN,
            ADD_ASSIGN,
            SUB_ASSIGN,
            MUL_ASSIGN,
            DIV_ASSIGN,
            MOD_ASSIGN,
            AND_ASSIGN,
            XOR_ASSIGN,
            OR_ASSIGN,
            RIGHT_OP,
            LEFT_OP,
            INC_OP,
            DEC_OP,
            PTR_OP,
            AND_OP,
            OR_OP,
            LE_OP,
            GE_OP,
            EQ_OP,
            NE_OP,
        }
        /// <summary>
        /// トークンの元ソース上での開始位置
        /// </summary>
        public Location Start {
            get;
        }
        /// <summary>
        /// トークンの元ソース上での末尾位置
        /// </summary>
        public Location End {
            get;
        }
        /// <summary>
        /// トークンの元文字列
        /// </summary>
        public string Raw {
            get;
        }
        /// <summary>
        /// トークンの種別
        /// </summary>
        public TokenKind Kind {
            get;
        }
        public Token(TokenKind kind, Location start, Location end, string raw) {
            Kind = kind;
            Start = start;
            End = end;
            Raw = raw;
        }
        public override string ToString() {
            return $"(\"{Raw}\", {Kind}, {Start}, {End})";
        }
    }
}