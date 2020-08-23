using System;
using System.Linq;
using System.Diagnostics;
using AnsiCParser.SyntaxTree;
using Codeplex.Data;
using System.Collections.Generic;
using System.Collections;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {

            //
            // I Do Not Know C. 
            //
            if (!Debugger.IsAttached) {
                CommonMain(args);
            } else {
//                CommonMain(args);
                DebugMain(args);
            }
        }

        private class CommandLineOptions {
            public string OutputFile;
            public string AstFile;
            public bool FlagSyntaxOnly;
            public string OutputEncoding = System.Text.Encoding.Default.WebName;
            public string[] Args = new string[0];

            public void Validation(Action<string> act) {
                // check outputEncoding
                var enc = System.Text.Encoding.GetEncodings().FirstOrDefault(x => String.Equals(x.Name, OutputEncoding, StringComparison.OrdinalIgnoreCase));
                if (enc == null) {
                    act($"指定されたエンコーディング名 {OutputEncoding} は利用可能なエンコーディングに一致しません。");
                } else {
                    Console.OutputEncoding = enc.GetEncoding();
                }

                // check input source
                if (Args.Length == 0) {
                    act("コンパイル対象のCソースファイルを１つ指定してください。");
                //} else if (Args.Length > 1) {
                //    act("コンパイル対象のCソースファイルが２つ以上指定されています。");
                } else {
                    foreach (var arg in Args) {
                        if (System.IO.File.Exists(arg) == false) {
                            act($"ファイル {arg} が見つかりません。処理を中止します。");
                        }
                    }
                }

                // check output
                if (OutputFile == null) {
                    OutputFile = System.IO.Path.ChangeExtension(Args[0], "s");
                }

            }
        }

        static void CommonMain(string[] args) {
            var opts = new CommandLineOptionsParser<CommandLineOptions>()
                .Entry(@"-o", 1, (t, s) => {
                    t.OutputFile = s[0];
                    return true;
                })
                .Entry(@"-ast", 1, (t, s) => {
                    t.AstFile = s[0];
                    return true;
                })
                .Entry(@"-console-output-encoding", 1, (t, s) => {
                    t.OutputEncoding = s[0];
                    return true;
                })
                .Entry(@"-fsyntax-only", 0, (t, s) => {
                    t.FlagSyntaxOnly = true;
                    return true;
                })
                .Default((t, s) => {
                    t.Args = s;
                    return true;
                })
                .Parse(new CommandLineOptions(), args);

            opts.Validation((e) => {
                Logger.Error(e);
                Environment.Exit(-1);
            });

            try {
                if (opts.Args.Length == 1) {
                    var ret = new Parser(System.IO.File.ReadAllText(opts.Args[0]), opts.Args[0]).Parse();
                    if (opts.AstFile != null) {
                        using (var o = new System.IO.StreamWriter(opts.AstFile)) {
                            o.WriteLine(DynamicJson.Serialize(ret.Accept(new ToJsonVisitor(), null)));
                        }
                    }

                    if (opts.FlagSyntaxOnly == false) {
                        using (var o = new System.IO.StreamWriter(opts.OutputFile)) {
                            var compiler = new Compiler();
                            compiler.Compile(ret, o);
                        }
                    }
                } else {
                    foreach (var arg in opts.Args) {
                        var astFile = System.IO.Path.ChangeExtension(System.IO.Path.GetFullPath(arg), "json");
                        var asmFile = System.IO.Path.ChangeExtension(System.IO.Path.GetFullPath(arg), "s");
                        var ret = new Parser(System.IO.File.ReadAllText(arg), arg).Parse();
                        using (var o = new System.IO.StreamWriter(astFile)) {
                            o.WriteLine(DynamicJson.Serialize(ret.Accept(new ToJsonVisitor(), null)));
                        }

                        if (opts.FlagSyntaxOnly == false) {
                            using (var o = new System.IO.StreamWriter(asmFile)) {
                                var compiler = new Compiler();
                                compiler.Compile(ret, o);
                            }
                        }
                    }
                }
            } catch (CompilerException e) {
                Logger.Error(e.Start, e.End, e.Message);
                Logger.Error(e.StackTrace);
                Environment.Exit(-1);
            } catch (Exception e) {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
                Environment.Exit(-1);
            }

        }

        static void DebugMain(string[] args) {
            BitBuffer.Test();
            var ret = new Parser(
                System.IO.File.ReadAllText(@"C:\Users\whelp\Desktop\cc1\TestCase\tcc\tmp\95_bitfields.i") /*
                @"
typedef long unsigned int size_t;

int printf (const char*, ...);
void *memset (void*, int, size_t);


void dump(void *p, int s)
{
    int i;
    for (i = s; --i >= 0;)
        printf(""%02X"", ((unsigned char*)p)[i]);
    printf(""\n"");
}

#pragma pack(1)

int top = 1;

int main(void) {

    struct __s
    {
        unsigned x:5, y:5, :0, z:5; char a:5; short b:5;
    };
    return 0;
}


"//*/
                , "<Debug>").Parse();
            using (var o = new System.IO.StreamWriter("debug.ast")) {
                o.WriteLine(DynamicJson.Serialize(ret.Accept(new ToJsonVisitor(), null)));
            }
            using (  var o = new System.IO.StreamWriter("debug.s")) {
                var compiler = new Compiler();
                compiler.Compile(ret, o);
            }
            return;
        }
    }

    public class BitBuffer : IEnumerable<Byte> {
        private readonly List<byte> bytes;
        public Byte this[int n] { get { return bytes[n]; } }
        public int Count { get { return bytes.Count; } }
        public BitBuffer() {
            this.bytes = new List<Byte>();
        }
        public void Write(UInt64 value, int offset, int width) {
            Console.WriteLine($"buf.Write({value:X8},{offset},{width});");
            var bytePos = offset / 8;
            var bitPos = offset % 8;

            var needLen = (offset + width + 7) / 8;
            if (this.bytes.Count < needLen) {
                this.bytes.AddRange(Enumerable.Repeat((byte)0, needLen - this.bytes.Count));
            }

            if (bitPos != 0) {
                var writeWidth = 8 - bitPos;
                if (width < writeWidth) {
                    writeWidth = width;
                }
                var srcmask = (byte)((1U << writeWidth) - 1);
                var dstmask = (byte)(~(srcmask << bitPos));

                var dstValue = (byte)(this.bytes[bytePos] & dstmask);
                var srcValue = (byte)((byte)value & srcmask);

                var margedValue = (byte)(dstValue | (srcValue << bitPos));
                this.bytes[bytePos] = margedValue;
                bytePos++;
                width -= writeWidth;
                value >>= writeWidth;
                bitPos = 0;
            }
            while (width >= 8) {
                this.bytes[bytePos] = (byte)(value & 0xFF);
                bytePos++;
                width -= 8;
                value >>= 8;
            }
            if (width > 0) {
                var mask = (byte)((1U << width) - 1);
                this.bytes[bytePos] = (byte)((this.bytes[bytePos] & (byte)~mask) | ((byte)value & mask));
            }
        }

        public IEnumerator<byte> GetEnumerator() {
            foreach (var v in this.bytes) {
                yield return v;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        internal static void Test() {
            {
                BitBuffer buf1 = new BitBuffer();
                buf1.Write(0xFFFFFFFFFFFFFFFF, 0, 12);
                buf1.Write(0x000000FF, 12, 6);
                buf1.Write(0xFFFFFFFFFFFFFFFF, 18, 63);
                buf1.Write(0x000000FF, 81, 4);
                buf1.Write(0xFFFFFFFFFFFFFFFF, 85, 2);

                BitBuffer buf2 = new BitBuffer();
                buf2.Write(0xFFFFFFFFFFFFFFFF, 85, 2);
                buf2.Write(0x000000FF, 81, 4);
                buf2.Write(0xFFFFFFFFFFFFFFFF, 18, 63);
                buf2.Write(0x000000FF, 12, 6);
                buf2.Write(0xFFFFFFFFFFFFFFFF, 0, 12);

                System.Diagnostics.Debug.Assert(buf1.SequenceEqual(buf2));
            }
            {

                BitBuffer buf1 = new BitBuffer();
                buf1.Write(0xFFFFFFFFFFFFFFFF, 0, 12);
                buf1.Write(0x000000FF, 12, 6);
                buf1.Write(0xFFFFFFFFFFFFFFFF, 18, 63);
                buf1.Write(0x000000FF, 81, 4);
                buf1.Write(0xFFFFFFFFFFFFFFFF, 85, 2);

                BitBuffer buf2 = new BitBuffer();
                buf2.Write(0xFFFFFFFFFFFFFFFF, 85, 2);
                buf2.Write(0x000000FF, 81, 4);
                buf2.Write(0xFFFFFFFFFFFFFFFF, 18, 63);
                buf2.Write(0x000000FF, 12, 6);
                buf2.Write(0xFFFFFFFFFFFFFFFF, 0, 12);
            }
        }
    }

}


/*
 * C89 Features 
 * - Lexer   : Complete
 * - Parser  : Complete
 * - Library : NO (now use glibc)
 * C99 Features
 * - __func__ : Complete
 * - Line comment : Complete
 * - Mix Statement and Declaration : Complete
 * - Flexible array member : Complete
 * - Variable length array : Not Supported. It's nothing, but harmful. 
 * - Complex numbers : Complete
 * - Imaginary numbers : Complete.
 * - Bool type : Complete.
 * - Long double type: Incomplete
 * 
 */
