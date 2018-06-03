using System;
using System.Diagnostics;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {

            //
            // I Do Not Know C. 
            //

            if (Debugger.IsAttached == false) {
                CommonMain(args);
            } else {
                DebugMain(args);
            }
        }

        private class CommandLineOptions {
            public string outputFile = null;
            public string astFile = null;
            public bool flagSyntaxOnly = false;
            public string outputEncoding = null;
            public string[] args = null;

            public void Validation(Action<string> act) {
                // check outputEncoding
                try {
                    if (outputEncoding != null) {
                        Console.OutputEncoding = System.Text.Encoding.GetEncoding(outputEncoding);
                    }
                } catch {
                    act($"指定されたエンコーディング ${outputEncoding}は不正です。");
                }

                // check input source
                if (args.Length == 0) {
                    act("コンパイル対象のCソースファイルを１つ指定してください。");
                } else if (args.Length > 1) {
                    act("コンパイル対象のCソースファイルが２つ以上指定されています。");
                } else if (System.IO.File.Exists(args[0]) == false) {
                    act($"ファイル {args[0]} が見つかりません。処理を中止します。");
                }

                // check output
                if (outputFile == null) {
                    outputFile = System.IO.Path.ChangeExtension(args[0], "s");
                }

            }
        }

        static void CommonMain(string[] args) {

            var opts = new CommandLineOptionsParser<CommandLineOptions>()
                .Entry("-o", 1, (t, s) => {
                    t.outputFile = s[0];
                    return true;
                })
                .Entry("-ast", 1, (t, s) => {
                    t.astFile = s[0];
                    return true;
                })
                .Entry("-console-output-encoding", 1, (t, s) => {
                    t.outputEncoding = s[0];
                    return true;
                })
                .Entry("-fsyntax-only", 0, (t, s) => {
                    t.flagSyntaxOnly = true;
                    return true;
                })
                .Default((t, s) => {
                    t.args = s;
                    return true;
                })
                .Parse(new CommandLineOptions(), args);

            opts.Validation((e) => {
                Logger.Error(e);
                Environment.Exit(-1);
            });

            try {
                var ret = new Parser(System.IO.File.ReadAllText(opts.args[0]), opts.args[0]).Parse();
                if (opts.astFile != null) {
                    using (var o = new System.IO.StreamWriter(opts.astFile)) {
                        o.WriteLine(ret.Accept(new ToSExprVisitor(), null).ToString());
                    }
                }

                if (opts.flagSyntaxOnly == false) {
                    using (var o = new System.IO.StreamWriter(opts.outputFile)) {
                        var compiler = new Compiler();
                        compiler.Compile(ret, o);
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
            var ret = new Parser(System.IO.File.ReadAllText(@"..\..\test.c"), "<Debug>").Parse();
            var sexpr = ret.Accept(new ToSExprVisitor(), null);

            var interpreter = new Lisp.SchemeInterpreter();
            interpreter.InterpreterWantsToPrint += (s, e) => Console.Write(e.WhatToPrint);
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
            //interpreter.Evaluate(@"(repl)");

            using (var o = new System.IO.StreamWriter(@"..\..\test.s")) {
                var compiler = new Compiler();
                compiler.Compile(ret, o);
            }

        }

#if false
#pragma pack(8)

// フレキシブル配列メンバは末尾にのみ置くことが出来る。
struct hoge {
	char str[];	// これはフレキシブル配列メンバ
	int x;
};


// フレキシブル配列メンバ自体は不完全型であり、構造体型側がそれを特別扱いするルールになっている
struct s { int n; double d[]; };

struct ss { int n; double d[1]; };

// タグ型の宣言はOK
struct sss { 
	struct s s1;
};

// 入れ子になっているフレキシブル配列メンバは初期化できないので以下のコードはエラーになる
//struct sss sss_bad = {{1, {2.0} }}; 
// こっちはOK
struct sss sss_ok1 = { {1} };

// これはポインタなのでOK
struct sss *sss_ok2 = 0;

// メンバにフレキシブル配列メンバを持つ構造体が複数あってもコンパイラは警告を出さない（gcc/clangでc99/c11どちらも）が
// CertC DCL38-C では制約として、以下を定めている
// ・フレキシブル配列メンバを含む構造体の配列は使えない
// ・フレキシブル配列メンバを含む構造体は他の構造体の途中になるメンバとしては使用できない
struct ssss { 
	struct s s1;	// これのオフセット位置は +0 になる
	struct s s2;	// これのオフセット位置は +sizeof(structs) になる。つまり、s1の末尾はs2と被った領域を示す。 
};


int main(void) {
	{
		// 規格書の例
	unsigned long sz1 = sizeof(struct s);
	unsigned long sz2 = (unsigned long)(&((struct s*)0)->d);
	unsigned long sz3 = (unsigned long)(&((struct ss*)0)->d);


	printf("sizeof(struct s) = %lu\n", sz1);
	printf("offsetof(struct s, d) = %lu\n", sz2);
	printf("offsetof(struct ss, d) = %lu\n", sz2);
	
	printf("sizeof(struct s) == offsetof(struct s, d) == offsetof(struct ss, d) = %d\n", sz1 == sz2 && sz2 == sz3);
	}
	

	{
		// フレキシブル配列メンバ自体は不完全型であって、構造体型側がそれを特別扱いしているだけなので
		// 「sizeofは不完全型に適用できない」のルールが適用されてエラーとなる
		/*
		unsigned long sz4 = sizeof(((struct s*)0)->d);	// これがエラー
		unsigned long sz5 = sizeof(((struct ss*)0)->d);	// これがエラー

		printf("sizeof(struct s, d) = %lu\n", sz4);	
		printf("sizeof(struct ss, d) = %lu\n", sz5);
		//*/
	}

	{
		static struct s x1 = {1, {2.0} };
		static struct s x2 = {1, {2.0, 3.0} };
		printf("sizeof(x1) = %lu\n", sizeof(x1));	// 当たり前だが変数のsizeofを求めた場合でも型が書き換わっているわけではないため sizeof(struct s)と同じ。
		printf("sizeof(x2) = %lu\n", sizeof(x2));

		printf("sizeof(x1) == sizeof(x2) = %d\n", sizeof(x1) == sizeof(x2));
	}

	{
		// フレキシブル配列メンバはコンパイル時にサイズが決定しなければならないタイプの型なので
		// auto変数の場合、それ単体で宣言できても初期化はできない。
		//*
		struct s x1;	// これはOK
		struct s x2 = {1, {2.0} };	// これはNG
		//*/
	}

	{
		// どうしてCert-Cが規格に無い制約を定めているかの例
		// うわぁ･･･酷い
		unsigned long sssz1 = sizeof(struct ssss);
		unsigned long sssz2 = (unsigned long)(&((struct ssss*)0)->s1.d);
		unsigned long sssz3 = (unsigned long)(&((struct ssss*)0)->s2.d);

		printf("sizeof(struct ssss) = %lu\n", sssz1);
		printf("offsetof(struct ssss, s1.d) = %lu\n", sssz2);	// 領域が
		printf("offsetof(struct ssss, s2.d) = %lu\n", sssz3);	// 被ってるやん！
		
		printf("sizeof(struct ssss) == offsetof(struct ssss, s1.d) == offsetof(struct ssss, s2.d) = %d\n", sssz1 == sssz2 && sssz2 == sssz3);
	}
	
	return 0;
}

#endif
    }
}


