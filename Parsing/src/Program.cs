using System.IO;
using System.Runtime.InteropServices;
using Parsing;

namespace cc
{
    class Program
    {
        static void Main(string[] args)
        {

            foreach (var arg in args) {
                using (var reader = new System.IO.StringReader("char * const * x;")) {
                    var target = new Source("", reader);
                    var ret = CParser.translation_unit(target, Position.Empty, Position.Empty, new CParser.ParserStatus());
                }
                using (var reader = new StreamReader(arg)) {
                    var target = new Source("", reader);
                    var ret = CParser.translation_unit(target, Position.Empty, Position.Empty, new CParser.ParserStatus());
                }

            }

        }
    }
}



