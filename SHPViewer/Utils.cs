using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SHPViewer.Utils
{
    public class Utils
    {
        static int GetIntegerDigitCount(int valueInt)
        {
            double value = valueInt;
            int sign = 0;
            if (value < 0)
            {
                value = -value;
                sign = 1;
            }
            if (value <= 9)
            {
                return sign + 1;
            }
            if (value <= 99)
            {
                return sign + 2;
            }
            if (value <= 999)
            {
                return sign + 3;
            }
            if (value <= 9999)
            {
                return sign + 4;
            }
            return sign + 5;
        }
    }
}
