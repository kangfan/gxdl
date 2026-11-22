#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$id = '__PipelineTest_' + [Guid]::NewGuid().ToString('N')
$batch = Join-Path $projectRoot ('ResourceCache/' + $id)
$targetFolder = Join-Path $projectRoot ('Assets/' + $id)
$manifestPath = Join-Path $batch 'manifest.json'
$null = New-Item -ItemType Directory -Path (Join-Path $batch 'raw') -Force
$passed = [Collections.Generic.List[string]]::new()
function Check([bool]$Ok, [string]$Name) { if (-not $Ok) { throw "FAIL: $Name" }; $passed.Add($Name) }
function Config {
    return @{ schemaVersion=1; generator=@{url='https://platform.openai.com/playground/images'}; entries=@(
        @{ key='test'; solidColor='#123456'; sourceSize=@(); crop=@(); size=@(16,16); alpha='opaque'; output='test.png'; target="$id/test.png" }
    ) }
}
function Run($Config, [bool]$Import=$false) {
    $Config | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'pipeline.ps1') -Manifest $manifestPath -Import:$Import | Out-Null
}
function Reject($Config, [string]$Name) {
    $failed=$false
    try { Run $Config $true } catch { $failed=$true }
    Check $failed $Name
}
try {
    $config=Config; Run $config
    Check (-not (Test-Path -LiteralPath $targetFolder)) 'prepare does not write Assets'
    Run $config $true
    $target=Join-Path $targetFolder 'test.png'; $originalHash=(Get-FileHash -LiteralPath $target).Hash
    $meta="fileFormatVersion: 2`nguid: $([Guid]::NewGuid().ToString('N'))`n"
    Set-Content -LiteralPath ($target+'.meta') -Value $meta -NoNewline
    Run $config $true
    $report=Get-Content -LiteralPath (Join-Path $batch 'pipeline-report.json') -Raw | ConvertFrom-Json
    Check ($report.entries[0].status -eq 'unchanged') 'identical import is skipped'
    $config.entries[0].solidColor='#654321'; Run $config $true
    $report=Get-Content -LiteralPath (Join-Path $batch 'pipeline-report.json') -Raw | ConvertFrom-Json
    Check ((Get-FileHash -LiteralPath $report.entries[0].backup).Hash -eq $originalHash) 'replacement backs up original bytes'
    Check ((Get-Content -LiteralPath ($target+'.meta') -Raw) -eq $meta) 'replacement preserves .meta and GUID'
    Copy-Item -LiteralPath $target -Destination (Join-Path $batch 'raw/source.png')
    $config=Config; $config.entries[0].Remove('solidColor'); $config.entries[0].source='source.png'; $config.entries[0].sourceSize=@(16,16)
    Run $config
    Check ((Get-FileHash -LiteralPath (Join-Path $batch 'processed/test.png')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $batch 'raw/source.png')).Hash) 'unchanged PNG preserves source bytes'
    $noise=[Drawing.Bitmap]::new(16,16)
    try { $noise.SetPixel(0,0,[Drawing.Color]::FromArgb(1,255,0,0)); $noise.SetPixel(1,0,[Drawing.Color]::FromArgb(3,0,255,0)); $noise.SetPixel(2,0,[Drawing.Color]::White); $noise.Save((Join-Path $batch 'raw/noise.png'),[Drawing.Imaging.ImageFormat]::Png) } finally { $noise.Dispose() }
    $clean=Config; $clean.entries[0].Remove('solidColor'); $clean.entries[0].source='noise.png'; $clean.entries[0].sourceSize=@(16,16); $clean.entries[0].alpha='transparent'; $clean.entries[0].alphaCutoff=2; Run $clean
    $image=[Drawing.Bitmap]::new((Join-Path $batch 'processed/test.png'))
    try { Check ($image.GetPixel(0,0).ToArgb() -eq 0 -and $image.GetPixel(1,0).A -eq 3 -and $image.GetPixel(2,0).A -eq 255) 'alpha cleanup removes only near-invisible noise and preserves art' } finally { $image.Dispose() }
    $clean.entries[0].alphaCutoff=9; Reject $clean 'reject excessive alpha cleanup'
    $config.entries[0].size=@(32,32); $config.entries[0].inset=4; $config.entries[0].alpha='transparent'; Run $config
    $image=[Drawing.Bitmap]::new((Join-Path $batch 'processed/test.png'))
    try { Check ($image.GetPixel(0,0).A -eq 0 -and $image.GetPixel(16,16).A -eq 255) 'inset preserves visible art and transparent border' } finally { $image.Dispose() }
    $config.entries[0].crop=@(12,12,16,16); Reject $config 'reject out-of-bounds crop'
    $config.entries[0].crop=@(); $config.entries[0].sourceSize=@(17,16); Reject $config 'reject incorrect source size'
    $config.entries[0].sourceSize=@(16,16); $config.entries[0].alpha='opaque'; Reject $config 'reject unexpected transparency'
    $config=Config; $config.entries[0].target='../ProjectSettings/escaped.png'; Reject $config 'reject target traversal'
    $config=Config; $config.entries[0].output='../raw/overwrite.png'; Reject $config 'protect raw originals'
    $config=Config; $config.entries += $config.entries[0].Clone(); $config.entries[1].key='other'; Reject $config 'reject duplicate destination'
    $config=Config; $config.entries[0].Remove('solidColor'); $config.entries[0].source='missing.png'; Reject $config 'reject missing source'
    $config=Config; $config.entries[0].Remove('solidColor'); $config.entries[0].source='source.png'; $config.entries[0].sourceSize=@(16,16); $config.entries[0].size=@(16,32); Reject $config 'reject distorted resize'
    $result=@{status='PASS';checks=$passed.Count;details=$passed;checkedAt=[DateTimeOffset]::Now.ToString('o')}
    $result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $projectRoot 'ResourceCache/pipeline-self-test.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 4
} finally {
    # Both disposable fixture paths are resolved and checked before recursive removal.
    foreach ($path in @($batch,$targetFolder)) {
        $resolved=[IO.Path]::GetFullPath($path)
        if (-not $resolved.StartsWith($projectRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -ne $id) { throw 'Unsafe fixture cleanup path.' }
        if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
    }
    if (Test-Path -LiteralPath ($targetFolder+'.meta')) { Remove-Item -LiteralPath ($targetFolder+'.meta') -Force }
}
