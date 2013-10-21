using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CommonLib.Utilities
{
    public class GUI_Helper
    {
        static public bool IsPositivNumeric(string txt)
        {
            return IsNumeric(txt, 0, "");
        }
        //---------------------------------------------------------------------
        static public bool IsNumeric(string txt, int minVal, string name)
        {
            bool ret = true;
            try
            {
                if (txt == null || txt.Trim().Length == 0)
                {
                    MessageBox.Show("Blank \"" + name + "\" field detected. Please correct."
                        , "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                long res = Convert.ToInt64(txt);
                if (res < minVal)
                {
                    MessageBox.Show("Entry " + name + ": " + txt + " is must larger then " + minVal.ToString()
                        + " Please correct."
                        , "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ret = false;
                }
            }
            catch
            {
                MessageBox.Show("Entry: " + txt + " is not numeric. Please correct."
                    , "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ret = false;
            }
            return ret;
        }
    }
}
