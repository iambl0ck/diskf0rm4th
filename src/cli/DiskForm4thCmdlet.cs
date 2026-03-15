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
            // TODO: Call C++ Core Engine via P/Invoke
            // Example: CoreEngineWrapper.FormatDisk(DriveLetter, Quick.IsPresent);
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

        protected override void ProcessRecord()
        {
            diskform4th.UI.LocalizationManager.Instance.SetLanguage(Language);
            var loc = diskform4th.UI.LocalizationManager.Instance;

            WriteObject($"{loc["status_burning"]} {IsoPath} -> {TargetDrive}...");

            // P/Invoke wrapper example
            // int result = CoreEngineWrapper.WriteIsoAsync(TargetDrive, IsoPath, SmartMonitor.IsPresent);
            // if (result == 0) WriteObject(loc["status_done"]);
            // else WriteError(new ErrorRecord(new Exception("Write failed"), "WriteError", ErrorCategory.WriteError, TargetDrive));

            WriteObject($"{loc["status_done"]} (mock).");
        }
    }

    // P/Invoke definitions for the Shared C++ Core Library
    internal static class CoreEngineWrapper
    {
        private const string DllName = "diskform4th_core.dll";

        // Example C++ export: extern "C" __declspec(dllexport) int write_iso(const char* target, const char* iso_path, bool smart_monitor);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int WriteIsoAsync(string target, string isoPath, bool smartMonitor);

        // Example C++ export: extern "C" __declspec(dllexport) int format_disk(const char* target, bool quick);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int FormatDisk(string target, bool quick);
    }
}