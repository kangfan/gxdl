# Read-only: estimate imported RGBA32 size of PNGs under Assets/Resources, grouped by folder.
# Size comes from the PNG IHDR; the cap is DefaultTexturePlatform maxTextureSize in the .meta.
# ASCII only: Windows PowerShell 5.1 decodes BOM-less files as ANSI.
$root = (Resolve-Path "Assets\Resources").Path
$rows = @()
foreach ($f in Get-ChildItem $root -Recurse -File -Filter *.png) {
    $fs = [System.IO.File]::OpenRead($f.FullName)
    $buf = New-Object byte[] 24
    [void]$fs.Read($buf, 0, 24)
    $fs.Close()
    $w = ([int]$buf[16] -shl 24) -bor ([int]$buf[17] -shl 16) -bor ([int]$buf[18] -shl 8) -bor [int]$buf[19]
    $h = ([int]$buf[20] -shl 24) -bor ([int]$buf[21] -shl 16) -bor ([int]$buf[22] -shl 8) -bor [int]$buf[23]

    $max = 2048
    $compression = -1
    $androidOverride = 0
    $meta = $f.FullName + ".meta"
    if (Test-Path -LiteralPath $meta) {
        $inDefault = $false
        $inAndroid = $false
        foreach ($line in Get-Content -LiteralPath $meta) {
            if ($line -match "buildTarget: DefaultTexturePlatform") { $inDefault = $true; $inAndroid = $false; continue }
            if ($line -match "buildTarget: Android") { $inAndroid = $true; $inDefault = $false; continue }
            if ($line -match "buildTarget:") { $inDefault = $false; $inAndroid = $false; continue }
            if ($inDefault -and $line -match "^\s+maxTextureSize: (\d+)") { $max = [int]$Matches[1] }
            if ($inDefault -and $line -match "^\s+textureCompression: (\d+)") { $compression = [int]$Matches[1] }
            if ($inAndroid -and $line -match "^\s+overridden: (\d+)") { $androidOverride = [int]$Matches[1] }
        }
    }

    $scale = 1.0
    $longest = [Math]::Max($w, $h)
    if ($longest -gt $max) { $scale = $max / $longest }
    $iw = [Math]::Round($w * $scale)
    $ih = [Math]::Round($h * $scale)

    $rows += [pscustomobject]@{
        Dir = $f.DirectoryName.Substring($root.Length).TrimStart('\')
        Name = $f.Name; W = $w; H = $h; IW = $iw; IH = $ih
        Max = $max; Compression = $compression; AndroidOverride = $androidOverride
        Rgba = [long]$iw * [long]$ih * 4; Source = $f.Length
    }
}

"=== by folder (estimated RGBA32 after import, MB) ==="
$rows | Group-Object Dir | ForEach-Object {
    [pscustomobject]@{
        Dir = $_.Name
        Files = $_.Count
        RgbaMB = [Math]::Round((($_.Group | Measure-Object Rgba -Sum).Sum) / 1MB, 1)
        SourceMB = [Math]::Round((($_.Group | Measure-Object Source -Sum).Sum) / 1MB, 1)
        Uncompressed = @($_.Group | Where-Object { $_.Compression -eq 0 }).Count
        AndroidOverride = @($_.Group | Where-Object { $_.AndroidOverride -eq 1 }).Count
    }
} | Sort-Object RgbaMB -Descending | Format-Table -AutoSize | Out-String -Width 200

"=== totals ==="
"files: " + $rows.Count
"rgba total MB: " + [Math]::Round((($rows | Measure-Object Rgba -Sum).Sum) / 1MB, 1)
"source total MB: " + [Math]::Round((($rows | Measure-Object Source -Sum).Sum) / 1MB, 1)
"uncompressed files: " + @($rows | Where-Object { $_.Compression -eq 0 }).Count
"android override files: " + @($rows | Where-Object { $_.AndroidOverride -eq 1 }).Count

"=== by imported longest side ==="
$rows | Group-Object { $l = [Math]::Max($_.IW, $_.IH); if ($l -ge 1024) { "3: >=1024" } elseif ($l -ge 512) { "2: 512-1023" } else { "1: <512" } } |
    Sort-Object Name | ForEach-Object {
        "{0,-12} files {1,4}  rgba {2,7} MB" -f $_.Name, $_.Count, ([Math]::Round((($_.Group | Measure-Object Rgba -Sum).Sum) / 1MB, 1))
    }

"=== 512-1023 bucket by folder ==="
$rows | Where-Object { $l = [Math]::Max($_.IW, $_.IH); $l -ge 512 -and $l -lt 1024 } | Group-Object Dir | ForEach-Object {
    "{0,-36} files {1,3}  rgba {2,6} MB  e.g. {3}" -f $_.Name, $_.Count, ([Math]::Round((($_.Group | Measure-Object Rgba -Sum).Sum) / 1MB, 1)), (($_.Group | Select-Object -First 4 | ForEach-Object { $_.Name }) -join ", ")
}

"=== 20 largest ==="
$rows | Sort-Object Rgba -Descending | Select-Object -First 20 |
    ForEach-Object { "{0,6} MB  {1}x{2} (src {3}x{4}, max {5}, comp {6})  {7}\{8}" -f ([Math]::Round($_.Rgba / 1MB, 1)), $_.IW, $_.IH, $_.W, $_.H, $_.Max, $_.Compression, $_.Dir, $_.Name }
