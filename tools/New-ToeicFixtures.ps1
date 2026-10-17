param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\docs\fixtures')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Add-ZipTextEntry {
    param($Archive, [string]$Path, [string]$Content)
    $entry = $Archive.CreateEntry($Path)
    $stream = $entry.Open()
    $writer = [System.IO.StreamWriter]::new($stream, [System.Text.UTF8Encoding]::new($false))
    try { $writer.Write($Content) } finally { $writer.Dispose() }
}

function ConvertTo-WordParagraph {
    param([string]$Text)
    $escaped = [System.Security.SecurityElement]::Escape($Text)
    return '<w:p><w:r><w:t xml:space="preserve">' + $escaped + '</w:t></w:r></w:p>'
}

function New-MinimalDocx {
    param([string]$Path, [string[]]$Paragraphs)
    $tempPath = $Path + '.tmp'
    if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath -Force }
    $stream = [System.IO.File]::Open($tempPath, [System.IO.FileMode]::CreateNew)
    $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        Add-ZipTextEntry $archive '[Content_Types].xml' @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>
'@
        Add-ZipTextEntry $archive '_rels/.rels' @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
'@
        $body = ($Paragraphs | ForEach-Object { ConvertTo-WordParagraph $_ }) -join "`n"
        $document = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
            '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>' +
            $body + '<w:sectPr/></w:body></w:document>'
        Add-ZipTextEntry $archive 'word/document.xml' $document
    }
    finally {
        $archive.Dispose()
        $stream.Dispose()
    }
    Move-Item -LiteralPath $tempPath -Destination $Path -Force
}

function Add-QuestionParagraphs {
    param(
        [System.Collections.Generic.List[string]]$Target,
        [int]$Number,
        [bool]$IncludePrompt = $true
    )
    $Target.Add('[[QUESTION number="' + $Number + '" shuffleQuestion="false" shuffleOptions="true"]]')
    if ($IncludePrompt) { $Target.Add('[[PROMPT]]Synthetic test question ' + $Number + ' (not Reading v06 content).') }
    $Target.Add('[[OPTION key="A"]]Test option A')
    $Target.Add('[[OPTION key="B"]]Test option B')
    $Target.Add('[[OPTION key="C"]]Test option C')
    $Target.Add('[[OPTION key="D"]]Test option D')
    $Target.Add('[[ANSWER key="A"]]')
    $Target.Add('[[HINT]]Technical fixture only; this is not a real TOEIC answer.')
    $Target.Add('[[/QUESTION]]')
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null

$small = [System.Collections.Generic.List[string]]::new()
$small.Add('[[TOEIC-READING version="1.0"]]')
$small.Add('[[EXAM code="TEST-SMALL-V1" title="Small synthetic test fixture" duration="10" official="false"]]')
$small.Add('[[PART number="5"]]')
Add-QuestionParagraphs $small 101
$small.Add('[[/PART]]')
$small.Add('[[PART number="6"]]')
$small.Add('[[GROUP id="test-p6" title="Part 6 test" order="1" shuffleQuestions="false"]]')
$small.Add('[[DOCUMENT id="test-p6-doc"]]')
$small.Add('Synthetic passage with blank {{BLANK:131}}.')
$small.Add('[[/DOCUMENT]]')
Add-QuestionParagraphs $small 131 $false
$small.Add('[[/GROUP]]')
$small.Add('[[/PART]]')
$small.Add('[[PART number="7"]]')
$small.Add('[[GROUP id="test-p7" title="Part 7 test" order="2" shuffleQuestions="false"]]')
$small.Add('[[DOCUMENT id="test-p7-doc"]]')
$small.Add('Sample reading content for parser and UI testing only.')
$small.Add('[[/DOCUMENT]]')
Add-QuestionParagraphs $small 147
$small.Add('[[/GROUP]]')
$small.Add('[[/PART]]')
New-MinimalDocx (Join-Path $resolvedOutput 'toeic-test-small-valid.docx') $small

$official = [System.Collections.Generic.List[string]]::new()
$official.Add('[[TOEIC-READING version="1.0"]]')
$official.Add('[[EXAM code="TEST-OFFICIAL-100-V1" title="Synthetic 100-question fixture - NOT READING V06" duration="75" official="true"]]')
$official.Add('[[PART number="5"]]')
101..130 | ForEach-Object { Add-QuestionParagraphs $official $_ }
$official.Add('[[/PART]]')
$official.Add('[[PART number="6"]]')
for ($groupIndex = 0; $groupIndex -lt 4; $groupIndex++) {
    $first = 131 + ($groupIndex * 4)
    $last = $first + 3
    $official.Add('[[GROUP id="test-p6-' + ($groupIndex + 1) + '" title="Part 6 fixture ' + ($groupIndex + 1) + '" order="' + ($groupIndex + 1) + '" shuffleQuestions="false"]]')
    $official.Add('[[DOCUMENT id="test-p6-doc-' + ($groupIndex + 1) + '"]]')
    $markers = ($first..$last | ForEach-Object { '{{BLANK:' + $_ + '}}' }) -join ' '
    $official.Add('Synthetic Part 6 content: ' + $markers)
    $official.Add('[[/DOCUMENT]]')
    $first..$last | ForEach-Object { Add-QuestionParagraphs $official $_ $false }
    $official.Add('[[/GROUP]]')
}
$official.Add('[[/PART]]')
$official.Add('[[PART number="7"]]')
$nextNumber = 147
for ($groupIndex = 0; $groupIndex -lt 15; $groupIndex++) {
    $questionCount = if ($groupIndex -lt 9) { 4 } else { 3 }
    $lastNumber = $nextNumber + $questionCount - 1
    $official.Add('[[GROUP id="test-p7-' + ($groupIndex + 1) + '" title="Part 7 fixture" order="' + ($groupIndex + 5) + '" shuffleQuestions="false"]]')
    $official.Add('[[DOCUMENT id="test-p7-doc-' + ($groupIndex + 1) + '"]]')
    $official.Add('Synthetic content for technical testing only. This is not Reading v07.')
    $official.Add('[[/DOCUMENT]]')
    $nextNumber..$lastNumber | ForEach-Object { Add-QuestionParagraphs $official $_ }
    $official.Add('[[/GROUP]]')
    $nextNumber = $lastNumber + 1
}
$official.Add('[[/PART]]')
New-MinimalDocx (Join-Path $resolvedOutput 'toeic-test-official-100-valid.docx') $official

$invalid = [System.Collections.Generic.List[string]]::new()
$invalid.Add('[[TOEIC-READING version="1.0"]]')
$invalid.Add('[[EXAM code="TEST-INVALID" title="Intentionally invalid fixture" duration="75" official="true"]]')
$invalid.Add('[[PART number="5"]]')
Add-QuestionParagraphs $invalid 101
$invalid.Add('[[/PART]]')
New-MinimalDocx (Join-Path $resolvedOutput 'toeic-test-invalid-incomplete-official.docx') $invalid

Get-ChildItem -LiteralPath $resolvedOutput -Filter '*.docx' | Select-Object Name, Length
