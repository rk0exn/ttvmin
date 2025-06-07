using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;

namespace ttvmin;

public sealed partial class App : Application
{
	[DllImport("uxtheme.dll", EntryPoint = "#135")]
	private static extern void AllowDarkModeForApp(int iEnable);

	[DllImport("user32.dll")]
	private static extern void SetForegroundWindow(nint hwnd);

	public static new App Current => _current;
	private static App _current;

#if DEBUG
	private Mutex mx;
#endif

	protected override void OnStartup(StartupEventArgs e)
	{
#if DEBUG
		mx = new(true, "ttvmin", out var createdNew);
		if (!createdNew)
		{
			var currentId = Process.GetCurrentProcess().Id;
			foreach (var p in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
			{
				if (p.Id != currentId && (nint)p.MainWindowHandle != 0)
				{
					SetForegroundWindow(p.MainWindowHandle);
					Environment.Exit(0);
					return;
				}
			}
		}
#endif
		if (Debugger.IsAttached
#if DEBUG
			&& !(File.Exists(@"S:\source\3dview\bin\Release\3dview.exe") && GenerateHash().ToLower() == "5f8e0529e689770b69f4860e8a1eb9ab70013367ee73324ccfcac81ec94fed64c5f02d7f9850415678964cda2eea5c916dbd229976da5d485c5f912f16387786".ToLower())
#endif
			)
		{
			MessageBox.Show("リリース版はデバッグできません！", "エラー", MessageBoxButton.OK, MessageBoxImage.Hand);
			Environment.FailFast("DO NOT DEBUG PLEASE!!!");
			Process.GetCurrentProcess().Kill();
			return;
		}
		base.OnStartup(e);
	}

#if DEBUG
	protected override void OnExit(ExitEventArgs e)
	{
		if (mx != null)
		{
			mx.ReleaseMutex();
			mx.Dispose();
		}
		base.OnExit(e);
	}

	private string GenerateHash()
	{
		try
		{
			using var s5 = SHA512.Create();
			StringBuilder sb = new();
			foreach (var c in s5.ComputeHash(File.ReadAllBytes(@"S:\source\3dview\bin\Release\3dview.exe")))
			{
				sb.Append($"{c:X2}");
			}
			return sb.ToString();
		}
		catch { return ""; }
	}
#endif

	public App()
	{
		AllowDarkModeForApp(2);
		_current = this;
	}

	public object GetResByStr(string key) => FindResource(key);
}
