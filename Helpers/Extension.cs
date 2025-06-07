using System.Windows.Media;
using ttvmin.Models;

namespace ttvmin.Helpers;

public static class Extension
{
	public static Color ToColor(this ColorFormatProvider provider) => provider.Color;
}
