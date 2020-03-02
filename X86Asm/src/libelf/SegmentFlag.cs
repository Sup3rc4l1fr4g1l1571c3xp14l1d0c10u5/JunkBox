using System;

namespace X86Asm.libelf {
    [Flags]
    public enum SegmentFlag : UInt32 {
        /// <summary>
        /// 実行可能
        /// </summary>
        PF_X = (1 << 0),
        PF_W = (1 << 1),
        PF_R = (1 << 2),
    }
}