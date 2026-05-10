using System.Runtime.InteropServices;
using System.Security;

namespace JipperKeyViewer.KeyViewer
{
    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int vKey);
    }
}
