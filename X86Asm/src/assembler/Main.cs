using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using X86Asm.libcoff;

namespace X86Asm {
    /// <summary>
    /// The Project79068 assembler main program.
    /// </summary>
    public sealed class Assembler {

        /// <summary>
        /// The main method. Argument 0 is the input file name. Argument 1 is the output file name. </summary>
        /// <param name="args"> the list of command line arguments </param>
        public static void Main(string[] args) {
            //CoffDump.Dump(@"C:\Users\whelp\Documents\Visual Studio 2017\Projects\AdaBoostCpp\AdaBoostCpp\Debug\AdaBoost.obj");

            if (args.Length != 2) {
                Console.Error.WriteLine("Usage: X86Asm <InputFile> <OutputFile>");
                Environment.Exit(1);
            }

            var inputFile = File.OpenRead(args[0]);
            var outputFile = File.OpenWrite(args[1]);

            Program program = parser.Parser.ParseFile(inputFile);
            generator.Assembler.assembleToFile(program, outputFile);
        }



    }
}