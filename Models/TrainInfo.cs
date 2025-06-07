using System.Text.Json.Serialization;
using System.Windows;

namespace ttvmin.Models;

public class TrainInfoWithoutTime : DependencyObject
{
	[JsonPropertyName("direction")]
	public string Direction { get; set; }

	[JsonPropertyName("upside")]
	public bool? Upside { get; set; }

	[JsonPropertyName("traintype")]
	public string TrainType { get; set; }

	[JsonPropertyName("next_station")]
	public string NextStation { get; set; }

	public string ImageColor { get; set; }

	public string UpsideText => Upside == null && IsTop ? "方面" : Upside == true ? "上り" : Upside == false ? "下り" : "";

	public bool IsTop { get; set; }
}

public class TrainInfo : TrainInfoWithoutTime
{
	[JsonPropertyName("time")]
	public string Time { get; set; }
}

public sealed class ExtTrainInfo : TrainInfo
{
	[JsonPropertyName("pattern")]
	public string PatternName { get; set; }

	public bool UseV2 => !string.IsNullOrWhiteSpace(PatternName);
}
