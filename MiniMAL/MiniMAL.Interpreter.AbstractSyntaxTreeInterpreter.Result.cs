namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class AbstractSyntaxTreeInterpreter
        {
            /// <summary>
            /// •]‰¿Œ‹‰Ê
            /// </summary>
            public class Result
            {
                public Result(string id, Environment<ExprValue> env, ExprValue value)
                {
                    Id = id;
                    Env = env;
                    Value = value;
                }

                public string Id { get; }
                public Environment<ExprValue> Env { get; }
                public ExprValue Value { get; }
            }
        }
    }
}