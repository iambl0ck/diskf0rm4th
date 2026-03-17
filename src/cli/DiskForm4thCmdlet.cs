using System;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace diskform4th.CLI
{
    [Cmdlet(VerbsCommon.Format, "Disk4th")]
    public class FormatDiskCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The target drive letter (e.g., E:)")]
        public string DriveLetter { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Perform a quick format")]
        public SwitchParameter Quick { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Language for CLI output (en/tr)")]
        public string Language { get; set; } = "en";

        protected override void ProcessRecord()
        {
            diskform4th.UI.LocalizationManager.Instance.SetLanguage(Language);
            var loc = diskform4th.UI.LocalizationManager.Instance;

            WriteObject($"{loc["status_formatting"]} {DriveLetter}...");

            CoreEngineWrapper.ProgressCallback callback = (percentage, speed, remaining, temp, healthy) =>
            {
                if (percentage % 25 == 0)
                {
                    WriteObject($"Progress: {percentage}%");
                }
            };

            CoreEngineWrapper.FormatDisk(DriveLetter, Quick.IsPresent, callback);

            WriteObject($"{loc["status_done"]}.");
        }
    }

    [Cmdlet(VerbsCommon.Write, "Iso4th")]
    public class WriteIsoCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The target drive letter (e.g., E:)")]
        public string TargetDrive { get; set; }

        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Path to the ISO file")]
        public string IsoPath { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable S.M.A.R.T monitoring during write")]
        public SwitchParameter SmartMonitor { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Language for CLI output (en/tr)")]
        public string Language { get; set; } = "en";

        [Parameter(Mandatory = false, HelpMessage = "Pre-load ISO to RAM before writing")]
        public SwitchParameter PreloadRam { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Perform a DoD 3-Pass wipe before writing")]
        public SwitchParameter SecureErase { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Encrypt remaining free space (BitLocker/LUKS)")]
        public SwitchParameter EncryptSpace { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Create persistent partition (casper-rw)")]
        public SwitchParameter Persistence { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable Multi-Boot Mode (Ventoy Alternative)")]
        public SwitchParameter MultiBoot { get; set; }

        protected override void ProcessRecord()
        {
            diskform4th.UI.LocalizationManager.Instance.SetLanguage(Language);
            var loc = diskform4th.UI.LocalizationManager.Instance;

            WriteObject($"{loc["status_burning"]} {IsoPath} -> {TargetDrive}...");

            CoreEngineWrapper.ProgressCallback callback = (percentage, speed, remaining, temp, healthy) =>
            {
                if (percentage % 25 == 0)
                {
                    WriteObject($"Progress: {percentage}% - {speed:F1} MB/s (Temp: {temp}C)");
                }
            };

            int result = CoreEngineWrapper.WriteIsoAsync(TargetDrive, IsoPath, false, SmartMonitor.IsPresent, false, PreloadRam.IsPresent, SecureErase.IsPresent, EncryptSpace.IsPresent, Persistence.IsPresent, MultiBoot.IsPresent, callback);

            if (result == 0) WriteObject(loc["status_done"]);
            else WriteError(new ErrorRecord(new Exception("Write failed"), "WriteError", ErrorCategory.WriteError, TargetDrive));
        }
    }

    [Cmdlet(VerbsCommon.Get, "Pxe4th")]
    public class PxeCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Path to the ISO file to serve")]
        public string IsoPath { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject($"Starting PXE Server for {IsoPath}...");
            CoreEngineWrapper.ProgressCallback callback = (percentage, speed, remaining, temp, healthy) => { };
            CoreEngineWrapper.StartPxeServer(IsoPath, callback);
            WriteObject("PXE Server stopped.");
        }
    }

    // P/Invoke definitions for the Shared C++ Core Library
    internal static class CoreEngineWrapper
    {
        private const string DllName = "diskform4th_core.dll";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProgressCallback(int percentage, double speedMbPs, int remainingSeconds, int temperature, bool healthy);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WriteIsoAsync(string target, string isoPath, bool isIsoMode, bool smartMonitor, bool verifyBlocks, bool preLoadRam, bool secureErase, bool encryptSpace, bool persistence, bool multiBoot, ProgressCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int FormatDisk(string target, bool quick, ProgressCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int StartPxeServer(string isoPath, ProgressCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int InjectWin11Bypass(string target, ProgressCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int BackupDriveAsync(string sourceDrive, string targetImagePath, ProgressCallback callback);
    }
}