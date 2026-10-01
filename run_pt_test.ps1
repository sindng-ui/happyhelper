$netDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$cscPath = Join-Path $netDir "csc.exe"
$srcDir = Join-Path $PSScriptRoot "src-csharp"

$args = @(
    "/target:exe",
    "/out:pt_test.exe",
    "/reference:$(Join-Path $srcDir "Nefarius.ViGEm.Client.dll")",
    "/reference:$netDir\System.dll",
    "/reference:$netDir\System.Core.dll",
    (Join-Path $srcDir "PassthroughTest.cs"),
    (Join-Path $srcDir "GamepadPassthrough.cs"),
    (Join-Path $srcDir "VirtualGamepad.cs"),
    (Join-Path $srcDir "DebugLog.cs")
)

& $cscPath $args
if (Test-Path .\pt_test.exe) {
    Copy-Item (Join-Path $srcDir "Nefarius.ViGEm.Client.dll") -Destination . -Force
    & .\pt_test.exe
    Remove-Item .\pt_test.exe -Force -ErrorAction SilentlyContinue
    Remove-Item .\Nefarius.ViGEm.Client.dll -Force -ErrorAction SilentlyContinue
}
