#requires -Version 7.0
param([Parameter(Mandatory)][string]$Manifest, [switch]$Import)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing
if (-not ('GuxuRaster' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'Raster.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core
}
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))

function Resolve-Below([string]$Base, [string]$Relative) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative) -or $Relative.Contains(':')) { throw "Expected relative path: $Relative" }
    $basePath = [IO.Path]::GetFullPath($Base).TrimEnd('\','/')
    $full = [IO.Path]::GetFullPath((Join-Path $basePath $Relative))
    if (-not $full.StartsWith($basePath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Path escapes allowed directory: $Relative" }
    $current = $full
    while ($current) {
        if ((Test-Path -LiteralPath $current) -and ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked path is not allowed: $current" }
        $current = [IO.Path]::GetDirectoryName($current)
    }
    return $full
}
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Write-Json($Value, [string]$Path) { $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Path -Encoding utf8 }

$cacheRoot = Join-Path $projectRoot 'ResourceCache'
$manifestPath = [IO.Path]::GetFullPath($Manifest)
$null = Resolve-Below $cacheRoot ([IO.Path]::GetRelativePath($cacheRoot, $manifestPath))
$batch = [IO.Path]::GetDirectoryName($manifestPath)
$config = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
if ($config.schemaVersion -ne 1 -or -not $config.entries) { throw 'Invalid pipeline manifest.' }
if ($config.generator['kind'] -eq 'user-provided-video') {
    $videoSource = Resolve-Below (Join-Path $batch 'raw') $config.generator.source
    if (-not (Test-Path -LiteralPath $videoSource -PathType Leaf) -or (Hash $videoSource) -ne $config.generator.sourceSha256) { throw 'User video source hash mismatch.' }
} elseif ($config.generator.url -ne 'https://platform.openai.com/playground/images') { throw 'Invalid pipeline generator.' }
$reportPath = Join-Path $batch 'pipeline-report.json'
$report = @{ status='processing'; checkedAt=[DateTimeOffset]::Now.ToString('o'); manifest=$manifestPath; entries=@() }
try {
    $keys = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $outputs = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $targets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $jobs = @()
    foreach ($entry in $config.entries) {
        if (-not $entry.key -or -not $keys.Add($entry.key)) { throw 'Missing or duplicate resource key.' }
        $source = if ($entry['source']) { Resolve-Below (Join-Path $batch 'raw') $entry.source } else { '' }
        if (($source -eq '') -eq [string]::IsNullOrEmpty($entry['solidColor'])) { throw 'Specify exactly one source or solidColor.' }
        if ($source -and -not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing source: $source" }
        if ($entry['solidColor'] -and $entry.solidColor -notmatch '^#[0-9a-fA-F]{6}$') { throw 'solidColor must be #RRGGBB.' }
        $output = Resolve-Below (Join-Path $batch 'processed') $entry.output
        $target = Resolve-Below (Join-Path $projectRoot 'Assets') $entry.target
        if ([IO.Path]::GetExtension($output) -ne '.png' -or [IO.Path]::GetExtension($target) -ne '.png') { throw 'Only PNG outputs and PNG asset targets are supported.' }
        if (-not $outputs.Add($output) -or -not $targets.Add($target)) { throw 'Duplicate output or target.' }
        $jobs += @{entry=$entry;source=$source;output=$output;target=$target}
    }
    # Validate and prepare the complete batch before touching any Unity assets.
    foreach ($job in $jobs) {
        $entry = $job.entry
        $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($job.output)) -Force
        [GuxuRaster]::Process($job.source, $job.output, [int[]]$entry.sourceSize, [int[]]$entry.crop, [int[]]$entry.size, $entry.alpha, $entry['solidColor'], [int]$entry['inset'], [int]$entry['alphaCutoff'], [int]$entry['alphaRemapFloor'])
        $report.entries += @{key=$entry.key;source=$job.source;sourceSha256=$(if($job.source){Hash $job.source}else{''});processed=$job.output;sha256=(Hash $job.output);target=$job.target;size=$entry.size;alpha=$entry.alpha;alphaRemapFloor=[int]$entry['alphaRemapFloor'];status='prepared'}
    }
    if ($Import) {
        $backupRoot = Join-Path $batch ('backups/' + [DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss-fff'))
        for ($i=0; $i -lt $jobs.Count; $i++) {
            $job = $jobs[$i]; $record = $report.entries[$i]
            if ((Test-Path -LiteralPath $job.target) -and (Hash $job.target) -eq $record.sha256) { $record.status='unchanged'; continue }
            if (Test-Path -LiteralPath $job.target) {
                $backup = Join-Path $backupRoot $job.entry.target
                $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($backup)) -Force
                Copy-Item -LiteralPath $job.target -Destination $backup
                if (Test-Path -LiteralPath ($job.target+'.meta')) { Copy-Item -LiteralPath ($job.target+'.meta') -Destination ($backup+'.meta') }
                $record.backup = $backup
            }
            $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($job.target)) -Force
            # Replace bytes in place; never recreate the .meta file or its GUID.
            Copy-Item -LiteralPath $job.output -Destination $job.target -Force
            if ((Hash $job.target) -ne $record.sha256) { throw "Copy verification failed: $($job.target)" }
            $record.status='copied'
        }
    }
    $report.status = if ($Import) { 'copied-awaiting-unity-validation' } else { 'prepared' }
    Write-Json $report $reportPath
    Write-Output ($report | ConvertTo-Json -Depth 12)
} catch {
    $report.status='failed'; $report.error=$_.Exception.Message
    Write-Json $report $reportPath
    throw
}
