using System;
using System.IO;
using System.Runtime.InteropServices;
using Parsing;

namespace CParser2
{
    class Program
    {
        static void Main(string[] args)
        {

            foreach (var arg in args) {
                using (var reader = new System.IO.StringReader(@"     ( (parser->tokenbuf) [ (parser->tokidx) ]='\0') ;")) {
                    var target = new Source("", reader);
                    var ret = CParser.expression_statement(target, Position.Empty, Position.Empty, new CParser.ParserStatus());
                }
                using (var reader = new StreamReader(arg)) {
                    var target = new Source("", reader);

                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    var ret = CParser.translation_unit(target, Position.Empty, Position.Empty, new CParser.ParserStatus());
                    sw.Stop();
                    Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                }

            }

        }
    }
}



