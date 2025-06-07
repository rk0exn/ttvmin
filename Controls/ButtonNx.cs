using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ttvmin.Helpers;
namespace ttvmin.Controls;
public sealed class ButtonNx : Button
{
	[DllImport("user32.dll")]
	private static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

	[StructLayout(LayoutKind.Sequential)]
	private struct TRACKMOUSEEVENT
	{
		public uint cbSize;
		public uint dwFlags;
		public IntPtr hwndTrack;
		public uint dwHoverTime;
	}

	private HwndSource _parentHwndSource;
	private static ButtonNx ReallyFocusedButton = null;

	static ButtonNx()
	{
		DefaultStyleKeyProperty.OverrideMetadata(typeof(ButtonNx), new FrameworkPropertyMetadata(typeof(ButtonNx)));
	}
	~ButtonNx()
	{
		if (ReallyFocusedButton == this) ReallyFocusedButton = null;
	}
	public ButtonNx()
	{
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}
	public static readonly DependencyProperty IsMouseReallyOverProperty = DependencyProperty.Register(nameof(IsMouseReallyOver), typeof(bool), typeof(ButtonNx), new(false));
	public bool IsMouseReallyOver
	{
		get => (bool)GetValue(IsMouseReallyOverProperty);
		private set => SetValue(IsMouseReallyOverProperty, value);
	}
	private void UpdateIsMouseReallyOver() => IsMouseReallyOver = IsMouseOver || IsNCMouseOver;
	public static readonly DependencyProperty IsReallyPressedProperty = DependencyProperty.Register(nameof(IsReallyPressed), typeof(bool), typeof(ButtonNx), new(false));
	public bool IsReallyPressed
	{
		get => (bool)GetValue(IsReallyPressedProperty);
		private set => SetValue(IsReallyPressedProperty, value);
	}
	private void UpdateIsReallyPressed() => IsReallyPressed = IsPressed || IsNCPressed;
	internal uint? HitTestCode { get; set; }
	private bool _isNCMouseOver;
	private bool IsNCMouseOver
	{
		get => _isNCMouseOver;
		set
		{
			if (_isNCMouseOver != value)
			{
				if ((!value || ReallyFocusedButton != null && ReallyFocusedButton != this) && (value || ReallyFocusedButton != this))
				{
					if (ReallyFocusedButton != null) ReallyFocusedButton.IsNCMouseOver = false;
				}
				ReallyFocusedButton = value ? this : null;
				_isNCMouseOver = value;
				OnIsNCMouseOverChanged();
			}
		}
	}
	private void OnIsNCMouseOverChanged()
	{
		UpdateIsMouseReallyOver();
		if (!IsNCMouseOver)
		{
			IsNCPressed = false;
		}
	}
	private bool _isNCPressed;
	private bool IsNCPressed
	{
		get => _isNCPressed;
		set
		{
			if (_isNCPressed != value)
			{
				_isNCPressed = value;
				OnIsNCPressedChanged();
			}
		}
	}
	private void OnIsNCPressedChanged() => UpdateIsReallyPressed();
	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.Property == IsMouseOverProperty)
		{
			UpdateIsMouseReallyOver();
		}
		else if (e.Property == IsPressedProperty)
		{
			UpdateIsReallyPressed();
		}
	}
	internal void DoClick()
	{
		IsNCMouseOver = false;
		OnClick();
	}
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (HitTestCode.HasValue)
		{
			_parentHwndSource = (HwndSource)PresentationSource.FromVisual(this);
			System.Diagnostics.Debug.Assert(_parentHwndSource != null);
			_parentHwndSource?.AddHook(ButtonNxFilterMessage);
		}
	}
	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_parentHwndSource != null)
		{
			StopTrackingMouseLeave();
			_parentHwndSource.RemoveHook(ButtonNxFilterMessage);
			_parentHwndSource = null;
		}
	}
	private void StartTrackingMouseLeave()
	{
		if (_parentHwndSource == null) return;

		var tme = new TRACKMOUSEEVENT
		{
			cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
			dwFlags = 0x12,
			hwndTrack = _parentHwndSource.Handle,
			dwHoverTime = 0
		};
		TrackMouseEvent(ref tme);
	}
	private void StopTrackingMouseLeave()
	{
		if (_parentHwndSource == null) return;

		var tme = new TRACKMOUSEEVENT
		{
			cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
			dwFlags = 0x80000010,
			hwndTrack = _parentHwndSource.Handle,
			dwHoverTime = 0
		};
		TrackMouseEvent(ref tme);
	}
	private nint ButtonNxFilterMessage(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		switch (msg)
		{
			case 0x84:
				if (IsMousePositionWithin(lParam))
				{
					IsNCMouseOver = true;
					StartTrackingMouseLeave();
					handled = true;
					return (nint)HitTestCode;
				}
				else
				{
					IsNCMouseOver = false;
				}
				break;
			case 0xa0:
			case 0x200:
				if (IsMousePositionWithin(lParam))
				{
					IsNCMouseOver = true;
					handled = true;
				}
				else
				{
					IsNCMouseOver = false;
				}
				break;
			case 0xa1:
				if (IsNCMouseOver)
				{
					IsNCPressed = true;
					handled = true;
				}
				break;
			case 0xa2:
				if (IsNCPressed)
				{
					IsNCPressed = false;
					if (IsMousePositionWithin(lParam)) OnClick();
					handled = true;
				}
				break;
			case 0x2a2:
				IsNCMouseOver = false;
				break;
		}
		return 0;
	}
	private bool IsMousePositionWithin(nint lParam) => new Rect(new(), RenderSize).Contains(PointFromScreen(new(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam))));
}
