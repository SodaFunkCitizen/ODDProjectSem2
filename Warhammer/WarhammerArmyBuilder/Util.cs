using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WarhammerArmyBuilder.Util
{   public static class Guard
    {
        // This class is used to validate input and throw exceptions if the input is invalid.
        public static string NotNullOrWhiteSpace(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} cannot be empty.", fieldName);
            return value.Trim();
        }
        public static int NonNegative(int value, string fieldName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(fieldName, "Value cannot be negative.");
            return value;
        }
    }

}
