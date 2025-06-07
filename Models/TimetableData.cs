using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ttvmin.Models;

public sealed class TimetableData
{
	[JsonPropertyName("station_name")]
	public string StationName { get; set; }

	[JsonPropertyName("weekdays_timetable")]
	public List<ExtTrainInfo> WeekDaysTimetable { get; set; }

	[JsonPropertyName("holidays_timetable")]
	public List<ExtTrainInfo> HolidaysTimetable { get; set; }

	[JsonPropertyName("update_time")]
	public string UpdateTime { get; set; }

	[JsonPropertyName("type_colors")]
	public Dictionary<string, string> TypeColors { get; set; }

	[JsonPropertyName("patterns")]
	public Dictionary<string, TrainInfoWithoutTime> Patterns { get; set; }

	[JsonPropertyName("comment")]
	public string Comment { get; set; }

	private bool _usingPattern = false;
	public bool UsingPattern => _usingPattern;

	public List<TrainInfo> SafeWeekdaysTimetable
	{
		get
		{
			if (Patterns == null) return ToTrainInfoList(WeekDaysTimetable?.ToList());
			List<TrainInfo> result = [];
			var data = WeekDaysTimetable.ToList() ?? null;
			if (data == null) return [];
			for (var i = 0; i < data.Count; i++)
			{
				if (!data[i].UseV2)
				{
					result.Add(data[i]);
					continue;
				}
				if (Patterns.TryGetValue(data[i].PatternName, out var ttime))
				{
					result.Add(ToTrainInfo(data[i], ttime));
				}
				else
				{
					return [];
				}
			}
			if (!UsingPattern) _usingPattern = true;
			return result;
		}
	}
	public List<TrainInfo> SafeHolidaysTimetable
	{
		get
		{
			if (Patterns == null) return ToTrainInfoList(HolidaysTimetable?.ToList());
			List<TrainInfo> result = [];
			var data = HolidaysTimetable.ToList() ?? null;
			if (data == null) return [];
			for (var i = 0; i < data.Count; i++)
			{
				if (!data[i].UseV2)
				{
					result.Add(data[i]);
					continue;
				}
				if (Patterns.TryGetValue(data[i].PatternName, out var ttime))
				{
					result.Add(ToTrainInfo(data[i], ttime));
				}
				else
				{
					return [];
				}
			}
			if (!UsingPattern) _usingPattern = true;
			return result;
		}
	}

	private List<TrainInfo> ToTrainInfoList(List<ExtTrainInfo> exInfos)
	{
		List<TrainInfo> r = [];
		foreach (var exInfo in exInfos)
		{
			r.Add(exInfo);
		}
		return r;
	}

	private TrainInfo ToTrainInfo(ExtTrainInfo exInfo, TrainInfoWithoutTime patternData)
	{
		TrainInfo result = new()
		{
			Direction = patternData.Direction,
			IsTop = patternData.IsTop,
			NextStation = patternData.NextStation,
			Time = exInfo.Time,
			TrainType = patternData.TrainType,
			Upside = patternData.Upside
		};
		if (TypeColors != null && TypeColors.TryGetValue(result.TrainType, out var color))
		{
			result.ImageColor = color;
		}
		else
		{
			result.ImageColor = "Transparent";
		}
		return result;
	}
}
