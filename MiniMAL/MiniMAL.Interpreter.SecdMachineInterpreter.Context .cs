namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class SecdMachineInterpreter
        {
            public class Context
            {
                public LinkedList<LinkedList<string>> NameEnv { get; }
                public LinkedList<LinkedList<ExprValue>> ValueEnv { get; }
                public Environment<ExprValue> BuiltinEnv { get; }
                public Environment<Typing.PolymorphicTyping.TypeScheme> TypingEnv { get; }
                //public Environment<Typing.MonomorphicTyping> TypingEnv { get; }

                public Context()
                {
                    NameEnv = LinkedList<LinkedList<string>>.Empty;
                    ValueEnv = LinkedList<LinkedList<ExprValue>>.Empty;
                    BuiltinEnv = Environment<ExprValue>.Empty;
                    TypingEnv = Environment<Typing.PolymorphicTyping.TypeScheme>.Empty;
                }
                public Context(LinkedList<LinkedList<string>> nameEnv, LinkedList<LinkedList<ExprValue>> valueEnv, Environment<ExprValue> builtinEnv, Environment<Typing.PolymorphicTyping.TypeScheme> typingEnv)
                {
                    NameEnv = nameEnv;
                    ValueEnv = valueEnv;
                    BuiltinEnv = builtinEnv;
                    TypingEnv = typingEnv;
                }
            }
        }
    }
}