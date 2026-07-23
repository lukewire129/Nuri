using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Nuri.SimplyShare.App;

internal static class NativeFilePicker
{
    private const int BufferCharacterCount = 65_536;
    private const uint Explorer = 0x00080000;
    private const uint AllowMultiSelect = 0x00000200;
    private const uint FileMustExist = 0x00001000;
    private const uint PathMustExist = 0x00000800;
    private const uint NoChangeDirectory = 0x00000008;

    public static string[] SelectFiles(IntPtr ownerWindowHandle, string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var fileBuffer = Marshal.AllocHGlobal(BufferCharacterCount * sizeof(char));
        var filter = Marshal.StringToHGlobalUni("All files (*.*)\0*.*\0");
        var dialogTitle = Marshal.StringToHGlobalUni(title);
        try
        {
            Marshal.WriteInt16(fileBuffer, 0);
            var dialog = new OpenFileName
            {
                StructureSize = Marshal.SizeOf<OpenFileName>(),
                OwnerWindowHandle = ownerWindowHandle,
                Filter = filter,
                FilterIndex = 1,
                File = fileBuffer,
                MaximumFileCharacters = BufferCharacterCount,
                Title = dialogTitle,
                Flags = Explorer | AllowMultiSelect | FileMustExist | PathMustExist | NoChangeDirectory
            };

            if (!GetOpenFileName(ref dialog))
            {
                var error = CommDlgExtendedError();
                if (error == 0)
                    return [];

                throw new Win32Exception(unchecked((int)error), $"The file picker failed with common-dialog error 0x{error:X4}.");
            }

            return ReadSelectedPaths(fileBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(dialogTitle);
            Marshal.FreeHGlobal(filter);
            Marshal.FreeHGlobal(fileBuffer);
        }
    }

    private static string[] ReadSelectedPaths(IntPtr buffer)
    {
        var segments = new List<string>();
        var characterOffset = 0;
        while (characterOffset < BufferCharacterCount)
        {
            var segment = Marshal.PtrToStringUni(IntPtr.Add(buffer, characterOffset * sizeof(char)));
            if (string.IsNullOrEmpty(segment))
                break;

            segments.Add(segment);
            characterOffset += segment.Length + 1;
        }

        if (segments.Count <= 1)
            return segments.ToArray();

        var directory = segments[0];
        var paths = new string[segments.Count - 1];
        for (var index = 1; index < segments.Count; index++)
            paths[index - 1] = Path.Combine(directory, segments[index]);
        return paths;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenFileName
    {
        public int StructureSize;
        public IntPtr OwnerWindowHandle;
        public IntPtr InstanceHandle;
        public IntPtr Filter;
        public IntPtr CustomFilter;
        public int MaximumCustomFilterCharacters;
        public int FilterIndex;
        public IntPtr File;
        public int MaximumFileCharacters;
        public IntPtr FileTitle;
        public int MaximumFileTitleCharacters;
        public IntPtr InitialDirectory;
        public IntPtr Title;
        public uint Flags;
        public short FileOffset;
        public short FileExtension;
        public IntPtr DefaultExtension;
        public IntPtr CustomData;
        public IntPtr Hook;
        public IntPtr TemplateName;
        public IntPtr Reserved;
        public int ReservedValue;
        public uint ExtendedFlags;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OpenFileName dialog);

    [DllImport("comdlg32.dll")]
    private static extern uint CommDlgExtendedError();
}
