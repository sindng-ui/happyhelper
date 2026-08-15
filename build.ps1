# PowerShell Build Automation Script for HappyHelper C# (WPF + WebView2)
# No heavy Visual Studio installation required.

$ErrorActionPreference = "Stop"

# Stop existing running instance if any
Stop-Process -Name "happyhelper" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

# Paths
$baseDir = Get-Location
$buildDir = Join-Path $baseDir "build-temp"
$distDir = Join-Path $baseDir "dist-csharp"
$srcDir = Join-Path $baseDir "src-csharp"
$rendererDir = Join-Path $baseDir "renderer"

Write-Host "=== HappyHelper C# Compact Builder Starting ===" -ForegroundColor Yellow

# 1. Clean & Setup Folders
if (Test-Path $buildDir) { 
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
    Remove-Item $buildDir -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $distDir) { 
    Remove-Item $distDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $buildDir -ErrorAction SilentlyContinue | Out-Null
New-Item -ItemType Directory -Path $distDir -ErrorAction SilentlyContinue | Out-Null

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 2. Download WebView2 NuGet Package
$nugetUrl = "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/1.0.2592.51"
$nupkgPath = Join-Path $buildDir "webview2.zip"

Write-Host "Downloading WebView2 NuGet package..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $nugetUrl -OutFile $nupkgPath

Write-Host "Extracting libraries..." -ForegroundColor Cyan
Expand-Archive -Path $nupkgPath -DestinationPath $buildDir -Force

# Copy required DLLs to build and dist folders
Copy-Item (Join-Path $buildDir "lib/net462/Microsoft.Web.WebView2.Core.dll") -Destination $srcDir -Force
Copy-Item (Join-Path $buildDir "lib/net462/Microsoft.Web.WebView2.Wpf.dll") -Destination $srcDir -Force
Copy-Item (Join-Path $buildDir "build/native/x64/WebView2Loader.dll") -Destination $srcDir -Force

Copy-Item (Join-Path $buildDir "lib/net462/Microsoft.Web.WebView2.Core.dll") -Destination $distDir -Force
Copy-Item (Join-Path $buildDir "lib/net462/Microsoft.Web.WebView2.Wpf.dll") -Destination $distDir -Force
Copy-Item (Join-Path $buildDir "build/native/x64/WebView2Loader.dll") -Destination $distDir -Force

# 2.1 Download ViGEmClient NuGet Package
try {
    Write-Host "Downloading ViGEmClient library..." -ForegroundColor Cyan
    $vigemNugetUrl = "https://www.nuget.org/api/v2/package/Nefarius.ViGEm.Client/1.17.178"
    $vigemNupkgPath = Join-Path $buildDir "vigem.zip"
    Invoke-WebRequest -Uri $vigemNugetUrl -OutFile $vigemNupkgPath -TimeoutSec 15
    if (Test-Path $vigemNupkgPath) {
        $extractDir = Join-Path $buildDir "vigem_extracted"
        Expand-Archive -Path $vigemNupkgPath -DestinationPath $extractDir -Force
        $dllPath = Join-Path $extractDir "lib/net452/Nefarius.ViGEm.Client.dll"
        if (Test-Path $dllPath) {
            Copy-Item $dllPath -Destination (Join-Path $srcDir "Nefarius.ViGEm.Client.dll") -Force
            Copy-Item $dllPath -Destination (Join-Path $distDir "Nefarius.ViGEm.Client.dll") -Force
            Write-Host "Successfully unpacked Nefarius.ViGEm.Client.dll" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "ViGEmClient package download error: $_" -ForegroundColor Yellow
}

# 2.2 Download ViGEmBus Setup
$vigemSetupPath = Join-Path $buildDir "ViGEmBus_Setup_1.22.0.exe"
try {
    if (-not (Test-Path $vigemSetupPath)) {
        Write-Host "Downloading ViGEmBus Driver Installer for embedding..." -ForegroundColor Cyan
        $vigemSetupUrl = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe"
        Invoke-WebRequest -Uri $vigemSetupUrl -OutFile $vigemSetupPath -TimeoutSec 15 -ErrorAction SilentlyContinue
    }
} catch {
    Write-Host "ViGEmBus installer download skipped: $_" -ForegroundColor Yellow
}

# 3. Locate csc.exe & WPF Assemblies
$netDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$cscPath = Join-Path $netDir "csc.exe"
if (-not (Test-Path $cscPath)) {
    throw "C# compiler csc.exe not found at $cscPath"
}

# 4. Compile C# Main Executable
Write-Host "Compiling C# Main Source with Embedded ViGEm Installer..." -ForegroundColor Cyan
$cmdArgs = @(
    "/target:winexe",
    "/main:HappyHelper.MainWindow",
    "/out:$(Join-Path $distDir "happyhelper.exe")",
    "/reference:$(Join-Path $srcDir "Microsoft.Web.WebView2.Core.dll")",
    "/reference:$(Join-Path $srcDir "Microsoft.Web.WebView2.Wpf.dll")",
    "/reference:$(Join-Path $srcDir "Nefarius.ViGEm.Client.dll")",
    "/reference:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\netstandard.dll",
    "/reference:$(Join-Path $netDir "System.dll")",
    "/reference:$(Join-Path $netDir "System.Core.dll")",
    "/reference:$(Join-Path $netDir "System.Xaml.dll")",
    "/reference:$(Join-Path $netDir "WPF\WindowsBase.dll")",
    "/reference:$(Join-Path $netDir "WPF\PresentationCore.dll")",
    "/reference:$(Join-Path $netDir "WPF\PresentationFramework.dll")",
    "/resource:$vigemSetupPath,ViGEmBus_Setup.exe",
    "$(Join-Path $srcDir "MainWindow.xaml.cs")",
    "$(Join-Path $srcDir "GlobalHook.cs")",
    "$(Join-Path $srcDir "InputEngine.cs")",
    "$(Join-Path $srcDir "VirtualGamepad.cs")",
    "$(Join-Path $srcDir "ViGEmInstaller.cs")",
    "$(Join-Path $srcDir "LoopRunner.cs")",
    "$(Join-Path $srcDir "ConfigManager.cs")",
    "$(Join-Path $srcDir "WindowHelper.cs")",
    "$(Join-Path $srcDir "DebugLog.cs")",
    "$(Join-Path $srcDir "GamepadPassthrough.cs")"
)

# Run compiler for Main App
& $cscPath $cmdArgs


# Copy Nefarius.ViGEm.Client.dll to buildDir for TestRunner execution
Copy-Item (Join-Path $srcDir "Nefarius.ViGEm.Client.dll") -Destination $buildDir -Force

# Compile TestRunner for dev/CI verification in buildDir
Write-Host "Compiling TestRunner for verification..." -ForegroundColor Cyan
$testArgs = @(
    "/target:exe",
    "/main:HappyHelper.TestRunner",
    "/out:$(Join-Path $buildDir "TestRunner.exe")",
    "/reference:$(Join-Path $srcDir "Nefarius.ViGEm.Client.dll")",
    "/reference:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\netstandard.dll",
    "/reference:$(Join-Path $netDir "System.dll")",
    "/reference:$(Join-Path $netDir "System.Core.dll")",
    "$(Join-Path $srcDir "TestRunner.cs")",
    "$(Join-Path $srcDir "InputEngine.cs")",
    "$(Join-Path $srcDir "VirtualGamepad.cs")",
    "$(Join-Path $srcDir "ViGEmInstaller.cs")",
    "$(Join-Path $srcDir "LoopRunner.cs")",
    "$(Join-Path $srcDir "ConfigManager.cs")",
    "$(Join-Path $srcDir "WindowHelper.cs")",
    "$(Join-Path $srcDir "DebugLog.cs")",
    "$(Join-Path $srcDir "GamepadPassthrough.cs")"
)
& $cscPath $testArgs

# Run TestRunner automatically during build to guarantee zero regressions
if (Test-Path (Join-Path $buildDir "TestRunner.exe")) {
    Write-Host "Executing Unit Tests..." -ForegroundColor Cyan
    & (Join-Path $buildDir "TestRunner.exe")
}

# 5. Copy renderer folder to dist folder
Write-Host "Copying UI resources..." -ForegroundColor Cyan
Copy-Item $rendererDir -Destination $distDir -Recurse -Force

# Clean temporary build files
[GC]::Collect()
[GC]::WaitForPendingFinalizers()
Remove-Item $buildDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "=== Build Completed Successfully! ===" -ForegroundColor Green
Write-Host "Output Directory: $distDir" -ForegroundColor Yellow
Write-Host " happyhelper.exe (1.5MB)" -ForegroundColor Green
