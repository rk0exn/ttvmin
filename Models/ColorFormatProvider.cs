using System;
using System.Reflection;
using System.Windows.Media;

namespace ttvmin.Models;

public sealed class ColorFormatProvider : IFormatProvider, ICustomFormatter
{
	public Color Color { get; private set; }

	public ColorFormatProvider(string colorString) => Color = ParseColor(colorString);

	private Color ParseColor(string input)
	{
		if (string.IsNullOrWhiteSpace(input)) return Colors.Transparent;
		input = input.Trim();
		var prop = typeof(Colors).GetProperty(input, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
		if (prop != null && prop.PropertyType == typeof(Color)) return (Color)prop.GetValue(null);
		if (input.StartsWith("#"))
		{
			var hex = input.Substring(1);
			try
			{
				if (hex.Length == 6) return Color.FromRgb(Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
			}
			catch { }
		}
		return Colors.Transparent;
	}

	public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;
	public string Format(string format, object arg, IFormatProvider formatProvider) => Color.ToString();
}
