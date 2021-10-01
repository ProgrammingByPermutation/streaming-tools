namespace streaming_tools.Views.Converters {
    using System;
    using System.Globalization;
    using Avalonia.Data.Converters;

    /// <summary>
    ///     Converts back and forth from percentages.
    /// </summary>
    internal class RoundingValueConverter : IValueConverter {
        /// <summary>
        ///     Converts from an integer to a string representation of a percentage.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The string representation of a percentage in "#%" format.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToInt32(value) + "%";
        }

        /// <summary>
        ///     Convert from a string representation of a percentage back to a integer.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The integer value of a percentage.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (null == value) {
                return 0;
            }

            var justNumber = value.ToString()?.Replace("%", "");
            return System.Convert.ToInt32(justNumber);
        }
    }
}