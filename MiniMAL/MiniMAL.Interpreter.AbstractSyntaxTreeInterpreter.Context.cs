namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class AbstractSyntaxTreeInterpreter
        {
            public class Context
            {
                public Environment<ExprValue> Env { get; }
                public Environment<ExprValue> BuiltinEnv { get; }
                public Environment<Typing.PolymorphicTyping.TypeScheme> TypingEnv { get; }
                //public Environment<Typing.MonomorphicTyping> TypingEnv { get; }
                public Environment<Typing.PolymorphicTyping.TypeScheme> TyEnv { get; }

                public Context()
                {
                    Env = Environment<ExprValue>.Empty;
                    BuiltinEnv = Environment<ExprValue>.Empty;
                    TypingEnv = Environment<Typing.PolymorphicTyping.TypeScheme>.Empty;
                    TyEnv = Environment<Typing.PolymorphicTyping.TypeScheme>.Empty;
                }
                public Context(Environment<ExprValue> env, Environment<ExprValue> builtinEnv, Environment<Typing.PolymorphicTyping.TypeScheme> typingEnv, Environment<Typing.PolymorphicTyping.TypeScheme> tyEnv)
                {
                    Env = env;
                    BuiltinEnv = builtinEnv;
                    TypingEnv = typingEnv;
                    TyEnv = tyEnv;
                }
            }
        }
    }
}