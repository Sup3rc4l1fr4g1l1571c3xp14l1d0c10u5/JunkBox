using System.Collections.Generic;

namespace X86Asm.generator {

    public static class DirectivePatternTable {
        public static List<DirectivePattern> DIRECTIVE_TABLE { get; } = new List<DirectivePattern>() {
            new SectionDirectivePattern(".text"),
            new SectionDirectivePattern(".data"),
            new SectionDirectivePattern(".bss" ),
            new GlobalDirectivePattern(".globl" ),
            new ConstantDirectivePattern(".db", 1 ),
            new ConstantDirectivePattern(".dw", 2 ),
            new ConstantDirectivePattern(".dd", 4 ),
        };

    }
}
