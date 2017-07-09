using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMAL.Test
{
    public static class TestHelper
    {
        public static TException GetException<TException>(Action action) where TException : System.Exception
        {
            try
            {
                action();
                return null;
            }
            catch (TException ex)
            {
                return ex;
            }
        }

    }
}
