#pragma warning disable IDE0079
#pragma warning disable IDE1006
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ttvmin;

public partial class MainWindow : Window
{
	[DllImport("dwmapi.dll")]
	private static extern void DwmSetWindowAttribute(nint hwnd, int pv, ref int dw, int cb);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(nint hwnd, int id);

	[DllImport("user32.dll")]
	private static extern void SetWindowLong(nint hwnd, int id, int dwnewlong);

	[DllImport("user32.dll")]
	private static extern nint GetSystemMenu(nint hwnd, bool bRevert);

	[DllImport("user32.dll")]
	private static extern int TrackPopupMenu(nint hmenu, int uFlags, int x, int y, int nReserved, nint hwnd, ref RECT prcRect);

	[DllImport("user32.dll")]
	private static extern void SendMessage(nint hwnd, int msg, nint wp, nint lp);

	[DllImport("user32.dll")]
	private static extern void GetWindowRect(nint hwnd, out RECT rc);

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	public static MainWindow Current => _current;
	private static MainWindow _current;

	public static bool IsLight => _isLight;

	private static bool _isLight = true;

	private bool IsChanging = false;

	private bool _isFullScreen = false, _switching = false;
	private WindowState _previousWindowState, _prevState;
	private WindowStyle _previousWindowStyle;

	private nint hWnd => IsLoaded ? new WindowInteropHelper(this).Handle : 0;

	public MainWindow()
	{
		_current = this;
		InitializeComponent();
#if DEBUG
		Title += " - Debug";
#endif
		if (Debugger.IsAttached) Title += "(Debugger Attached)";
		KeyDown += (_, e) =>
		{
			if (e.Key == Key.F11) ToggleFullscreenMode();
		};
		Loaded += (_, _) =>
		{
			int r = 4;
			DwmSetWindowAttribute(hWnd, 38, ref r, sizeof(int));
			SetWindowLong(hWnd, -16, GetWindowLong(hWnd, -16) & ~0x80000);
			CloseButton.Click += (_, _) => Close();
			MaxRsButton.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
			MinmzButton.Click += (_, _) =>
			{
				if (WindowState != WindowState.Minimized)
				{
					_prevState = WindowState;
				}
				WindowState = WindowState == WindowState.Minimized ? _prevState : WindowState.Minimized;
			};
			SizeChanged += (_, _) =>
			{
				if (WindowState == WindowState.Maximized)
				{
					grid.Margin = new(7);
					MaxRsButton.Content = "\uE923";
					MinmzButton.Content = "\uE921";
				}
				else if (WindowState == WindowState.Minimized)
				{
					MaxRsButton.Content = "\uE922";
					MinmzButton.Content = "\uE923";
				}
				else
				{
					grid.Margin = new(0);
					MaxRsButton.Content = "\uE922";
					MinmzButton.Content = "\uE921";
				}
			};
			HwndSource.FromHwnd(hWnd).AddHook(WndProc);
			SwitchTheme_();
		};
	}

	private nint WndProc(nint hwnd, int msg, nint wp, nint lp, ref bool handled)
	{
		if (msg == 0x112 && wp == 0xF120 && _isFullScreen)
		{
			handled = true;
		}
		else if (msg == 0x112 && wp == 0xF100 && !_isFullScreen)
		{
			RECT rc = new();
			GetWindowRect(hWnd, out var wndRect);
			var r = TrackPopupMenu(GetSystemMenu(hWnd, false), 0x1500, wndRect.left, wndRect.top + 32, 0, hWnd, ref rc);
			SendMessage(hWnd, 0x112, r, 0);
			handled = true;
		}
		else if (msg == 0x1a)
		{
			SwitchTheme_();
		}
		return 0;
	}

	private void SwitchTheme_()
	{
		bool r0 = false;
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			if ((int)key.GetValue("AppsUseLightTheme", 0) == 1)
			{
				r0 = true;
			}
		}
		catch { }
		SwitchTheme(r0);
	}

	private Style GetStyle(string key) => (Style)App.Current.GetResByStr(key);

	private void SwitchTheme(bool isl)
	{
		if (IsChanging || isl == IsLight) return;
		IsChanging = true;
		var i = isl ? 0 : 1;
		DwmSetWindowAttribute(hWnd, 20, ref i, sizeof(int));
		Resources.Clear();
		ResourceDictionary rd = [];
		string y = isl ? "Light" : "Dark";
		Style[] basedStyles = [GetStyle($"{y}DefSys"), GetStyle($"{y}Text")];
		foreach (var style in basedStyles)
		{
			rd.Add(style.TargetType, new Style(style.TargetType, style));
		}
		CloseButton.Style = GetStyle($"{y}SysClose");
		Resources = rd;
		_isLight = isl;
		Views.TrainTimetableView.Current.SwitchTheme(isl);
		IsChanging = false;
	}

	private void ToggleFullscreenMode()
	{
		if (_switching) return;
		_switching = true;
		if (!_isFullScreen)
		{
			_previousWindowState = WindowState;
			_previousWindowStyle = WindowStyle;
			if (_previousWindowState == WindowState.Maximized)
			{
				WindowState = WindowState.Normal;
			}
			wc.CaptionHeight = 0;
			gr0.Visibility = Visibility.Collapsed;
			row0.Height = new(0, GridUnitType.Pixel);
			ResizeMode = ResizeMode.NoResize;
			WindowStyle = WindowStyle.None;
			WindowState = WindowState.Maximized;
			_isFullScreen = true;
		}
		else
		{
			if (_previousWindowState == WindowState.Maximized)
			{
				WindowState = WindowState.Normal;
			}
			ResizeMode = ResizeMode.CanResize;
			row0.Height = new(32, GridUnitType.Pixel);
			gr0.Visibility = Visibility.Visible;
			wc.CaptionHeight = 32;
			WindowStyle = _previousWindowStyle;
			WindowState = _previousWindowState;
			_isFullScreen = false;
		}
		_switching = false;
	}
}
