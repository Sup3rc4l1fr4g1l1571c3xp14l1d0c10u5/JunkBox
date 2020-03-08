namespace X86Asm.parser {
    /// <summary>
    /// トークンの種別 
    /// </summary>
    public enum TokenType {
        WHITESPACE,
        NEWLINE,
        COLON,
        NAME,
        DIRECTIVE,
        LEFT_PAREN,
        RIGHT_PAREN,
        LABEL,
        REGISTER,
        COMMA,
        PLUS,
        MINUS,
        DECIMAL,
        HEXADECIMAL,
        STRING,
        DOLLAR,
        END_OF_FILE
    }
}