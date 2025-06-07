using System.Windows;
using ttvmin.ViewModels;

namespace ttvmin.Views;

public sealed partial class TrainTimetableView : System.Windows.Controls.UserControl
{
	public static TrainTimetableView Current => _current;

	private static TrainTimetableView _current;

	public static bool IsLight => _isLight;

	private static bool _isLight = true;

	private bool IsChanging = false;

	public TrainTimetableView()
	{
		_current = this;
		InitializeComponent();
		btSwitch.Click += (_, _) => TrainTimetableViewModel.SwitchRefresh();
		btChange.Click += (_, _) =>
		{
			if (DataContext is TrainTimetableViewModel model)
			{
				model.Dispose();
				DataContext = new TrainTimetableViewModel();
			}
		};
	}

	public void SwitchTheme(bool isLight) => SwitchTheme_(isLight);

	private Style GetStyle(string key) => (Style)App.Current.GetResByStr(key);

	private void SwitchTheme_(bool isl)
	{
		if (IsChanging || isl == IsLight) return;
		IsChanging = true;
		Resources.Clear();
		ResourceDictionary rd = [];
		var t = isl ? "Light" : "Dark";
		Style[] basedStyles = [GetStyle($"{t}Button"), GetStyle($"{t}ComboBox"), GetStyle($"{t}ComboBoxItem"), GetStyle($"{t}ListBox"), GetStyle($"{t}Text")];
		foreach (var style in basedStyles)
		{
			rd.Add(style.TargetType, new Style(style.TargetType, style));
		}
		Resources = rd;
		_isLight = isl;
		IsChanging = false;
	}
}
