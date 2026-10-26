$ErrorActionPreference = "Stop"
$projRoot = $PSScriptRoot
if (-not $projRoot) { $projRoot = "c:\Users\georg\.gemini\config\projects\g-suite-contained" }
$srcFile  = Join-Path $projRoot "src\GSuiteContainer.cs"
$outDll   = Join-Path $projRoot "GSuiteContainer.dll"
$outExe   = Join-Path $projRoot "GSuiteContainer.exe"
$rtConfig = Join-Path $projRoot "GSuiteContainer.runtimeconfig.json"
$icoFile  = Join-Path $projRoot "assets\app-icon.ico"

# Stop running instance if open
Stop-Process -Name "GSuiteContainer" -Force -ErrorAction SilentlyContinue

# 1. Copy WebView2 Runtime Interop Libraries (1.0.2903.40, < 900 KB total) if not already present
$wvSource = "C:\Program Files\WindowsApps\SVGDesignSolutions.AppsLauncherSearchEmailVideoDoc_1.4.1.0_x64__fj0jsbcsefh74\GAppLauncher"
if (Test-Path $wvSource) {
    foreach ($f in @("Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.Wpf.dll", "WebView2Loader.dll")) {
        $targetPath = Join-Path $projRoot $f
        if (-not (Test-Path $targetPath)) {
            Copy-Item (Join-Path $wvSource $f) $targetPath -Force
        }
    }
}

# 2. Compile GSuiteContainer.dll targeting .NET 8.0 + WPF + WebView2 via Roslyn
$netCoreDir = "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.32"
$wpfDir     = "C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.32"

$refNames = @(
    "$netCoreDir\System.Private.CoreLib.dll",
    "$netCoreDir\System.Runtime.dll",
    "$netCoreDir\System.Collections.dll",
    "$netCoreDir\System.Linq.dll",
    "$netCoreDir\System.Text.Json.dll",
    "$netCoreDir\System.Text.RegularExpressions.dll",
    "$netCoreDir\System.ComponentModel.Primitives.dll",
    "$netCoreDir\System.ComponentModel.TypeConverter.dll",
    "$netCoreDir\System.ComponentModel.dll",
    "$netCoreDir\System.Private.Uri.dll",
    "$netCoreDir\System.Diagnostics.Process.dll",
    "$netCoreDir\System.Security.Cryptography.dll",
    "$netCoreDir\System.Memory.dll",
    "$netCoreDir\System.IO.FileSystem.dll",
    "$netCoreDir\System.ObjectModel.dll",
    "$netCoreDir\System.Threading.dll",
    "$netCoreDir\System.Threading.Tasks.dll",
    "$netCoreDir\Microsoft.Win32.Primitives.dll",
    "$netCoreDir\Microsoft.Win32.Registry.dll",
    "$netCoreDir\System.Drawing.Primitives.dll",
    "$netCoreDir\netstandard.dll",
    "$netCoreDir\mscorlib.dll",
    "$wpfDir\PresentationCore.dll",
    "$wpfDir\PresentationFramework.dll",
    "$wpfDir\WindowsBase.dll",
    "$wpfDir\System.Xaml.dll",
    "$wpfDir\System.Drawing.Common.dll",
    "$projRoot\Microsoft.Web.WebView2.Core.dll",
    "$projRoot\Microsoft.Web.WebView2.Wpf.dll"
)

$code = Get-Content -Raw -Encoding UTF8 $srcFile
$syntaxTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($code, $null, $srcFile, [System.Text.Encoding]::UTF8)
[Microsoft.CodeAnalysis.MetadataReference[]]$refs = foreach ($r in $refNames) {
    if (Test-Path $r) {
        [Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($r)
    } else {
        Write-Warning "Missing reference: $r"
    }
}

$opts = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new(
    [Microsoft.CodeAnalysis.OutputKind]::WindowsApplication
).WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release)

[Microsoft.CodeAnalysis.SyntaxTree[]]$trees = @($syntaxTree)
$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create(
    "GSuiteContainer",
    $trees,
    $refs,
    $opts
)

$win32ResStream = $null
$icoStream = $null
$fs = [System.IO.File]::Create($outDll)
try {
    if (Test-Path $icoFile) {
        $icoStream = [System.IO.File]::OpenRead($icoFile)
        $win32ResStream = $comp.CreateDefaultWin32Resources($true, $false, $null, $icoStream)
    }
    $emitResult = $comp.Emit($fs, $null, $null, $win32ResStream, $null, $null)
} finally {
    $fs.Close()
    if ($win32ResStream) { $win32ResStream.Dispose() }
    if ($icoStream) { $icoStream.Dispose() }
}
if (-not $emitResult.Success) {
    foreach ($diag in $emitResult.Diagnostics) {
        if ($diag.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error) {
            Write-Host $diag.ToString() -ForegroundColor Red
        }
    }
    throw "Compilation of GSuiteContainer.dll failed."
}

# 3. Write .NET 8.0 runtimeconfig.json
@"
{
  "runtimeOptions": {
    "tfm": "net8.0-windows",
    "rollForward": "LatestMinor",
    "framework": {
      "name": "Microsoft.WindowsDesktop.App",
      "version": "8.0.0"
    }
  }
}
"@ | Set-Content -Path $rtConfig -Encoding UTF8

# 4. Create Native .NET 8.0 Windows GUI AppHost (GSuiteContainer.exe)
$apphostTemplate = "C:\Program Files\WindowsApps\SVGDesignSolutions.AppsLauncherSearchEmailVideoDoc_1.4.1.0_x64__fj0jsbcsefh74\GAppLauncher\GAppLauncher.exe"
if (Test-Path $apphostTemplate) {
    $appBytes = [System.IO.File]::ReadAllBytes($apphostTemplate)
    $oldName  = [System.Text.Encoding]::UTF8.GetBytes("GAppLauncher.dll" + [char]0)
    $newName  = [System.Text.Encoding]::UTF8.GetBytes("GSuiteContainer.dll" + [char]0)

    $patched = $false
    for ($i = 0; $i -lt ($appBytes.Length - $oldName.Length); $i++) {
        $m = $true
        for ($j = 0; $j -lt $oldName.Length; $j++) {
            if ($appBytes[$i + $j] -ne $oldName[$j]) { $m = $false; break }
        }
        if ($m) {
            for ($k = 0; $k -lt 64; $k++) { $appBytes[$i + $k] = 0 }
            [Array]::Copy($newName, 0, $appBytes, $i, $newName.Length)
            $patched = $true
            break
        }
    }
    if ($patched) { [System.IO.File]::WriteAllBytes($outExe, $appBytes) }
}

# 5. Inject Official Google Workspace Icon (assets\app-icon.ico) into GSuiteContainer.exe Win32 PE Resources (#32512)
if ((Test-Path $outExe) -and (Test-Path $icoFile)) {
    if (-not ("Win32IconPatcher" -as [type])) {
        Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Runtime.InteropServices;

public static class Win32IconPatcher
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public static void ApplyIconToExe(string exePath, string icoPath)
    {
        byte[] ico = File.ReadAllBytes(icoPath);
        if (ico.Length < 22) return;

        ushort count = BitConverter.ToUInt16(ico, 4);
        IntPtr hUpdate = BeginUpdateResource(exePath, false);
        if (hUpdate == IntPtr.Zero) return;

        using var grpStream = new MemoryStream();
        using var bw = new BinaryWriter(grpStream);
        bw.Write((ushort)0);     // Reserved
        bw.Write((ushort)1);     // Type: 1 = Icon
        bw.Write(count);         // Image count

        ushort langEnUs = 1033;
        ushort langNeutral = 0;

        for (int i = 0; i < count; i++)
        {
            int entryOffset = 6 + (i * 16);
            byte width = ico[entryOffset + 0];
            byte height = ico[entryOffset + 1];
            byte colors = ico[entryOffset + 2];
            byte reserved = ico[entryOffset + 3];
            ushort planes = BitConverter.ToUInt16(ico, entryOffset + 4);
            ushort bpp = BitConverter.ToUInt16(ico, entryOffset + 6);
            uint bytesInRes = BitConverter.ToUInt32(ico, entryOffset + 8);
            uint imageOffset = BitConverter.ToUInt32(ico, entryOffset + 12);

            byte[] imgData = new byte[bytesInRes];
            Buffer.BlockCopy(ico, (int)imageOffset, imgData, 0, (int)bytesInRes);

            ushort iconId = (ushort)(i + 1);
            UpdateResource(hUpdate, (IntPtr)3, (IntPtr)iconId, langNeutral, imgData, bytesInRes);
            UpdateResource(hUpdate, (IntPtr)3, (IntPtr)iconId, langEnUs, imgData, bytesInRes);

            bw.Write(width);
            bw.Write(height);
            bw.Write(colors);
            bw.Write(reserved);
            bw.Write(planes);
            bw.Write(bpp);
            bw.Write(bytesInRes);
            bw.Write(iconId);
        }

        byte[] grpData = grpStream.ToArray();
        // #32512 = IDI_APPLICATION (Primary Win32 & .NET AppHost Icon Group)
        UpdateResource(hUpdate, (IntPtr)14, (IntPtr)32512, langNeutral, grpData, (uint)grpData.Length);
        UpdateResource(hUpdate, (IntPtr)14, (IntPtr)32512, langEnUs, grpData, (uint)grpData.Length);
        UpdateResource(hUpdate, (IntPtr)14, (IntPtr)1, langNeutral, grpData, (uint)grpData.Length);

        EndUpdateResource(hUpdate, false);
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }
}
"@
    }
    [Win32IconPatcher]::ApplyIconToExe($outExe, $icoFile)
}

Write-Host "Build Succeeded! Output files in $projRoot :" -ForegroundColor Green
Get-ChildItem $projRoot -File | Where-Object { $_.Name -like "GSuiteContainer*" -or $_.Name -like "*WebView2*" } | Select-Object Name, @{N='KB';E={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
