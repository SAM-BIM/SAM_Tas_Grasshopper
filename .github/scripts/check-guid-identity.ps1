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
      component's file+declaring-type, a plugin/assembly GUID's value),
      fails. This is what specifically catches: an existing component's GUID
      silently changing, a component's GUID moving to a different file or a
      different type within the same file (including two existing
      components swapping GUIDs - each swapped GUID reports as an
      independent "moved" failure), and a removed component's GUID being
      silently reassigned to an unrelated replacement.

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

# Strips text that never actually compiles, so a "removed" component hidden
# behind a comment or a disabled preprocessor block is not silently counted
# as still live. Deliberately bounded, not a real C# parser:
#   - '//' line comments (existing behaviour).
#   - '/* ... */' block comments, including ones spanning multiple lines.
#   - '#if false' / '#if 0' ... matching '#endif' spans. Nesting is tracked
#     only for finding the matching #endif; any #else/#elif inside such a
#     span is conservatively also treated as inactive (real symbol
#     evaluation, e.g. '#if DEBUG', is out of scope - anything other than
#     the literal false/0 is assumed active, matching normal compilation).
function Get-ActiveCSharpText([string[]]$lines) {
  $kept = New-Object System.Collections.Generic.List[string]
  $skipDepth = 0
  foreach ($line in $lines) {
    $trimmed = $line.TrimStart()
    if ($trimmed -match '^#if\s+(false|0)\b') {
      $skipDepth++
      $kept.Add('')
      continue
    }
    if ($skipDepth -gt 0) {
      if ($trimmed -match '^#if\b') { $skipDepth++ }
      elseif ($trimmed -match '^#endif\b') { $skipDepth-- }
      $kept.Add('')
      continue
    }
    $kept.Add([regex]::Replace($line, '//.*$', ''))
  }
  $joined = $kept -join "`n"
  return [regex]::Replace($joined, '/\*[\s\S]*?\*/', '')
}

# SDK-style projects (Microsoft.NET.Sdk*) implicitly compile every **\*.cs
# under the project folder; <Compile Remove="..."> subtracts from that
# default glob. All six TAS Grasshopper projects are SDK-style, and three of
# them genuinely use Remove today (e.g. "Classes\**", "ToTAS\GH_Curve.cs") -
# some of the excluded files still declare a live-looking ComponentGuid on
# disk (dead code left behind mid-rewrite). Without honouring Remove, this
# script would inventory a GUID that never actually ships in the built GHA,
# and - the sharper failure - would not notice if a Remove is later added
# for a file whose component is genuinely still in use, silently dropping a
# real component out from under baseline protection while reporting green.
function Get-CompileExcludePatterns([string]$csprojPath) {
  [xml]$xml = Get-Content -LiteralPath $csprojPath -Raw
  $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
  return @($xml.SelectNodes('//*[local-name()="Compile"][@Remove]') | ForEach-Object { $_.Remove })
}

function Convert-MSBuildGlobToRegex([string]$pattern) {
  $normalized = $pattern -replace '\\', '/'
  $escaped = [regex]::Escape($normalized)
  # Order matters: collapse the escaped "**" token before the leftover "*".
  $escaped = $escaped -replace '\\\*\\\*', '.*'
  $escaped = $escaped -replace '\\\*', '[^/]*'
  return '^' + $escaped + '$'
}

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
  # Stripped through Get-ActiveCSharpText first (same as component identity
  # below): a stale Id left behind in a comment or a disabled #if block must
  # never be picked up ahead of the live declaration - a raw-text match would
  # report whichever occurrence comes first in the file, live or not.
  $kernelInfo = Join-Path $proj.FullName 'Kernel\AssemblyInfo.cs'
  if (Test-Path $kernelInfo) {
    $text = Get-ActiveCSharpText (Get-Content -LiteralPath $kernelInfo)
    $m = [regex]::Match($text, 'public\s+override\s+Guid\s+Id\s*\{[\s\S]*?return\s+new\s+Guid\("([0-9a-fA-F-]{36})"\)')
    if (-not $m.Success) {
      $errors.Add("Could not find an active (non-commented) GH_AssemblyInfo.Id in $kernelInfo")
    } else {
      $pluginIds[$name] = $m.Groups[1].Value.ToLowerInvariant()
    }
  }

  # --- Assembly identity ([assembly: Guid(...)]) --------------------------
  $propsInfo = Join-Path $proj.FullName 'Properties\AssemblyInfo.cs'
  if (Test-Path $propsInfo) {
    $text = Get-ActiveCSharpText (Get-Content -LiteralPath $propsInfo)
    $m = [regex]::Match($text, '\[assembly:\s*Guid\("([0-9a-fA-F-]{36})"\)\]')
    if (-not $m.Success) {
      $errors.Add("Could not find an active (non-commented) [assembly: Guid(...)] in $propsInfo")
    } else {
      $assemblyGuids[$name] = $m.Groups[1].Value.ToLowerInvariant()
    }
  }

  # --- Component identity (ComponentGuid) ---------------------------------
  $csprojPath = Join-Path $proj.FullName "$name.csproj"
  $excludeRegexes = @()
  if (Test-Path $csprojPath) {
    $excludeRegexes = @(Get-CompileExcludePatterns $csprojPath | ForEach-Object { Convert-MSBuildGlobToRegex $_ })
  }

  Get-ChildItem -Path $proj.FullName -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
    Where-Object {
      if ($excludeRegexes.Count -eq 0) { return $true }
      $projRel = $_.FullName.Substring($proj.FullName.Length).TrimStart('\', '/').Replace('\', '/')
      -not ($excludeRegexes | Where-Object { $projRel -imatch $_ })
    } |
    ForEach-Object {
      # Strip text that isn't actually compiled (line/block comments, disabled
      # #if false/#if 0 blocks) before matching, so a removed-but-still-present
      # ComponentGuid (e.g. dead code left during a rewrite) is not picked up
      # as a live identity - see Get-ActiveCSharpText above.
      $lines = Get-Content -LiteralPath $_.FullName
      $text = Get-ActiveCSharpText $lines
      # Guid(...) or the C# 9 target-typed `new ("...")` form - both appear in
      # this codebase.
      $matches = [regex]::Matches($text, 'Guid\s+ComponentGuid\s*(?:=>|\{[\s\S]*?get[\s\S]*?return)\s*new\s*(?:Guid)?\s*\(\s*"([0-9a-fA-F-]{36})"\s*\)')

      # Nearest-preceding-class-declaration heuristic (deliberately not a
      # real C# parser, same spirit as Get-ActiveCSharpText): when a file
      # declares more than one component/param type, two of them swapping
      # ComponentGuid values previously went undetected - both baseline
      # entries still mapped to the same File, so the strict comparison saw
      # no change. Recording the declaring type alongside File turns that
      # swap into two independent "value changed" failures, same as a swap
      # across two different files already produces.
      $classDecls = [regex]::Matches($text, '\bclass\s+(\w+)')
      foreach ($m in $matches) {
        $rel = $_.FullName.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $typeName = $null
        foreach ($c in $classDecls) {
          if ($c.Index -gt $m.Index) { break }
          $typeName = $c.Groups[1].Value
        }
        if (-not $typeName) {
          $errors.Add("Component identity (ComponentGuid): $rel has a ComponentGuid declaration at text offset $($m.Index) with no enclosing 'class' found before it - cannot attribute it to a type.")
          continue
        }
        $componentGuids.Add([pscustomobject]@{
          Guid = $m.Groups[1].Value.ToLowerInvariant()
          File = $rel
          Type = $typeName
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
      #
      # Deliberately NOT anchored to a specific accessibility modifier list
      # (a prior version required "(?:public|protected) override" with
      # nothing in between, which a legal `public sealed override` or
      # `protected internal override` silently slipped past - neither this
      # counter nor the literal extractor above requires a fixed modifier
      # prefix, so a mismatched form there produced 0 and 0: no
      # disagreement, no error, the component vanishing from the inventory
      # exactly like the unsupported-form case this check exists to catch).
      # `override` immediately followed by the property signature is the
      # real, non-optional signal - C# does not let any other member kind
      # legally precede `Guid ComponentGuid` with the `override` keyword.
      $declarationCount = [regex]::Matches($text, '\boverride\s+Guid\s+ComponentGuid\b').Count
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
  $componentByValue[$entry.Guid].Add("$($entry.File)#$($entry.Type)")
}
foreach ($v in $componentByValue.Keys) {
  if ($componentByValue[$v].Count -gt 1) {
    $errors.Add("Duplicate component GUID '$v' shared by: $($componentByValue[$v] -join ', ')")
  }
}

# --- Build current snapshot ----------------------------------------------

# Value is "File#Type", not just File: two components declared in the same
# file swapping ComponentGuid values must independently fail the strict
# comparison below, the same way a swap across two different files already
# does (see the "class declaration" comment above the extraction loop).
$componentMap = [ordered]@{}
foreach ($entry in ($componentGuids | Sort-Object Guid)) {
  $componentMap[$entry.Guid] = "$($entry.File)#$($entry.Type)"
}

$currentProjectNames = @($projects.Name)

$current = [ordered]@{
  schemaVersion  = 3
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
