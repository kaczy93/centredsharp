using System.Runtime.InteropServices;
using System.Text;

namespace CentrED.Renderer;

public class FixedAsciiString : IDisposable
{
    public IntPtr DataPtr { get; }

    public unsafe FixedAsciiString(string s)
    {
        int byteCount = Encoding.ASCII.GetByteCount(s);
        DataPtr = Marshal.AllocHGlobal(byteCount + 1);
        fixed (char* sPtr = s)
        {
            int end = Encoding.ASCII.GetBytes(sPtr, s.Length, (byte*)DataPtr, byteCount);
            ((byte*)DataPtr)[end] = 0;
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(DataPtr);
    }
}