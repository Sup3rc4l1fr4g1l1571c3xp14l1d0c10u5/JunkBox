using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace X86Asm.parser
{



	public sealed class StringTokenizer : Tokenizer
	{

		private class TokenPattern
		{

			public readonly Regex pattern;

			public readonly TokenType tokenType;


			public TokenPattern(string pattern, TokenType tokenType)
			{
				if (pattern == null)
				{
					throw new ArgumentNullException();
				}
				this.pattern = new Regex(pattern);
				this.tokenType = tokenType;
			}


			public override string ToString()
			{
				return $"[{tokenType}: /{pattern}/]";
			}

		}



		private static IList<TokenPattern> patterns;

		static StringTokenizer()
		{
			patterns = new List<TokenPattern>();
			patterns.Add(new TokenPattern("[ \t]+", TokenType.WHITESPACE)); // Whitespace
			patterns.Add(new TokenPattern("[A-Za-z_][A-Za-z0-9_]*:", TokenType.LABEL));
			patterns.Add(new TokenPattern("[A-Za-z_][A-Za-z0-9_]*", TokenType.NAME));
			patterns.Add(new TokenPattern("%[A-Za-z][A-Za-z0-9_]*", TokenType.REGISTER));
			patterns.Add(new TokenPattern("0[xX][0-9a-fA-F]+", TokenType.HEXADECIMAL));
			patterns.Add(new TokenPattern("-?[0-9]+", TokenType.DECIMAL));
			patterns.Add(new TokenPattern("\\$", TokenType.DOLLAR));
			patterns.Add(new TokenPattern(",", TokenType.COMMA));
			patterns.Add(new TokenPattern("\\+", TokenType.PLUS));
			patterns.Add(new TokenPattern("-", TokenType.MINUS));
			patterns.Add(new TokenPattern("\\(", TokenType.LEFT_PAREN));
			patterns.Add(new TokenPattern("\\)", TokenType.RIGHT_PAREN));
			patterns.Add(new TokenPattern("[\n\r]+", TokenType.NEWLINE));
			patterns.Add(new TokenPattern("#[^\n\r]*", TokenType.WHITESPACE)); // Comment
			patterns.Add(new TokenPattern("$", TokenType.END_OF_FILE));
		}


		/// <summary>
		/// Reads the contents of the specified file, appends a newline character ({@code '\n'}), and returns the result. </summary>
		/// <param name="file"> the file to read from </param>
		/// <returns> the contents of the file as a string, plus a trailing newline </returns>
		private static string read(Stream file)
		{
			if (file == null)
			{
				throw new ArgumentNullException();
			}

			return new StreamReader(file).ReadToEnd();
		}



		private string sourceCode;

		private int offset;



		public StringTokenizer(Stream file)
		{
			if (file == null)
			{
				throw new ArgumentNullException();
			}
			sourceCode = read(file);
			offset = 0;
		}


		public StringTokenizer(string sourceCode)
		{
			if (sourceCode == null)
			{
				throw new ArgumentNullException();
			}
			this.sourceCode = sourceCode;
			offset = 0;
		}



		/// <summary>
		/// Returns the next token from this tokenizer. </summary>
		/// <returns> the next token from this tokenizer </returns>
		public override Token next()
		{
			foreach (TokenPattern pat in patterns)
			{
				string m = match(pat.pattern);
				if (m != null)
				{
					offset += m.Length;
					if (pat.tokenType != TokenType.WHITESPACE)
					{
						return new Token(pat.tokenType, m);
					}
					else
					{
						return next();
					}
				}
			}
			throw new Exception("No token pattern match");
		}


		private string match(Regex pattern)
		{
			var m = pattern.Match(sourceCode,offset);
			if (m.Success && m.Index == offset)
			{
                return m.Value;
			}
			else
			{
                return null;
			}
		}

	}

}