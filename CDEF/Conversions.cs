using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.CDEF
{
    public class Conversions
    {
        static Int64 mTicksAt190003010000 = 0;
        static Int64 mTicksPerMilliSecond = 0;

        static void InitialiseTickValues()
        {
            DateTime dt;
            dt = new DateTime(1900, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            mTicksAt190003010000 = dt.Ticks;
            mTicksPerMilliSecond = dt.AddMilliseconds(1).Ticks - dt.Ticks;
        }

        /// <summary>
        /// Convert System.DateTime to CDEF TimeUTC as Int64
        /// </summary>
        /// <param name="dt">DateTime</param>
        /// <returns></returns>
        public static Int64 DateTimeToCdefTimeUtc(DateTime dt)
        {
            if (0 == mTicksAt190003010000) InitialiseTickValues();
            return (((dt.Kind == DateTimeKind.Local) ? dt.ToUniversalTime().Ticks : dt.Ticks) - mTicksAt190003010000) / mTicksPerMilliSecond;
        }

        /// <summary>
        /// Convert CDEF TimeUTC as Int64 to System.DateTime
        /// </summary>
        /// <param name="s64"></param>
        /// <returns></returns>
        public static DateTime CdefTimeUtcToDateTime(Int64 s64)
        {
            if (0 == mTicksAt190003010000) InitialiseTickValues();
            return new DateTime((s64 * mTicksPerMilliSecond) + mTicksAt190003010000, DateTimeKind.Utc);
        }

        /// <summary>
        /// Convert DateTime to CDEF Date as Uint16
        /// </summary>
        /// <param name="dt">Date</param>
        /// <returns>CDEF Date as UInt16</returns>
        public static UInt16 DateTimeToCdefDate(DateTime dt)
        {
            return (UInt16)((((dt.Year % 100) * 12 + (dt.Month - 1)) * 31) + (dt.Day - 1));
        }

        /// <summary>
        /// Convert CDEF Date as UInt16 to System.DateTime
        /// </summary>
        /// <param name="cdef">CDEF Date as UInt16</param>
        /// <returns>System.DateTime</returns>
        public static DateTime CdefDateToDateTime(UInt16 cdef)
        {
            int temp = (int)cdef & 0xFFFF;
            int Day = temp % 31 + 1;
            temp = temp / 31;
            int Month = temp % 12 + 1;
            temp = temp / 12;
            int Year = temp % 100;
            int thisYear = DateTime.Now.Year;
            int thisCentury = (thisYear / 100) * 100;
            Year = Year + thisCentury;
            if ((Year - thisYear) > 75) Year = Year - 100; // use -25 to +75 year sliding window
            return new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Convert DateTime to CDEF CreditCardExpiryDate as Uint16
        /// </summary>
        /// <param name="dt">Date</param>
        /// <returns>CDEF CreditCardExpiryDate as UInt16</returns>
        public static UInt16 DateTimeToCdefCreditCardExpiryDate(DateTime dt)
        {
            return (UInt16)(((dt.Year % 100) * 100) + (dt.Month));
        }

        /// <summary>
        /// Convert CDEF CreditCardExpiryDate as UInt16 to System.DateTime
        /// </summary>
        /// <param name="cdef">CDEF CreditCardExpiryDate as UInt16</param>
        /// <returns>System.DateTime</returns>
        public static DateTime CdefCreditCardExpiryDateToDateTime(UInt16 cdef)
        {
            int temp = (int)cdef & 0xFFFF;
            int Month = temp % 100;
            int Year = (temp / 100) % 100;
            int thisYear = DateTime.Now.Year;
            int thisCentury = (thisYear / 100) * 100;
            Year = Year + thisCentury;
            if ((Year - thisYear) > 75) Year = Year - 100; // use -25 to +75 year sliding window
            Month = Month + 1;
            if (Month > 12) { Month = 1; Year = Year + 1; }
            return new DateTime(Year, Month, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
        }

        /// <summary>
        /// Convert decimal to CDEF SDec8 as Int16
        /// </summary>
        /// <param name="d">the System.Decimal to convert</param>
        /// <param name="fractionPlaces">number of fractional places</param>
        /// <param name="fractionBase">fraction base</param>
        /// <returns>CDEF SDec8 as Int16</returns>
        public static Int16 DecimalToCdefSDec8(decimal d, int fractionPlaces, int fractionBase)
        {
            int resolution = 1;
            for (int x = 0; x < fractionPlaces; x++)
            {
                resolution = resolution * fractionBase;
            }
            if (resolution < 0 || resolution > 256)
            {
                throw new Exception("CdefSDec8ToDecimal does not support " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            Int64 temp = (Int64)(Math.Round(d * resolution, 0));
            bool sign = (((Int64.MinValue) & temp) != 0);
            if (sign) temp = -temp;
            Int64 fractionPart = temp % resolution;
            Int64 integralPart = temp / resolution;
            if (integralPart > 127 || integralPart < -127)
            {
                throw new Exception("DecimalToCdefSDec8 unable to represent " + d.ToString() + " with " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            Int64 result = (((integralPart << 8) & 0x7F00) | (fractionPart & 0xFF));
            if (sign) result = result | 0x8000;
            return (Int16)result;
        }

        /// <summary>
        /// Convert a CDEF SDec8 as Int16 to decimal
        /// </summary>
        /// <param name="cdef">CDEF SDec8 as Int16 - See the CDEF spec.</param>
        /// <param name="fractionPlaces">Get this from the CDEF spec</param>
        /// <param name="fractionBase">Get this from the CDEF spec</param>
        /// <returns>System.Decimal</returns>
        public static decimal CdefSDec8ToDecimal(Int16 cdef, int fractionPlaces, int fractionBase)
        {
            int resolution = 1;
            for (int x = 0; x < fractionPlaces; x++)
            {
                resolution = resolution * fractionBase;
            }
            if (resolution < 0 || resolution > 256)
            {
                throw new Exception("CdefSDec8ToDecimal does not support " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            bool sign = ((0x8000 & cdef) != 0);
            Int64 fractionPart = ((Int64)cdef) & 0xFF;
            Int64 integralPart = (((Int64)cdef) >> 8) & 0x7f;
            if (fractionPart >= resolution)
            {
                throw new Exception("CdefSDec8ToDecimal detected fractional part " + fractionPart.ToString() + " greater than allowed for " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            Int64 iResult = integralPart * resolution + fractionPart;
            if (sign) iResult = -iResult;
            decimal result = ((decimal)iResult) / resolution;
            return result;
        }

        /// <summary>
        /// Convert decimal to CDEF SDecNum as Int32
        /// </summary>
        /// <param name="d">the System.Decimal to convert</param>
        /// <param name="fractionPlaces">number of fractional places</param>
        /// <param name="fractionBase">fraction base</param>
        /// <returns>CDEF SDecNum as Int32</returns>
        public static Int32 DecimalToCdefSDecNum(decimal d, int fractionPlaces, int fractionBase)
        {
            int resolution = 1;
            for (int x = 0; x < fractionPlaces; x++)
            {
                resolution = resolution * fractionBase;
            }
            if (resolution < 0 || resolution > 65536)
            {
                throw new Exception("DecimalToCdefSDecNum does not support " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            Int64 temp = (Int64)(Math.Round(d * resolution, 0));
            bool sign = (((Int64.MinValue) & temp) != 0);
            if (sign) temp = -temp;
            Int64 fractionPart = temp % resolution;
            Int64 integralPart = temp / resolution;
            if (integralPart > 32767 || integralPart < -32767)
            {
                throw new Exception("DecimalToCdefSDecNum unable to represent " + d.ToString() + " with " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            Int64 result = (((integralPart << 16) & 0x7FFF0000) | (fractionPart & 0xFFFF));
            if (sign) result = result | 0x80000000;
            return (Int32)result;
        }

        /// <summary>
        /// Convert a CDEF SDecNum as Int32 to decimal
        /// </summary>
        /// <param name="cdef">CDEF SDecNum as Int32 - See the CDEF spec.</param>
        /// <param name="fractionPlaces">Get this from the CDEF spec</param>
        /// <param name="fractionBase">Get this from the CDEF spec</param>
        /// <returns>System.Decimal</returns>
        public static decimal CdefSDecNumToDecimal(Int32 cdef, int fractionPlaces, int fractionBase)
        {
            int resolution = 1;
            for (int x = 0; x < fractionPlaces; x++)
            {
                resolution = resolution * fractionBase;
            }
            if (resolution < 0 || resolution > 65536)
            {
                throw new Exception("CdefSDecNumToDecimal does not support " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            bool sign = ((0x80000000 & cdef) != 0);
            Int64 fractionPart = ((Int64)cdef) & 0xFFFF;
            Int64 integralPart = (((Int64)cdef) >> 16) & 0x7FFF;
            if (fractionPart >= resolution)
            {
                throw new Exception("CdefSDecNumToDecimal detected fractional part " + fractionPart.ToString() + " greater than allowed for " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            Int64 iResult = integralPart * resolution + fractionPart;
            if (sign) iResult = -iResult;
            decimal result = ((decimal)iResult) / resolution;
            return result;
        }

        /// <summary>
        /// Convert decimal to CDEF Dec32 as UInt64
        /// </summary>
        /// <param name="d">the System.Decimal to convert</param>
        /// <param name="fractionPlaces">number of fractional places</param>
        /// <param name="fractionBase">fraction base</param>
        /// <returns>CDEF Dec32 as UInt64</returns>
        public static UInt64 DecimalToCdefDec32(decimal d, int fractionPlaces, int fractionBase)
        {
            if (d < 0.0M)
            {
                throw new Exception("DecimalToCdefDec32 cannot represent the negative number " + d.ToString() + ".");
            }
            UInt64 resolution = 1;
            for (int x = 0; x < fractionPlaces; x++)
            {
                resolution = resolution * (((UInt64)fractionBase) & 0x00000000FFFFFFFFL);
            }
            if (resolution > 0x0000000100000000L)
            {
                throw new Exception("DecimalToCdefDec32 does not support " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            UInt64 integralPart = (UInt64)(Math.Round(d * resolution, 0));
            UInt64 fractionPart = integralPart % resolution;
            integralPart = integralPart / resolution;
            if (integralPart > 0x00000000FFFFFFFFL)
            {
                throw new Exception("DecimalToCdefDec32 cannot represent the large number " + d.ToString() + ".");
            }
            UInt64 result = ((integralPart & 0x00000000FFFFFFFFL) | ((fractionPart << 32) & 0xFFFFFFFF00000000L));
            return result;
        }

        /// <summary>
        /// Convert a CDEF Dec32 as UInt64 to decimal
        /// </summary>
        /// <param name="cdef">CDEF Dec32 as UInt64 - See the CDEF spec.</param>
        /// <param name="fractionPlaces">Get this from the CDEF spec</param>
        /// <param name="fractionBase">Get this from the CDEF spec</param>
        /// <returns>System.Decimal</returns>
        public static decimal CdefDec32ToDecimal(UInt64 cdef, int fractionPlaces, int fractionBase)
        {
            UInt64 resolution = 1;
            for (int x = 0; x < fractionPlaces; x++)
            {
                resolution = resolution * (((UInt64)fractionBase) & 0x00000000FFFFFFFFL);
            }
            if (resolution > 0x0000000100000000L)
            {
                throw new Exception("CdefDec32ToDecimal does not support " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            UInt64 fractionPart = (cdef >> 32) & 0x00000000FFFFFFFFL;
            UInt64 integralPart = (cdef) & 0x00000000FFFFFFFFL;
            if (fractionPart >= resolution)
            {
                throw new Exception("CdefDec32ToDecimal detected fractional part " + fractionPart.ToString() + " greater than allowed for " + fractionPlaces.ToString() + " fractional places of base " + fractionBase.ToString() + ".");
            }
            UInt64 iResult = integralPart * resolution + fractionPart;
            decimal result = ((decimal)iResult) / resolution;
            return result;
        }

    }
}
