using System;
using System.Diagnostics;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {
            if (Debugger.IsAttached == false) {
                CommonMain(args);
            } else {
                DebugMain(args);
            }
        }

        static void CommonMain(string[] args) {
            string outputFile = null;
            string astFile = null;
            bool flag_SyntaxOnly = false;
            string outputEncoding = null;

            args = new CommandLineOptionsParser()
                .Entry("-o", 1, (s) => {
                    outputFile = s[0];
                    return true;
                })
                .Entry("-ast", 1, (s) => {
                    astFile = s[0];
                    return true;
                })
                .Entry("-console-output-encoding", 1, (s) => {
                    outputEncoding = s[0];
                    return true;
                })
                .Entry("-fsyntax-only", 0, (s) => {
                    flag_SyntaxOnly = true;
                    return true;
                })
                .Parse(args);

            
            try {
                if (outputEncoding != null) {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(outputEncoding);
                }
            } catch {
                Logger.Error($"指定されたエンコーディング ${outputEncoding}は不正です。");
                Environment.Exit(-1);
            }


            if (args.Length == 0) {
                Logger.Error("コンパイル対象のCソースファイルを１つ指定してください。");
                Environment.Exit(-1);
            } else if (args.Length > 1) {
                Logger.Error("コンパイル対象のCソースファイルが２つ以上指定されています。");
                Environment.Exit(-1);
            } else {
                var arg = args[0];

                if (System.IO.File.Exists(arg) == false) {
                    Logger.Error($"ファイル {arg} が見つかりません。処理を中止します。");
                    Environment.Exit(-1);
                }
                if (outputFile == null) {
                    outputFile = System.IO.Path.ChangeExtension(arg, "s");
                }
                if (astFile == null) {
                    astFile = System.IO.Path.ChangeExtension(arg, "ast");
                }
                try {
                    var ret = new Parser(System.IO.File.ReadAllText(arg), arg).Parse();
                    using (var o = new System.IO.StreamWriter(astFile)) {
                        o.WriteLine(ret.Accept(new SyntaxTreeDumpVisitor(), null).ToString());
                    }

                    if (flag_SyntaxOnly == false) {
                        using (var o = new System.IO.StreamWriter(outputFile)) {
                            var v = new SyntaxTreeCompileVisitor.Value();
                            var visitor = new SyntaxTreeCompileVisitor();
                            ret.Accept(visitor, v);
                            visitor.WriteCode(o);
                        }
                    }
                } catch (Exception e) {
                    if (e is CompilerException) {
                        var ce = e as CompilerException;
                        Logger.Error(ce.Start, ce.End, ce.Message);
                    } else {
                        Logger.Error(e.Message);
                    }
                    Logger.Error(e.StackTrace);
                    Environment.Exit(-1);
                }
            }

        }

        static void DebugMain(string[] args) {
                        var ret = new Parser(System.IO.File.ReadAllText(@"..\..\tcctest\00_assignment.c"), "<Debug>").Parse();
            var sexpr = ret.Accept(new SyntaxTreeDumpVisitor(), null);

            var interpreter = new Lisp.SchemeInterpreter();
            interpreter.InterpreterWantsToPrint += (s,e) => Console.Write(e.WhatToPrint);
            interpreter.Evaluate($"(define ast '{new Lisp.Writer(false).Write(sexpr)})");
            interpreter.Evaluate(@"(define repl (lambda () (let ((expr (begin (display ""cc1> "") (read (standard-input-port))))) (begin (write (eval expr)) (newline) (repl)))))");
            interpreter.Evaluate(@"
(define pp (lambda (s)
  (define do-indent (lambda (level)
    (dotimes (_ level) (write-char #\space))))
  (define pp-parenl (lambda ()
    (write-char #\()))
  (define pp-parenr (lambda ()
    (write-char #\))))
  (define pp-atom (lambda (e prefix)
    (when prefix (write-char #\space))
    (write e)))
  (define pp-list (lambda (s level prefix)
    (and prefix (do-indent level))
    (pp-parenl)
    (let loop ((s s)
               (prefix #f))
      (if (null? s)
          (pp-parenr)
          (let ((e (car s)))
            (if (list? e)
                (begin (and prefix (newline))
                       (pp-list e (+ level 1) prefix))
                (pp-atom e prefix))
            (loop (cdr s) #t))))))
  (if (list? s)
      (pp-list s 0 #f)
      (write s))
  (newline)))
");
            interpreter.Evaluate(@"(repl)");

            var v = new SyntaxTreeCompileVisitor.Value();
            using (var o = new System.IO.StreamWriter(@"..\..\test.s")) {
                var visitor = new SyntaxTreeCompileVisitor();
                ret.Accept(visitor, v);
                visitor.WriteCode(o);
            }
        }
    }   
}
