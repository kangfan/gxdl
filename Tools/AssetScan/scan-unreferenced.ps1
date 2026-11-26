$corpus = Get-Content "Reference\HomeRework20261123\text-corpus.txt"
$text = ""
foreach ($f in $corpus) {
    if ($f -like "*\Assets\Resources\Game\*") { continue }
    $text += (Get-Content -LiteralPath $f -Raw -ErrorAction SilentlyContinue)
}
$rows = @()
foreach ($a in Get-ChildItem "Assets\Resources" -Recurse -File -Include *.png, *.jpg, *.mp3, *.wav) {
    if ($text -notmatch [regex]::Escape($a.BaseName)) {
        $rows += [pscustomobject]@{ Size = $a.Length; Path = $a.FullName }
    }
}
$rows | Sort-Object Size -Descending | ForEach-Object { "{0}`t{1}" -f $_.Size, $_.Path } |
    Set-Content -Encoding utf8 "Reference\HomeRework20261123\candidate-unreferenced.txt"
"candidates: " + $rows.Count
