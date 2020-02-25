namespace X86Asm.parser {
    /// <summary>
    /// トークンの種別 
    /// </summary>
    public enum TokenType {
        WHITESPACE,
        NEWLINE,
        COLON,
        NAME,
        LEFT_PAREN,
        RIGHT_PAREN,
        LABEL,
        REGISTER,
        COMMA,
        PLUS,
        MINUS,
        DECIMAL,
        HEXADECIMAL,
        DOLLAR,
        END_OF_FILE
    }
}