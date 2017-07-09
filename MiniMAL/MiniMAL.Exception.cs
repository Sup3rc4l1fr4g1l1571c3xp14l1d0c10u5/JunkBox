namespace MiniMAL {
    public abstract class Exception : System.Exception {
        protected Exception() { }
        protected Exception(string s) : base(s) { }
        protected Exception(string s, Exception e) : base(s, e) { }

        public class NotBound : Exception {
            public NotBound() {
            }
            public NotBound(string s) : base(s) {
            }
            public NotBound(string s, Exception e) : base(s, e) {
            }
        }

        public class HaltException : Exception {
            public HaltException() {
            }
            public HaltException(string s) : base(s) {
            }
            public HaltException(string s, Exception e) : base(s, e) {
            }
        }

        public class InvalidArgumentNumException : Exception {
            public InvalidArgumentNumException() {
            }
            public InvalidArgumentNumException(string s) : base(s) {
            }
            public InvalidArgumentNumException(string s, Exception e) : base(s, e) {
            }
        }

        public class InvalidArgumentTypeException : Exception {
            public InvalidArgumentTypeException() {
            }
            public InvalidArgumentTypeException(string s) : base(s) {
            }
            public InvalidArgumentTypeException(string s, Exception e) : base(s, e) {
            }
        }

        public class EvaluateException : Exception {
            public EvaluateException() {
            }
            public EvaluateException(string s) : base(s) {
            }
            public EvaluateException(string s, Exception e) : base(s, e) {
            }
        }

        public class TypingException : Exception {
            public TypingException() {
            }
            public TypingException(string s) : base(s) {
            }
            public TypingException(string s, Exception e) : base(s, e) {
            }
        }

        public class RuntimeErrorException : Exception {
            public RuntimeErrorException() {
            }
            public RuntimeErrorException(string s) : base(s) {
            }
            public RuntimeErrorException(string s, Exception e) : base(s, e) {
            }
        }

    }
}
