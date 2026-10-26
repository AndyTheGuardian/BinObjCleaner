using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BinObjCleaner.Services;

/// <summary>
/// Kapselt das Verschieben von Verzeichnissen in den Windows-Papierkorb.
/// </summary>
public static class RecycleBin
{
    private const int FoDelete = 0x0003;
    private const ushort FofSilent = 0x0004;
    private const ushort FofNoConfirmation = 0x0010;
    private const ushort FofAllowUndo = 0x0040;
    private const ushort FofNoConfirmMkDir = 0x0200;
    private const ushort FofNoErrorUi = 0x0400;

    /// <summary>
    /// Gibt an, ob auf dem aktuellen System ein Papierkorb verfügbar ist.
    /// </summary>
    public static bool IsSupported => OperatingSystem.IsWindows();

    /// <summary>
    /// Verschiebt ein Verzeichnis in den Papierkorb.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Auf Nicht-Windows-Systemen.</exception>
    /// <exception cref="IOException">Der Vorgang schlug fehl.</exception>
    [SupportedOSPlatform("windows")]
    public static void SendToRecycleBin(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // SHFileOperation erwartet eine doppelt nullterminierte Pfadliste.
        var from = Marshal.StringToHGlobalUni(path + "\0\0");

        try
        {
            var operation = new ShFileOpStruct
            {
                wFunc = FoDelete,
                pFrom = from,
                fFlags = FofSilent | FofNoConfirmation | FofAllowUndo | FofNoErrorUi | FofNoConfirmMkDir,
            };

            var result = SHFileOperation(ref operation);

            if (result != 0)
            {
                throw new IOException($"Das Verschieben in den Papierkorb ist fehlgeschlagen (Fehlercode {result}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(from);
        }
    }

    [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHFileOperation(ref ShFileOpStruct fileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileOpStruct
    {
        public IntPtr hwnd;
        public uint wFunc;
        public IntPtr pFrom;
        public IntPtr pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public IntPtr lpszProgressTitle;
    }
}
