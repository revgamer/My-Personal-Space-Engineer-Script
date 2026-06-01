# HogOS Minified Tool Project

This folder exists so `IngameScriptMergeTool` has a solution file to read.

Active source file:

```text
../Scripts/HogOS.cs
```

HogOS is written as a raw paste-ready PB script. The minified project runs
`GenerateMergeSource.ps1` before compile to wrap that file in a temporary
`namespace Script` / `Program : MyGridProgram` file for the merge tool.

Do not edit generated files. Edit `Scripts/HogOS.cs`.

Merge command:

```powershell
& "F:\Space Engineers Script\minifiedtool\IngameScriptMergeTool\IngameScriptMergeTool.exe" -s "F:\Space Engineers Script\My-Personal-Space-Engineer-Script\HogOS\Minified_Tool\HogOS_Minified.sln" -m -d "HogOS"
```
