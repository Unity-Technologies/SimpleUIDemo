using System;

namespace Unity.Tiny.Utils
{
    public static class NumberConverter
    {
        public static string FloatToString(float value, uint precision = 3)
        {
            return DoubleToString(value, precision);
        }

        public static string DoubleToString(double value, uint precision = 3)
        {
            if (double.IsNegativeInfinity(value))
                return "-Inf";
            if (double.IsPositiveInfinity(value))
                return "Inf";
            string result = "";
            if (value < 0.0) {
                result += "-";
                value = -value;
            }
            if (double.IsNaN(value)) {
                result += "NaN";
                return result;
            }

            int dec = (int)value;
            result += dec.ToString();
            result += ".";
            value -= dec;
            // remove leading zeros
            while ((int)(value*10.0) == 0) {
                value *= 10.0;
                result += "0";
                if (value==0.0)
                    return result;
            } 
            value *= Math.Pow(10, precision);
            result += ((int)value).ToString();
            return result;
        }
    }
}
