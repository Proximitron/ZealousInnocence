# RimWorld 1.7 compatibility

The 1.7 version targets .NET 10 and references the installed RimWorld, Harmony,
and Dubs Bad Hygiene 1.7 assemblies. Earlier version folders are unchanged.

Build from the mod root with the .NET 10 SDK:

```powershell
dotnet build 1.7/Source/ZealousInnocence/ZealousInnocence.csproj -c Release
```

The project expects the mod in `RimWorld/Mods/ZealousInnocence`, Harmony in
Workshop item `2009463077`, and Dubs Bad Hygiene in Workshop item `836308268`.
For other installations, pass `-p:GameDir="C:/path/to/RimWorld/"` and, if needed,
`-p:WorkshopDir="C:/path/to/workshop/content/294100/"`. Keep the trailing slash.
Output goes to `1.7/Assemblies`. Close RimWorld before replacing a loaded DLL.

Compatibility fixes:

- Remove unused toilet-patch instance parameters that incorrectly resolved to
  this mod's job giver rather than Dubs Bad Hygiene's class. This prevents
  Harmony's binding validation from aborting the mod constructor.
- Mark the SpeakUp-only interaction field with its mod requirement and skip
  attempts to use an absent interaction.
- Remove unused imports for APIs absent from the 1.7 references.

Validation: Release build succeeded with 0 errors and 12 warnings. The toilet
prefix and postfix registered against the installed 1.7 Harmony and DBH
assemblies in an isolated validation process. The optional interaction attribute
and XML syntax were checked. The user reported successful startup after the
compatibility fixes, with existing patch-overlap diagnostics. Full gameplay,
save/load, multiplayer, and optional-mod combinations still need testing.
