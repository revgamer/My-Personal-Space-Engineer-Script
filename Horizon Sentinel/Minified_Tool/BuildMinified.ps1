$ErrorActionPreference = "Stop"

$toolRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $toolRoot
$mergeTool = "F:\Space Engineers Script\minifiedtool\IngameScriptMergeTool\IngameScriptMergeTool.exe"
$solution = Join-Path $toolRoot "HorizonSentinel_Minified.sln"
$output = Join-Path $projectRoot "Horizon Sentinel.min.cs"

if (!(Test-Path -LiteralPath $mergeTool)) {
    throw "Missing IngameScriptMergeTool: $mergeTool"
}

& $mergeTool -s $solution -m | Set-Content -LiteralPath $output -Encoding ASCII

$text = Get-Content -Raw -LiteralPath $output
$open = ($text.ToCharArray() | Where-Object { $_ -eq "{" }).Count
$close = ($text.ToCharArray() | Where-Object { $_ -eq "}" }).Count
Write-Host "Wrote $output"
Write-Host "Chars $($text.Length) braces $open/$close"
