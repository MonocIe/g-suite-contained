$ErrorActionPreference = "Stop"
$projRoot = $PSScriptRoot
if (-not $projRoot) { $projRoot = "c:\Users\georg\.gemini\config\projects\g-suite-contained" }
$srcFile  = Join-Path $projRoot "src\GSuiteContainer.cs"
$outDll   = Join-Path $projRoot "GSuiteContainer.dll"
$outExe   = Join-Path $projRoot "GSuiteContainer.exe"
$rtConfig = Join-Path $projRoot "GSuiteContainer.runtimeconfig.json"

# 1. Copy WebView2 Runtime Interop Libraries (1.0.2903.40, < 900 KB total) if not already present
$wvSource = "C:\Program Files\WindowsApps\SVGDesignSolutions.AppsLauncherSearchEmailVideoDoc_1.4.1.0_x64__fj0jsbcsefh74\GAppLauncher"
if (Test-Path $wvSource) {
    foreach ($f in @("Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.Wpf.dll", "WebView2Loader.dll")) {
        Copy-Item (Join-Path $wvSource $f) (Join-Path $projRoot $f) -Force
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

$fs = [System.IO.File]::Create($outDll)
try {
    $emitResult = $comp.Emit($fs)
} finally {
    $fs.Close()
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

Write-Host "Build Succeeded! Output files in $projRoot :" -ForegroundColor Green
Get-ChildItem $projRoot -File | Where-Object { $_.Name -like "GSuiteContainer*" -or $_.Name -like "*WebView2*" } | Select-Object Name, @{N='KB';E={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
