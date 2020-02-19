using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using X86Asm.libcoff;

namespace X86Asm
{
	/// <summary>
	/// The Project79068 assembler main program.
	/// </summary>
	public sealed class Assembler
	{

		/// <summary>
		/// The main method. Argument 0 is the input file name. Argument 1 is the output file name. </summary>
		/// <param name="args"> the list of command line arguments </param>
		public static void Main(string[] args)
		{
            using (var file = new FileStream(@"C:\Users\whelp\Documents\Visual Studio 2017\Projects\AdaBoostCpp\AdaBoostCpp\Debug\AdaBoost.obj", FileMode.Open)) 
            using (var br = new BinaryReader(file,Encoding.ASCII, false)) {
                libcoff.IMAGE_FILE_HEADER fileHeader = br.ReadFrom<IMAGE_FILE_HEADER>();
                Console.WriteLine("IMAGE_FILE_HEADER:");
                Console.WriteLine($"  Machine: {fileHeader.Machine.ToString()} (0x{(UInt16)fileHeader.Machine:X4})");
                Console.WriteLine($"  NumberOfSections: {fileHeader.NumberOfSections}");
                Console.WriteLine($"  TimeDateStamp: {new DateTime(1970,1,1,0,0,0).ToLocalTime().AddSeconds(fileHeader.TimeDateStamp).ToString("G")} (0x{fileHeader.TimeDateStamp:X8})");
                Console.WriteLine($"  PointerToSymbolTable: 0x{fileHeader.PointerToSymbolTable:X8}");
                Console.WriteLine($"  NumberOfSymbols: {fileHeader.NumberOfSymbols}");
                Console.WriteLine($"  SizeOfOptionalHeader: {fileHeader.SizeOfOptionalHeader}");
                Console.WriteLine($"  Characteristics: {fileHeader.Characteristics.ToString()} (0x{(UInt16)fileHeader.Characteristics:X4})");
                libcoff.IMAGE_SECTION_HEADER[] sectionHeaders = new IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];
                for (var i = 0; i < fileHeader.NumberOfSections; i++) {
                    sectionHeaders[i] = br.ReadFrom<IMAGE_SECTION_HEADER>();
        //[FieldOffset(8)]  public UInt32 VirtualSize;
        //[FieldOffset(12)] public UInt32 VirtualAddress;
        //[FieldOffset(16)] public UInt32 SizeOfRawData;
        //[FieldOffset(20)] public UInt32 PointerToRawData;
        //[FieldOffset(24)] public UInt32 PointerToRelocations;
        //[FieldOffset(28)] public UInt32 PointerToLinenumbers;
        //[FieldOffset(32)] public UInt16 NumberOfRelocations;
        //[FieldOffset(34)] public UInt16 NumberOfLinenumbers;
        //[FieldOffset(36)] public DataSectionFlags Characteristics;

                    Console.WriteLine($"IMAGE_SECTION_HEADER[{i}/{fileHeader.NumberOfSections}]:");
                    Console.WriteLine($"  Name: {new String(sectionHeaders[i].Name,0,8)}");
                    Console.WriteLine($"  VirtualSize: {sectionHeaders[i].VirtualSize}");
                    Console.WriteLine($"  VirtualAddress: 0x{sectionHeaders[i].VirtualAddress:X8}");
                    Console.WriteLine($"  SizeOfRawData: {sectionHeaders[i].SizeOfRawData}");
                    Console.WriteLine($"  PointerToRawData: 0x{sectionHeaders[i].PointerToRawData:X8}");
                    Console.WriteLine($"  PointerToRelocations: 0x{sectionHeaders[i].PointerToRelocations:X8}");
                    Console.WriteLine($"  PointerToLinenumbers: 0x{sectionHeaders[i].PointerToLinenumbers:X8}");
                    Console.WriteLine($"  NumberOfRelocations: {sectionHeaders[i].NumberOfRelocations}");
                    Console.WriteLine($"  NumberOfLinenumbers: {sectionHeaders[i].NumberOfLinenumbers}");
                    Console.WriteLine($"  Characteristics: 0x{(UInt32)sectionHeaders[i].Characteristics:X8}");
                }

                for (var i = 0; i < fileHeader.NumberOfSections; i++) {
                    br.BaseStream.Seek(sectionHeaders[i].PointerToRawData,SeekOrigin.Begin);
                    Console.WriteLine($"RelocationEntry[{i}/{fileHeader.NumberOfSections}]:");
                    for (var j = 0; j < (int) sectionHeaders[i].NumberOfRelocations; j++) {
                        Console.WriteLine($"  sectionHeaders[{j}/{sectionHeaders[i].NumberOfRelocations}]:");
                        libcoff.RelocationEntry relocationEntry = br.ReadFrom<RelocationEntry>();
                        Console.WriteLine($"    VirtualAddress: 0x{relocationEntry.VirtualAddress:X8}");
                        Console.WriteLine($"    SymbolTableIndex: 0x{relocationEntry.SymbolTableIndex:X8}");
                        Console.WriteLine($"    Type: 0x{relocationEntry.Type:X4}");

                    }
                }
            }

			if (args.Length != 2)
			{
				Console.Error.WriteLine("Usage: X86Asm INPUTFILE OUTPUTFILE");
				Environment.Exit(1);
			}

			var inputfile = File.OpenRead(args[0]);
            var outputfile = File.OpenWrite(args[1]);

			Program program =parser.Parser.parseFile(inputfile);
            generator.Assembler.assembleToFile(program, outputfile);
		}



	}

    static class BinaryReadWriteExt {
        public static void WriteTo<TStruct>(this BinaryWriter writer, TStruct s) where TStruct : struct
        {
            var size = Marshal.SizeOf(typeof(TStruct));
            var buffer = new byte[size];
            var ptr = IntPtr.Zero;

            try {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.StructureToPtr(s, ptr, false);

                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }

            writer.Write(buffer);
        }

        public static TStruct ReadFrom<TStruct>(this BinaryReader reader) where TStruct : struct
        {
            var size = Marshal.SizeOf(typeof(TStruct));
            var ptr = IntPtr.Zero;

            try {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(reader.ReadBytes(size), 0, ptr, size);

                return (TStruct)Marshal.PtrToStructure(ptr, typeof(TStruct));
            }
            finally {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
    }

}