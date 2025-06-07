using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ttvmin.Models;

namespace ttvmin.Helpers;

public sealed class ColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string colorString = value as string;
		if (string.IsNullOrWhiteSpace(colorString)) return Brushes.Transparent;

		try
		{
			var color = new ColorFormatProvider(colorString).ToColor();
			return new SolidColorBrush(color);
		}
		catch
		{
			return Brushes.Transparent;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}
