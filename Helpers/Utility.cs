namespace ttvmin.Helpers;

public static class Utility
{
	public static int GET_X_LPARAM(nint lParam) => LOWORD((int)lParam);
	public static int GET_Y_LPARAM(nint lParam) => HIWORD((int)lParam);
	public static int HIWORD(int i) => (short)(i >> 16);
	public static int LOWORD(int i) => (short)(i & 0xFFFF);
}
