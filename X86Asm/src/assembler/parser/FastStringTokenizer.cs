using System;
using System.IO;
using System.Text;

namespace X86Asm.parser
{



	public class FastStringTokenizer : Tokenizer
	{

		private static string read(Stream file)
		{
			if (file == null)
			{
				throw new ArgumentNullException();
			}
			return new StreamReader(file).ReadToEnd() + "\n";
		}



		private string sourceCode;

		private int offset;



		public FastStringTokenizer(Stream file)
		{
			if (file == null)
			{
				throw new ArgumentNullException();
			}
			sourceCode = read(file);
			offset = 0;
		}


		public FastStringTokenizer(string sourceCode)
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
			int start = offset;

			switch (peekChar())
			{
				case -1:
					return new Token(TokenType.END_OF_FILE, "");

				case '$':
					offset++;
					return new Token(TokenType.DOLLAR, "$");

				case '(':
					offset++;
					return new Token(TokenType.LEFT_PAREN, "(");

				case ')':
					offset++;
					return new Token(TokenType.RIGHT_PAREN, ")");

				case ',':
					offset++;
					return new Token(TokenType.COMMA, ",");

				case '+':
					offset++;
					return new Token(TokenType.PLUS, "+");

				case '-':
					offset++;
					if (isDecimal(peekChar()))
					{
						offset++;
						while (isDecimal(peekChar()))
						{
							offset++;
						}
						return new Token(TokenType.DECIMAL, sourceCode.Substring(start, offset - start));
					}
					else
					{
						return new Token(TokenType.MINUS, "-");
					}

				case '\n':
				case '\r':
					offset++;
					while (peekChar() == '\r' || peekChar() == '\n')
					{
						offset++;
					}
					return new Token(TokenType.NEWLINE, sourceCode.Substring(start, offset - start));

				case ' ': // Whitespace
				case '\t':
					offset++;
					while (peekChar() == ' ' || peekChar() == '\t')
					{
						offset++;
					}
					return next();

				case '#': // Comment
					offset++;
					while (peekChar() != '\r' && peekChar() != '\n')
					{
						offset++;
					}
					return next();

				case '%':
					offset++;
					if (!isNameStart(peekChar()))
					{
						throw new Exception("No token pattern match");
					}
					offset++;
					while (isNameContinuation(peekChar()))
					{
						offset++;
					}
					return new Token(TokenType.REGISTER, sourceCode.Substring(start, offset - start));

				case '0':
					offset++;
					if (peekChar() == -1)
					{
						return new Token(TokenType.DECIMAL, "0");
					}
					if (peekChar() != 'x' && peekChar() != 'X')
					{
						while (isDecimal(peekChar()))
						{
							offset++;
						}
						return new Token(TokenType.DECIMAL, sourceCode.Substring(start, offset - start));
					}
					else
					{
						offset++;
						while (isHexadecimal(peekChar()))
						{
							offset++;
						}
						return new Token(TokenType.HEXADECIMAL, sourceCode.Substring(start, offset - start));
					}

				default:
					if (isNameStart(peekChar()))
					{
						offset++;
						while (isNameContinuation(peekChar()))
						{
							offset++;
						}

						if (peekChar() == ':')
						{
							offset++;
							return new Token(TokenType.LABEL, sourceCode.Substring(start, offset - start));
						}
						else
						{
							return new Token(TokenType.NAME, sourceCode.Substring(start, offset - start));
						}

					}
					else if (isDecimal(peekChar()))
					{
						offset++;
						while (isDecimal(peekChar()))
						{
							offset++;
						}
						return new Token(TokenType.DECIMAL, sourceCode.Substring(start, offset - start));

					}
					else
					{
						throw new Exception("No token pattern match");
					}
			}
		}


		private int peekChar()
		{
			if (offset == sourceCode.Length)
			{
				return -1;
			}
			else
			{
				return sourceCode[offset];
			}
		}


		private static bool isNameStart(int c)
		{
			return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_';
		}


		private static bool isNameContinuation(int c)
		{
			return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_';
		}


		private static bool isDecimal(int c)
		{
			return c >= '0' && c <= '9';
		}


		private static bool isHexadecimal(int c)
		{
			return c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f' || c >= '0' && c <= '9';
		}

	}

}