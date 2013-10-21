using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class CDEFAmount
    {
        public static Int64 FACTOR = 0x10000;
        public static int SHIFT_FACTOR = 16;
        public const int AMOUNT_FACTOR = 65536;

        public byte AmountID;
        public Int64 AmountVal;
        public CDEFAmount(byte id, Int64 amount)
        {
            AmountID = id;
            AmountVal = amount;
        }
    }
    public class CDEFRole
    {
        public string Group;
        public string RoleType;
        public CDEFRole(string g, string t)
        {
            Group = g;
            RoleType = t;
        }
    }
}
