using System.IO;
using System.Runtime.InteropServices;

namespace X86Asm.libelf {

    /// <summary>
    /// ELFヘッダの ident 
    /// </summary>
    public class ElfIdent {
        public const int Size = 16;
        public byte EI_MAG0 { get; set; }
        public byte EI_MAG1 { get; set; }
        public byte EI_MAG2 { get; set; }
        public byte EI_MAG3 { get; set; }
        public ElfClass EI_CLASS { get; set; }
        public ElfData EI_DATA { get; set; }
        public ElfVersion EI_VERSION { get; set; }
        public ElfOSABI EI_OSABI { get; set; }
        public byte EI_ABIVERSION { get; set; }

        public void WriteTo(BinaryWriter writer) {
            writer.Write(new byte[] {
                EI_MAG0 , EI_MAG1 , EI_MAG2 , EI_MAG3 ,
                (byte)EI_CLASS ,
                (byte)EI_DATA ,
                (byte)EI_VERSION ,
                (byte)EI_OSABI ,
                EI_ABIVERSION ,
                0, 0, 0, 0, 0, 0, 0
            });
        }
    }
}