using System.IO;
using Parsing;

namespace CParser2
{
    class Program
    {
        static void Main(string[] args)
        {

            foreach (var arg in args)
            {
                //using (var reader = new System.IO.StringReader("x = 1"))
                //{
                //    var target = new Source("", reader);
                //    var ret = CParser.assignment_expression(target, Position.Empty, Position.Empty);
                //}
                using (var reader = new StreamReader(arg))
                {
                    var target = new Source("", reader);
                    var ret = CParser.translation_unit(target, Position.Empty, Position.Empty, new CParser.ParserStatus());
                }
            }


        }
    }
}



