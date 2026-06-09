# Horizon Sentinel Minified Tool Project

This folder gives `IngameScriptMergeTool` a solution file to read.

Active source file:

```text
../Horizon Sentinel.cs
```

Horizon Sentinel is written as a raw paste-ready PB script. The minified
project runs `GenerateMergeSource.ps1` before compile to wrap that file in a
temporary `namespace Script` / `Program : MyGridProgram` file for the merge
tool.

Do not edit generated files. Edit `../Horizon Sentinel.cs`.

Build the paste-ready minified file:

```powershell
& "F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel\Minified_Tool\BuildMinified.ps1"
```
