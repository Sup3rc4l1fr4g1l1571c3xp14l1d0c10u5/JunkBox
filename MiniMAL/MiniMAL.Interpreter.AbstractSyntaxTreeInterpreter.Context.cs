namespace MiniMAL
{
    namespace Interpreter
    {
        public static partial class AbstractSyntaxTreeInterpreter
        {
            public class Context
            {
                /// <summary>
                /// �l�̕]�����Ɏg����
                /// </summary>
                public Environment<ExprValue> Env { get; }
                /// <summary>
                /// �g�ݍ��ݒl�̊�
                /// </summary>
                public Environment<ExprValue> BuiltinEnv { get; }

                /// <summary>
                /// �^�̕]�����Ɏg����
                /// </summary>
                public Environment<Typing.PolymorphicTyping.TypeScheme> TypingEnv { get; }
                //public Environment<Typing.MonomorphicTyping> TypingEnv { get; }

                /// <summary>
                /// �^�錾�������
                /// </summary>
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