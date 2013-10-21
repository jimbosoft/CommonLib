using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    /// <summary>
    /// End of Raceday Notify
    /// </summary>
    public class CDEFMessage0D4F : CDEFMessage3316
    {
        public CDEFMessage0D4F() : base()
        {
            mFunctionCode = (Int16)FuncCode.EndOfRacedayNotify;
        }
		public CDEFMessage0D4F(byte[] newMessage) : base(newMessage)
		{
		}

        public CDEFMessage0D4F(CDEFMessage newCDEF)
            : base(newCDEF)
		{
		}
   }
}
