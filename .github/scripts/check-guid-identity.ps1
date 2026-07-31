<#
.SYNOPSIS
  Identity check for the six TAS Grasshopper projects: fails the build if any
  GUID collision is (re-)introduced, or if the current source tree's identity
  inventory differs from the committed, reviewed baseline in ANY way.

.DESCRIPTION
  Four independent categories are checked, on purpose kept separate (a
  collision or drift in one category must never be masked by another):

    1. Project presence   - which project folders exist under Grasshopper/.
    2. Plugin identity     - GH_AssemblyInfo.Id, the value Grasshopper itself
       uses to distinguish loaded plugin assemblies (Kernel/AssemblyInfo.cs).
       Two GHAs sharing this Id is what actually produces a duplicate-identity
       warning inside Rhino/Grasshopper.
    3. Assembly identity   - the .NET [assembly: Guid(...)] COM typelib
       attribute (Properties/AssemblyInfo.cs). Inert while ComVisible(false),
       but still an accidental-duplication class of bug worth catching.
    4. Component identity  - every GH_Component/GH_Param's ComponentGuid.
       These identify placed components inside saved .gh documents.

  For every category, the check is STRICT and SYMMETRIC against
  guid-baseline.json: the current inventory must equal the baseline exactly.
    - A duplicate value within the current tree always fails (regardless of
      the baseline).
    - Anything in the baseline but absent from the current tree fails
      (accidental removal).
    - Anything in the current tree but absent from the baseline fails
      (unreviewed addition - e.g. a brand new component, project, or
      identity that nobody has approved into the baseline yet).
    - Anything present in both, but with a different associated value (a
      component's file, a plugin/assembly GUID's value), fails. This is
      what specifically catches: an existing component's GUID silently
      changing, a component's GUID moving to a different file (including
      two existing components swapping GUIDs - each swapped GUID reports
      as an independent "moved" failure), and a removed component's GUID
      being silently reassigned to an unrelated replacement.

  There is deliberately no "just log a notice" path for any of the above.
  A legitimate change (new component, intentional component move/rename,
  deliberate GUID correction, added/removed project) always requires a
  human to run -UpdateBaseline and commit the regenerated baseline as part
  of the same, reviewed change.

.PARAMETER RepoRoot
  Root of the SAM_Tas_Grasshopper checkout. Defaults to two levels up from
  this script's location (.github/scripts/ -> repo root).

.PARAMETER UpdateBaseline
  Regenerate .github/scripts/guid-baseline.json from the current source tree
  instead of checking against it. Use only after manually reviewing the diff.
  Running it twice on an unchanged tree must produce byte-identical output
  (no timestamps, no non-deterministic ordering).

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

$pluginIds      = [ordered]@{}
$assemblyGuids  = [ordered]@{}
$componentGuids = New-Object System.Collections.Generic.List[object]
$errors         = New-Object System.Collections.Generic.List[string]

foreach ($proj in $projects) {
  $name = $proj.Name

  # --- Plugin identity (GH_AssemblyInfo.Id) -------------------------------
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

  # --- Assembly identity ([assembly: Guid(...)]) --------------------------
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

  # --- Component identity (ComponentGuid) ---------------------------------
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
      $matches = [regex]::Matches($text, 'Guid\s+ComponentGuid\s*(?:=>|\{[\s\S]*?get[\s\S]*?return)\s*new\s*(?:Guid)?\s*\(\s*"([0-9a-fA-F-]{36})"\s*\)')
      foreach ($m in $matches) {
        $rel = $_.FullName.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $componentGuids.Add([pscustomobject]@{
          Guid = $m.Groups[1].Value.ToLowerInvariant()
          File = $rel
        })
      }

      # Independent sanity check: count every ComponentGuid *declaration*
      # (the property signature, regardless of body form) and compare against
      # how many of them the literal-constructor regex above actually
      # extracted a value from. A declaration using an unsupported form (e.g.
      # Guid.Parse("..."), a field-backed property, a ternary) would
      # otherwise silently vanish from the inventory - no baseline entry, no
      # duplicate check - reopening exactly the collision this script exists
      # to prevent. This does not need to understand the unsupported form,
      # only to notice the count disagrees.
      $declarationCount = [regex]::Matches($text, '(?:public|protected)\s+override\s+Guid\s+ComponentGuid\b').Count
      if ($declarationCount -gt $matches.Count) {
        $rel = $_.FullName.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $errors.Add("Component identity (ComponentGuid): $rel declares $declarationCount ComponentGuid override(s) but only $($matches.Count) could be parsed as a literal `new Guid(`"...`")` (or target-typed `new(`"...`")`) constant. Every ComponentGuid must use that literal form so this check can enumerate it - rewrite the declaration, or extend the parser if a new form is intentional.")
      }
    }
}

# --- Duplicate checks (within the CURRENT tree only; independent of the ---
# --- baseline and of each other category)                               ---

function Test-NoDuplicates([System.Collections.Specialized.OrderedDictionary]$map, [string]$categoryLabel) {
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

# --- Build current snapshot ----------------------------------------------

$componentMap = [ordered]@{}
foreach ($entry in ($componentGuids | Sort-Object Guid)) {
  $componentMap[$entry.Guid] = $entry.File
}

$currentProjectNames = @($projects.Name)

$current = [ordered]@{
  schemaVersion  = 2
  projects       = $currentProjectNames
  pluginIds      = $pluginIds
  assemblyGuids  = $assemblyGuids
  componentGuids = $componentMap
}

if ($UpdateBaseline) {
  $current | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $baselinePath -Encoding utf8
  Write-Host "Baseline written to $baselinePath ($($currentProjectNames.Count) projects, $($pluginIds.Count) plugin ids, $($assemblyGuids.Count) assembly guids, $($componentMap.Count) component guids)."
  exit 0
}

# --- Strict, symmetric comparison against the committed baseline. --------
# Every category must match exactly: nothing missing, nothing new and
# unreviewed, nothing changed in place. There is no "log and pass" path -
# any legitimate change requires -UpdateBaseline as part of the same,
# reviewed commit.

if (-not (Test-Path $baselinePath)) {
  $errors.Add("No baseline found at $baselinePath. Run with -UpdateBaseline once, after review, to create it.")
}
else {
  $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json

  # Generic strict key/value comparator, used for plugin ids, assembly
  # guids, and component guids alike - all three are "key -> value, and the
  # committed baseline is the full approved inventory" in exactly the same
  # shape, just with different key/value semantics.
  function Test-StrictBaselineMatch {
    param(
      [System.Collections.Specialized.OrderedDictionary]$Current,
      [hashtable]$Baseline,
      [string]$CategoryLabel,
      [string]$KeyLabel
    )
    $allKeys = @($Current.Keys) + @($Baseline.Keys) | Sort-Object -Unique
    foreach ($k in $allKeys) {
      $inCurrent  = $Current.Contains($k)
      $inBaseline = $Baseline.ContainsKey($k)
      if ($inBaseline -and -not $inCurrent) {
        $errors.Add("$CategoryLabel`: baseline $KeyLabel '$k' (value '$($Baseline[$k])') is missing from the current source tree. If this was deliberately removed, review it and re-run with -UpdateBaseline.")
      }
      elseif ($inCurrent -and -not $inBaseline) {
        $errors.Add("$CategoryLabel`: $KeyLabel '$k' (value '$($Current[$k])') is not present in the committed baseline. Review this new/changed identity, then re-run with -UpdateBaseline.")
      }
      elseif ($inCurrent -and $inBaseline -and $Current[$k] -ne $Baseline[$k]) {
        $errors.Add("$CategoryLabel`: $KeyLabel '$k' value changed from '$($Baseline[$k])' to '$($Current[$k])' without an -UpdateBaseline review. If '$k' is a component GUID, this means it moved to a different file (possibly swapped with another component) - saved .gh documents would silently resolve to the wrong component.")
      }
    }
  }

  # 1. Project presence.
  $baselineProjectNames = @($baseline.projects)
  foreach ($p in $currentProjectNames) {
    if ($baselineProjectNames -notcontains $p) {
      $errors.Add("Project presence: '$p' exists in the source tree but is not present in the committed baseline. Review it, then re-run with -UpdateBaseline.")
    }
  }
  foreach ($p in $baselineProjectNames) {
    if ($currentProjectNames -notcontains $p) {
      $errors.Add("Project presence: baseline project '$p' is missing from the current source tree. If deliberately removed, review it and re-run with -UpdateBaseline.")
    }
  }

  # 2. Plugin identity.
  $baselinePluginIds = @{}
  if ($baseline.pluginIds) { foreach ($p in $baseline.pluginIds.PSObject.Properties) { $baselinePluginIds[$p.Name] = $p.Value } }
  Test-StrictBaselineMatch -Current $pluginIds -Baseline $baselinePluginIds -CategoryLabel 'Plugin identity (GH_AssemblyInfo.Id)' -KeyLabel 'project'

  # 3. Assembly identity.
  $baselineAssemblyGuids = @{}
  if ($baseline.assemblyGuids) { foreach ($p in $baseline.assemblyGuids.PSObject.Properties) { $baselineAssemblyGuids[$p.Name] = $p.Value } }
  Test-StrictBaselineMatch -Current $assemblyGuids -Baseline $baselineAssemblyGuids -CategoryLabel 'Assembly identity ([assembly: Guid])' -KeyLabel 'project'

  # 4. Component identity.
  $baselineComponents = @{}
  if ($baseline.componentGuids) { foreach ($p in $baseline.componentGuids.PSObject.Properties) { $baselineComponents[$p.Name] = $p.Value } }
  Test-StrictBaselineMatch -Current $componentMap -Baseline $baselineComponents -CategoryLabel 'Component identity (ComponentGuid)' -KeyLabel 'GUID'
}

Write-Host "Checked $($projects.Count) TAS Grasshopper projects: $($pluginIds.Count) plugin ids, $($assemblyGuids.Count) assembly guids, $($componentMap.Count) component guids."

if ($errors.Count -gt 0) {
  Write-Host '--- GUID identity check FAILED ---'
  foreach ($e in $errors) { Write-Host "::error::$e" }
  exit 1
}

Write-Host 'GUID identity check passed: project set, plugin/assembly/component identities all match the committed baseline exactly.'
