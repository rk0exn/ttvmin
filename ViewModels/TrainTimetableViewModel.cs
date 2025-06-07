using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using ttvmin.Models;

namespace ttvmin.ViewModels;

public sealed class TrainTimetableViewModel : INotifyPropertyChanged, IDisposable
{
	public static TrainTimetableViewModel Current => _current;
	private static TrainTimetableViewModel _current;

	public event PropertyChangedEventHandler PropertyChanged;

	private TimetableData _data;
	private Timer _timer;
	private DirectionFilterMode _filterMode;
	private DayTypeMode _dayTypeMode;
	private bool m_bDisposed;
	private string _jsonVersion = "1";
	private const bool UseV2Sample = true;

	public bool IsShowingAll { get; private set; } = false;

	public ObservableCollection<TrainInfo> FilteredTrains { get; } = [];
	public List<DirectionFilterMode> DirectionModes { get; } = [.. Enum.GetValues(typeof(DirectionFilterMode)).Cast<DirectionFilterMode>()];
	public List<DayTypeMode> DayTypeModes { get; } = [.. Enum.GetValues(typeof(DayTypeMode)).Cast<DayTypeMode>()];

	public DirectionFilterMode FilterMode
	{
		get => _filterMode;
		set
		{
			if (_filterMode == value) return;
			_filterMode = value;
			OnPropertyChanged(nameof(FilterMode));
			Refresh();
		}
	}

	public DayTypeMode DayType
	{
		get => _dayTypeMode;
		set
		{
			if (_dayTypeMode == value) return;
			_dayTypeMode = value;
			OnPropertyChanged(nameof(DayType));
			Refresh();
		}
	}

	public string StationName => _data?.StationName;
	public string UpdateTime => _data?.UpdateTime;
	public string JsonVersion => _jsonVersion;
	public string Comment => string.IsNullOrWhiteSpace(_data?.Comment) ? "" : $"コメント: {_data?.Comment}";

	public TrainTimetableViewModel()
	{
		_current = this;
		if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
		{
			LoadSampleData();
			return;
		}
		FilterMode = DirectionFilterMode.All;
		DayType = DayTypeMode.Weekday;
		if (!LoadData())
		{
			LoadSampleData();
		}
		_timer = new(_ => Refresh(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(13300));
	}

	~TrainTimetableViewModel()
	{
		_timer.Dispose();
	}

	private void LoadSampleData()
	{
		if (m_bDisposed) throw new ObjectDisposedException(null);
		_data = UseV2Sample ? new()
		{
			StationName = "デサインモード - 新宿駅",
			UpdateTime = "2025/05/23 00:48",
			TypeColors = new()
			{
				{ "普通", "#0080FF" },
				{ "快速", "#00FF80" },
				{ "特急", "#8000FF" }
			},
			Patterns = new()
			{
				{ "TkyNm", new() { Direction = "東京", NextStation = "千駄ヶ谷", Upside = true, TrainType = "普通" } }
			},
			WeekDaysTimetable =
			[
				new() { Time = "06:00", PatternName = "TkyNm" },
				new() { Time = "08:00", Direction = "甲府", TrainType = "特急", NextStation = "立川", Upside = false },
				new() { Time = "10:00", Direction = "東京", TrainType = "快速", NextStation = "御茶ノ水", Upside = true },
				new() { Time = "12:00", Direction = "高尾", TrainType = "普通", NextStation = "大久保", Upside = false },
				new() { Time = "14:00", Direction = "東京", TrainType = "特急", NextStation = "東京", Upside = true },
				new() { Time = "16:00", Direction = "松本", TrainType = "特急", NextStation = "八王子", Upside = false },
				new() { Time = "18:00", Direction = "東京", TrainType = "普通", NextStation = "千駄ヶ谷", Upside = true },
				new() { Time = "20:00", Direction = "高尾", TrainType = "快速", NextStation = "代々木", Upside = false },
				new() { Time = "22:00", Direction = "南小谷", TrainType = "特急" , NextStation = "八王子", Upside = false }
			],
			HolidaysTimetable = [],
			Comment = "これはデザインモードまたはファイル未選択時用のサンプルデータです。"
		} : new()
		{
			StationName = "デザインモード - 新宿駅",
			UpdateTime = "2025/05/23 00:49",
			TypeColors = new()
			{
				{ "普通", "#0080FF" },
				{ "快速", "#00FF80" },
				{ "特急", "#8000FF" }
			},
			WeekDaysTimetable =
			[
				new() { Time = "06:00", Direction = "東京", TrainType = "普通", NextStation = "千駄ヶ谷", Upside = true },
				new() { Time = "08:00", Direction = "甲府", TrainType = "特急", NextStation = "立川", Upside = false },
				new() { Time = "10:00", Direction = "東京", TrainType = "快速", NextStation = "御茶ノ水", Upside = true },
				new() { Time = "12:00", Direction = "高尾", TrainType = "普通", NextStation = "大久保", Upside = false },
				new() { Time = "14:00", Direction = "東京", TrainType = "特急", NextStation = "東京", Upside = true },
				new() { Time = "16:00", Direction = "松本", TrainType = "特急", NextStation = "八王子", Upside = false },
				new() { Time = "18:00", Direction = "東京", TrainType = "普通", NextStation = "千駄ヶ谷", Upside = true },
				new() { Time = "20:00", Direction = "高尾", TrainType = "快速", NextStation = "代々木", Upside = false },
				new() { Time = "22:00", Direction = "南小谷", TrainType = "特急" , NextStation = "八王子", Upside = false }
			],
			HolidaysTimetable = [],
			Comment = "これはデザインモードまたはファイル未選択時用のサンプルデータです。"
		};
		foreach (var t in _data.SafeWeekdaysTimetable.Concat(_data.SafeHolidaysTimetable)) t.ImageColor = _data.TypeColors[t.TrainType];
		RefreshOnce();
	}

	private bool LoadData()
	{
		if (m_bDisposed) throw new ObjectDisposedException(null);

		try
		{
			string json;
			OpenFileDialog ofd = new()
			{
				Filter = "JSONファイル (*.json)|*.json",
				CheckFileExists = true,
				AddExtension = true,
				ReadOnlyChecked = true,
				ShowReadOnly = false,
				ValidateNames = true,
				Title = "駅情報の含まれるJSONファイルを開いてください..."
			};
			if (ofd.ShowDialog() == true)
			{
				json = File.ReadAllText(ofd.FileName);
			}
			else
			{
				MessageBox.Show("ファイルが選択されなかったため、サンプルデータを読み込みます。");
				return false;
			}
			_data = JsonSerializer.Deserialize<TimetableData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			var all = _data.SafeWeekdaysTimetable.Concat(_data.SafeHolidaysTimetable);
			if (_data.TypeColors == null || !all.All(t => _data.TypeColors.ContainsKey(t.TrainType)))
			{
				MessageBox.Show("未定義の traіntype が含まれています。代わりにサンプルデータを読み込みます。");
				return false;
			}
			foreach (var t in all) t.ImageColor = _data.TypeColors[t.TrainType];
			RefreshOnce();
			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"構成読み込みエラー: {ex.Message}\n代わりにサンプルデータを読み込みます。");
			return false;
		}
	}

	public static void SwitchRefresh() => Current.RefreshEx();

	private void RefreshEx()
	{
		if (m_bDisposed) throw new ObjectDisposedException(null);

		IsShowingAll = !IsShowingAll;
		Refresh();
	}

	private void RefreshOnce()
	{
		if (_data.UsingPattern) _jsonVersion = "2";
		Refresh();
	}

	private void Refresh()
	{
		if (m_bDisposed) throw new ObjectDisposedException(null);

		if (_data == null) return;
		var now = DateTime.Now.TimeOfDay;
		var source = DayType == DayTypeMode.Weekday ? _data.SafeWeekdaysTimetable : _data.SafeHolidaysTimetable;
		List<TrainInfo> list = IsShowingAll ? [ShowingAllTip, DefaultTip] : [DefaultTip];
		List<TrainInfo> l2 = IsShowingAll ? [.. source.Where(t => FilterMode == DirectionFilterMode.All || t.Upside == (FilterMode == DirectionFilterMode.Upside)).OrderBy(t => TimeSpan.Parse(t.Time))] : [.. source.Where(t => TimeSpan.TryParse(t.Time, out var tm) && tm >= now).Where(t => FilterMode == DirectionFilterMode.All || t.Upside == (FilterMode == DirectionFilterMode.Upside)).OrderBy(t => TimeSpan.Parse(t.Time))];
		var (ti1, ti2, ti3) = NextDayWarnTip;
		if (!l2.Any())
		{
			list.Add(ti1);
			list.Add(ti2);
			list.Add(ti3);
			l2 = [.. source.Where(t => TimeSpan.TryParse(t.Time, out _)).Where(t => FilterMode == DirectionFilterMode.All || t.Upside == (FilterMode == DirectionFilterMode.Upside)).OrderBy(t => TimeSpan.Parse(t.Time))];
		}
		if (l2.Count == 0)
		{
			list.Remove(ti1);
			list.Remove(ti2);
			list.Remove(ti3);
			var (te1, te2) = EmptyTip;
			l2.Add(te1);
			l2.Add(te2);
		}
		foreach (var l in l2) list.Add(l);
		Application.Current.Dispatcher.BeginInvoke(() =>
		{
			FilteredTrains.Clear();
			foreach (var item in list) FilteredTrains.Add(item);
		});
	}

	private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Clear() => Dispose();

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer?.Dispose();
			_timer = null;
			m_bDisposed = true;
		}
	}

	private TrainInfo ShowingAllTip => new() { ImageColor = "Gray", Time = "全てを表示しています" };
	private (TrainInfo ti1, TrainInfo ti2, TrainInfo ti3) NextDayWarnTip => (new(), new() { Direction = "次の日の", TrainType = "データを表示", NextStation = "しています" }, new());

	private (TrainInfo te1, TrainInfo te2) EmptyTip => (new(), new() { TrainType = "データがありません" });

	private TrainInfo DefaultTip => new()
	{
		ImageColor = "Gray",
		NextStation = "次の駅",
		Time = "時刻",
		TrainType = "種別",
		Direction = "方向",
		IsTop = true
	};
}
