<#
.SYNOPSIS
  Identity check for the six TAS Grasshopper projects: fails the build if any
  GUID collision is (re-)introduced, and fails if a previously-approved
  component GUID silently changes.

.DESCRIPTION
  Three independent categories are checked, on purpose kept separate (a
  collision in one category must never be masked by uniqueness in another):

    1. Plugin identity  - GH_AssemblyInfo.Id, the value Grasshopper itself
       uses to distinguish loaded plugin assemblies (Kernel/AssemblyInfo.cs).
       Two GHAs sharing this Id is what actually produces a duplicate-identity
       warning inside Rhino/Grasshopper.
    2. Assembly identity - the .NET [assembly: Guid(...)] COM typelib
       attribute (Properties/AssemblyInfo.cs). Inert while ComVisible(false),
       but still an accidental-duplication class of bug worth catching.
    3. Component identity - every GH_Component/GH_Param's ComponentGuid.
       These identify placed components inside saved .gh documents: they must
       never collide, AND an existing one must never change value silently
       (that would orphan every saved document referencing it). New
       components appearing with a new GUID is fine and expected.

  Run with -UpdateBaseline to regenerate baseline.json after a reviewed,
  intentional change (e.g. a brand new component, or a deliberate GUID
  correction like this script's own introduction).

.PARAMETER RepoRoot
  Root of the SAM_Tas_Grasshopper checkout. Defaults to two levels up from
  this script's location (.github/scripts/ -> repo root).

.PARAMETER UpdateBaseline
  Regenerate .github/scripts/guid-baseline.json from the current source tree
  instead of checking against it. Use only after manually reviewing the diff.

.EXAMPLE
  pwsh -File .github/scripts/check-guid-identity.ps1
#>
[CmdletBinding()]
param(
  [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
  [switch]$UpdateBaseline
)

$ErrorActionPreference = 'Stop'

$baselinePath = Join-Path $PSScriptRoot 'guid-baseline.json'
$ghRoot = Join-Path $RepoRoot 'Grasshopper'

if (-not (Test-Path $ghRoot)) {
  throw "Grasshopper folder not found at $ghRoot"
}

$projects = Get-ChildItem -Path $ghRoot -Directory | Sort-Object Name

$pluginIds     = [ordered]@{}
$assemblyGuids = [ordered]@{}
$componentGuids = New-Object System.Collections.Generic.List[object]
$errors = New-Object System.Collections.Generic.List[string]

foreach ($proj in $projects) {
  $name = $proj.Name

  # --- 1. Plugin identity (GH_AssemblyInfo.Id) ---------------------------
  $kernelInfo = Join-Path $proj.FullName 'Kernel\AssemblyInfo.cs'
  if (Test-Path $kernelInfo) {
    $text = Get-Content -LiteralPath $kernelInfo -Raw
    $m = [regex]::Match($text, 'public\s+override\s+Guid\s+Id\s*\{[\s\S]*?return\s+new\s+Guid\("([0-9a-fA-F-]{36})"\)')
    if (-not $m.Success) {
      $errors.Add("Could not find GH_AssemblyInfo.Id in $kernelInfo")
    } else {
      $pluginIds[$name] = $m.Groups[1].Value.ToLowerInvariant()
    }
  }

  # --- 2. Assembly identity ([assembly: Guid(...)]) ----------------------
  $propsInfo = Join-Path $proj.FullName 'Properties\AssemblyInfo.cs'
  if (Test-Path $propsInfo) {
    $text = Get-Content -LiteralPath $propsInfo -Raw
    $m = [regex]::Match($text, '\[assembly:\s*Guid\("([0-9a-fA-F-]{36})"\)\]')
    if (-not $m.Success) {
      $errors.Add("Could not find [assembly: Guid(...)] in $propsInfo")
    } else {
      $assemblyGuids[$name] = $m.Groups[1].Value.ToLowerInvariant()
    }
  }

  # --- 3. Component identity (ComponentGuid) ------------------------------
  Get-ChildItem -Path $proj.FullName -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
    ForEach-Object {
      # Strip '//' line comments before matching, so a commented-out
      # ComponentGuid override (e.g. dead code left during a rewrite) is not
      # picked up as a live identity - it isn't compiled, so it can't collide.
      $lines = Get-Content -LiteralPath $_.FullName |
        ForEach-Object { [regex]::Replace($_, '//.*$', '') }
      $text = $lines -join "`n"
      # Guid(...) or the C# 9 target-typed `new ("...")` form - both appear in
      # this codebase.
      foreach ($m in [regex]::Matches($text, 'Guid\s+ComponentGuid\s*(?:=>|\{[\s\S]*?get[\s\S]*?return)\s*new\s*(?:Guid)?\s*\(\s*"([0-9a-fA-F-]{36})"\s*\)')) {
        $rel = $_.FullName.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $componentGuids.Add([pscustomobject]@{
          Guid = $m.Groups[1].Value.ToLowerInvariant()
          File = $rel
        })
      }
    }
}

# --- Duplicate checks (within each category; categories never cross-checked) --

function Test-NoDuplicates([hashtable]$map, [string]$categoryLabel) {
  $byValue = @{}
  foreach ($k in $map.Keys) {
    $v = $map[$k]
    if (-not $byValue.ContainsKey($v)) { $byValue[$v] = New-Object System.Collections.Generic.List[string] }
    $byValue[$v].Add($k)
  }
  foreach ($v in $byValue.Keys) {
    if ($byValue[$v].Count -gt 1) {
      $errors.Add("Duplicate $categoryLabel GUID '$v' shared by: $($byValue[$v] -join ', ')")
    }
  }
}

Test-NoDuplicates $pluginIds 'plugin identity (GH_AssemblyInfo.Id)'
Test-NoDuplicates $assemblyGuids 'assembly identity ([assembly: Guid])'

$componentByValue = @{}
foreach ($entry in $componentGuids) {
  if (-not $componentByValue.ContainsKey($entry.Guid)) { $componentByValue[$entry.Guid] = New-Object System.Collections.Generic.List[string] }
  $componentByValue[$entry.Guid].Add($entry.File)
}
foreach ($v in $componentByValue.Keys) {
  if ($componentByValue[$v].Count -gt 1) {
    $errors.Add("Duplicate component GUID '$v' shared by: $($componentByValue[$v] -join ', ')")
  }
}

# --- Build current snapshot ---------------------------------------------

$componentMap = [ordered]@{}
foreach ($entry in ($componentGuids | Sort-Object Guid)) {
  $componentMap[$entry.Guid] = $entry.File
}

$current = [ordered]@{
  schemaVersion   = 1
  pluginIds       = $pluginIds
  assemblyGuids   = $assemblyGuids
  componentGuids  = $componentMap
}

if ($UpdateBaseline) {
  $current | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $baselinePath -Encoding utf8
  Write-Host "Baseline written to $baselinePath ($($pluginIds.Count) plugin ids, $($assemblyGuids.Count) assembly guids, $($componentMap.Count) component guids)."
  exit 0
}

# --- Compare against committed baseline: an existing component GUID must ---
# --- never silently change value or disappear. New ones are fine.        ---

if (-not (Test-Path $baselinePath)) {
  $errors.Add("No baseline found at $baselinePath. Run with -UpdateBaseline once, after review, to create it.")
}
else {
  $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json

  $baselineComponents = @{}
  if ($baseline.componentGuids) {
    foreach ($p in $baseline.componentGuids.PSObject.Properties) { $baselineComponents[$p.Name] = $p.Value }
  }

  foreach ($guid in $baselineComponents.Keys) {
    if (-not $componentMap.Contains($guid)) {
      $errors.Add("Baseline component GUID '$guid' (was $($baselineComponents[$guid])) is missing from the current source tree. If a component was deliberately removed, re-run with -UpdateBaseline after review.")
    }
    elseif ($componentMap[$guid] -ne $baselineComponents[$guid]) {
      # Same GUID, different file: could be a legitimate rename/move. Report,
      # don't fail - the GUID itself (what documents actually reference)
      # hasn't changed, only its declaring file.
      Write-Host "::notice::Component GUID '$guid' moved from '$($baselineComponents[$guid])' to '$($componentMap[$guid])'."
    }
  }

  $added = $componentMap.Keys | Where-Object { -not $baselineComponents.ContainsKey($_) }
  if ($added) {
    Write-Host "New component GUID(s) since baseline: $($added.Count) (expected for genuinely new components) -> re-run with -UpdateBaseline after review."
  }

  foreach ($name in $baseline.pluginIds.PSObject.Properties.Name) {
    $expected = $baseline.pluginIds.$name
    if (-not $pluginIds.Contains($name)) {
      $errors.Add("Baseline plugin identity for '$name' is missing (project renamed/removed?).")
    } elseif ($pluginIds[$name] -ne $expected) {
      $errors.Add("Plugin identity (GH_AssemblyInfo.Id) for '$name' changed from '$expected' to '$($pluginIds[$name])' without -UpdateBaseline.")
    }
  }

  foreach ($name in $baseline.assemblyGuids.PSObject.Properties.Name) {
    $expected = $baseline.assemblyGuids.$name
    if (-not $assemblyGuids.Contains($name)) {
      $errors.Add("Baseline assembly identity for '$name' is missing (project renamed/removed?).")
    } elseif ($assemblyGuids[$name] -ne $expected) {
      $errors.Add("Assembly identity ([assembly: Guid]) for '$name' changed from '$expected' to '$($assemblyGuids[$name])' without -UpdateBaseline.")
    }
  }
}

Write-Host "Checked $($projects.Count) TAS Grasshopper projects: $($pluginIds.Count) plugin ids, $($assemblyGuids.Count) assembly guids, $($componentMap.Count) component guids."

if ($errors.Count -gt 0) {
  Write-Host '--- GUID identity check FAILED ---'
  foreach ($e in $errors) { Write-Host "::error::$e" }
  exit 1
}

Write-Host 'GUID identity check passed: no duplicate plugin/assembly/component GUIDs, no unreviewed component GUID drift.'
