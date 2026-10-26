# Generates the .resx resource files from strings.json.
# ASCII-only script on purpose: the JSON is read with an explicit UTF-8 encoding.
param(
    [string]$ProjectRoot = "C:\Users\ITA6-TN01\Documents\Standardprojekt\BinObjCleaner"
)

$ErrorActionPreference = "Stop"

$jsonPath = Join-Path $ProjectRoot "src\BinObjCleaner\Strings\strings.json"
$outDir = Join-Path $ProjectRoot "src\BinObjCleaner\Strings"

$utf8 = New-Object System.Text.UTF8Encoding($false)
$text = [System.IO.File]::ReadAllText($jsonPath, [System.Text.Encoding]::UTF8)
$data = $text | ConvertFrom-Json

function New-ResxContent {
    param([hashtable]$Values)

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
    [void]$sb.AppendLine('<root>')
    [void]$sb.AppendLine('  <resheader name="resmimetype">')
    [void]$sb.AppendLine('    <value>text/microsoft-resx</value>')
    [void]$sb.AppendLine('  </resheader>')
    [void]$sb.AppendLine('  <resheader name="version">')
    [void]$sb.AppendLine('    <value>2.0</value>')
    [void]$sb.AppendLine('  </resheader>')
    [void]$sb.AppendLine('  <resheader name="reader">')
    [void]$sb.AppendLine('    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>')
    [void]$sb.AppendLine('  </resheader>')
    [void]$sb.AppendLine('  <resheader name="writer">')
    [void]$sb.AppendLine('    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>')
    [void]$sb.AppendLine('  </resheader>')

    foreach ($key in ($Values.Keys | Sort-Object)) {
        $value = [string]$Values[$key]
        $escaped = $value.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
        [void]$sb.AppendLine("  <data name=`"$key`" xml:space=`"preserve`">")
        [void]$sb.AppendLine("    <value>$escaped</value>")
        [void]$sb.AppendLine('  </data>')
    }

    [void]$sb.AppendLine('</root>')
    return $sb.ToString()
}

# Neutral fallback (empty).
$neutral = @{}
[System.IO.File]::WriteAllText((Join-Path $outDir "Strings.resx"), (New-ResxContent -Values $neutral), $utf8)

foreach ($culture in @("de", "en")) {
    $values = @{}
    foreach ($prop in $data.PSObject.Properties) {
        $entry = $prop.Value
        if ($null -ne $entry.PSObject.Properties[$culture]) {
            $values[$prop.Name] = [string]$entry.$culture
        }
    }
    $file = Join-Path $outDir "Strings.$culture.resx"
    [System.IO.File]::WriteAllText($file, (New-ResxContent -Values $values), $utf8)
    "wrote $file ($($values.Count) entries)"
}
