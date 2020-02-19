using System;
using System.Collections.Generic;

namespace X86Asm
{



	/// <summary>
	/// An assembly language program. A program consists of a list of statements. Program objects are mutable.
	/// </summary>
	public sealed class Program
	{

		/// <summary>
		/// The list of statements. </summary>
		private List<Statement> statements;



		/// <summary>
		/// Constructs a program with an empty list of statements.
		/// </summary>
		public Program()
		{
			statements = new List<Statement>();
		}



		/// <summary>
		/// Returns a read-only view of the list of statements in this program. The data will change as the program changes. </summary>
		/// <returns> the list of statements in this program </returns>
		public IReadOnlyList<Statement> Statements
		{
			get
			{
				return statements;
			}
		}


		/// <summary>
		/// Appends the specified statement to the list of statements in this program. </summary>
		/// <param name="statement"> the statement to append </param>
		public void addStatement(Statement statement)
		{
			if (statement == null)
			{
				throw new ArgumentNullException();
			}
			statements.Add(statement);
		}

	}

}