$ErrorActionPreference = "Stop"

$toolRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $toolRoot
$source = Join-Path $projectRoot "AutoGrid-Manager-v3.0.cs"
$generatedDir = Join-Path $toolRoot "AGM3_Minified\Generated"
$generated = Join-Path $generatedDir "AGM3_Merge.cs"

if (!(Test-Path -LiteralPath $source)) {
    throw "Missing AutoGrid Manager source: $source"
}

if (!(Test-Path -LiteralPath $generatedDir)) {
    New-Item -ItemType Directory -Path $generatedDir | Out-Null
}

$body = Get-Content -LiteralPath $source -Raw
$header = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;
using VRage.Game.GUI.TextPanel;

namespace Script
{
    public sealed class Program : MyGridProgram
    {
'@

$footer = @'
    }
}
'@

$wrapped = $header + "`r`n" + $body + "`r`n" + $footer
Set-Content -LiteralPath $generated -Value $wrapped -Encoding UTF8
