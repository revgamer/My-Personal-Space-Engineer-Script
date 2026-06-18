# Developer Files

## Editable Source

```text
AutoGrid-Manager-v3.0.cs
```

This is the canonical unminified source. Edit and validate this file first.

## Minification Project

```text
Minified_Tool/AGM3_Minified.sln
```

`GenerateMergeSource.ps1` wraps the editable source for the local
`IngameScriptMergeTool`. Generated `bin`, `obj`, and merge-source files are
build artifacts and are not stored here.

## Archive

`Archive/` contains only meaningful recovery points:

- The stable Phase 1 script
- The last script before autocrafting/disassembly were removed
- The immediate pre-combined v3.0 development source
