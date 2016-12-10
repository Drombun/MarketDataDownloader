using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDD.Library.Helpers
{
    public static class StringExtensions
    {
        public static string ToShortenedString(this long value, int digits = 4, int digitsFloor = 1, string format = "#,0", RoundingMode roundingMode = RoundingMode.None)
        {
            const long billion = 1000000000;
            const long million = 1000000;
            string pref = "";
            double correctNumber = value;

            if (value >= billion)
            {
                correctNumber = correctNumber / billion;
                pref = "B";
            }
            else if (value >= million)
            {
                correctNumber = correctNumber / million;
                pref = "M";
            }
            else if (value >= Math.Pow(10, digits))
            {
                correctNumber = value * 0.001;

                if (correctNumber + 1 > 1000)
                {
                    correctNumber = 1;
                    pref = "M";
                }
                else
                {
                    pref = "K";
                }
            }

            double floorMultiplier = 1.0;

            for (int i = 0; i < digitsFloor; ++i)
                floorMultiplier *= 10.0;

            correctNumber = Math.Floor(correctNumber * floorMultiplier) / floorMultiplier;

            correctNumber = RoundingFunction(correctNumber, digitsFloor, roundingMode);

            return correctNumber.ToString(format, System.Globalization.CultureInfo.InvariantCulture) + pref;
        }

        private static double RoundingFunction(double value, int digitsFloor, RoundingMode roundingMode = RoundingMode.None)
        {
            if (roundingMode == RoundingMode.Up && digitsFloor != 0)
            {
                if ((value - Math.Truncate(value)) > 0)
                    value += 5 / Math.Pow(10, digitsFloor + 1); // if number is not an integer we need to increase last digit of 5
                return (double)Math.Round((decimal)value, digitsFloor, MidpointRounding.AwayFromZero);
            }
            else if (roundingMode == RoundingMode.Down)
                return Math.Round(value, digitsFloor, MidpointRounding.ToEven);

            return value;
        }

        public enum RoundingMode
        {
            None,
            Up,
            Down
        }
    }
}
