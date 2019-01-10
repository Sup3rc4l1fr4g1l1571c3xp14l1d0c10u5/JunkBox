namespace CSCPP
{
    /// <summary>
    /// 警告オプション
    /// </summary>
    public enum Warning {
        Trigraphs,
        UndefinedToken,
        UnusedMacros,
        Pedantic,
        UnknownPragmas,
        UnknownDirectives,
        LineComment,
        RedefineMacro,
        ImplicitSystemHeaderInclude,
        EmptyMacroArgument,
        VariadicMacro,
        LongLongConstant,
        ExtensionForVariadicMacro,
        Error,
        CertCCodingStandard,
        UnspecifiedBehavior,
    }
}