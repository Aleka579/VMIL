# Publishes only the exact CI artifact whose ordinary-user guest acceptance passed23/23.
# A failed upload leaves a draft for inspection; existing releases are never overwritten.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:GITHUB_REPOSITORY -ne 'Aleka579/VMIL' -or
    $env:GITHUB_REF -ne 'refs/heads/release/v0.2.0-alpha.1' -or -not $env:GH_TOKEN) {
    throw 'This fixed release task must run in its authorized GitHub Actions context.'
}

$target = 'e27543fb5a8519b9333b57af26f95dc3873a732c'
$tag = 'v0.2.0-alpha.1'
$runId = 39109350478
$artifactId = 12130205017
$expectedZipHash = '77ab023491fb677eeaeff2d9860c9c771b718c762444eccd8d4e498d42de27b7'
$zipName = 'VMIdentityLab-0.2.0-alpha.1-win-x64.zip'
$manifestName = 'VMIdentityLab-0.2.0-alpha.1-win-x64.files.sha256'
$api = 'https://api.github.com/repos/Aleka579/VMIL'
$headers = @{
    Authorization = "Bearer $env:GH_TOKEN"
    Accept = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2026-03-10'
}
$utf8 = [Text.UTF8Encoding]::new($false)
$stage = Join-Path $env:RUNNER_TEMP 'verified-v02-alpha1'
New-Item -ItemType Directory -Path $stage | Out-Null
$zipPath = Join-Path $stage $zipName

$run = Invoke-RestMethod -Uri "$api/actions/runs/$runId" -Headers $headers
if ($run.head_sha -ne $target -or $run.status -ne 'completed' -or $run.conclusion -ne 'success') {
    throw 'The tested CI source or successful conclusion does not match.'
}
$artifact = Invoke-RestMethod -Uri "$api/actions/artifacts/$artifactId" -Headers $headers
if ($artifact.expired -or $artifact.workflow_run.id -ne $runId -or
    $artifact.workflow_run.head_sha -ne $target -or $artifact.size_in_bytes -ne 65197403 -or
    $artifact.digest -ne "sha256:$expectedZipHash") {
    throw 'The original CI artifact metadata does not match the accepted package.'
}
Invoke-WebRequest -Uri "$api/actions/artifacts/$artifactId/zip" -Headers $headers -OutFile $zipPath -TimeoutSec 300
if ((Get-Item -LiteralPath $zipPath).Length -ne 65197403 -or
    (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedZipHash) {
    throw 'Downloaded CI ZIP differs from the exact guest-tested bytes.'
}

$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $reader = [IO.StreamReader]::new($archive.GetEntry('BUILD-INFO.json').Open())
    try { $buildInfo = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($buildInfo.sourceCommit -ne $target -or $buildInfo.version -ne '0.2.0-alpha.1' -or
        $buildInfo.sdkVersion -ne '10.0.402' -or $buildInfo.includedFrameworks.Count -ne 2 -or
        @($buildInfo.includedFrameworks | Where-Object version -ne '10.0.13').Count -ne 0) {
        throw 'The artifact build identity differs from the accepted package.'
    }
    [string[]]$paths = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') } | ForEach-Object FullName)
    if ($paths.Count -ne 412) { throw 'Expected exactly412 product files.' }
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $manifest = [Text.StringBuilder]::new()
    foreach ($path in $paths) {
        $stream = $archive.GetEntry($path).Open()
        try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() }
        finally { $stream.Dispose() }
        [void]$manifest.Append($hash).Append('  ').Append($path).Append("`r`n")
    }
    [IO.File]::WriteAllText((Join-Path $stage $manifestName), $manifest.ToString(), $utf8)
} finally { $archive.Dispose() }
[IO.File]::WriteAllText((Join-Path $stage "$zipName.sha256"), "$expectedZipHash  $zipName`r`n", $utf8)
$expectedAssets = [ordered]@{
    $zipName = $expectedZipHash
    "$zipName.sha256" = 'a5128808d2e8f97e437ee63cb95ba8b47a1e8e499a8ccad4a1ee637df9c800d7'
    $manifestName = '1b9abe100ae15f4c3cb573f5590718ae7453b32fb501e0598ea747e8f13c1ea4'
}
foreach ($name in $expectedAssets.Keys) {
    if ((Get-FileHash -LiteralPath (Join-Path $stage $name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedAssets[$name]) {
        throw "Prepared asset differs from its reviewed local counterpart: $name"
    }
}

[array]$existing = Invoke-RestMethod -Uri "$api/releases?per_page=100" -Headers $headers
[array]$refs = Invoke-RestMethod -Uri "$api/git/matching-refs/tags/$tag" -Headers $headers
if (@($existing | Where-Object tag_name -eq $tag).Count -or
    @($refs | Where-Object ref -eq "refs/tags/$tag").Count) {
    throw 'The target release or tag already exists; inspect it instead of overwriting.'
}
$notesPath = Join-Path $env:GITHUB_WORKSPACE '.github/release-notes/v0.2.0-alpha.1.md'
$notes = [IO.File]::ReadAllText($notesPath)
$payload = @{
    tag_name = $tag
    target_commitish = $target
    name = 'v0.2.0-alpha.1 — VMware API配置管理预览'
    body = $notes
    draft = $true
    prerelease = $true
    generate_release_notes = $false
    make_latest = 'false'
} | ConvertTo-Json
$release = Invoke-RestMethod -Method Post -Uri "$api/releases" -Headers $headers -ContentType 'application/json; charset=utf-8' -Body $payload
Write-Output "Created draft release ID $($release.id) for $target"
foreach ($name in $expectedAssets.Keys) {
    $file = Get-Item -LiteralPath (Join-Path $stage $name)
    $uploadUri = "https://uploads.github.com/repos/Aleka579/VMIL/releases/$($release.id)/assets?name=$([Uri]::EscapeDataString($name))"
    $asset = Invoke-RestMethod -Method Post -Uri $uploadUri -Headers $headers -ContentType 'application/octet-stream' -InFile $file.FullName -TimeoutSec 300
    if ($asset.state -ne 'uploaded' -or $asset.size -ne $file.Length -or $asset.digest -ne "sha256:$($expectedAssets[$name])") {
        throw "Release asset verification failed; draft retained: $name"
    }
    Write-Output "Verified uploaded asset $name ($($asset.size) bytes, $($asset.digest))"
}
[array]$assets = Invoke-RestMethod -Uri "$api/releases/$($release.id)/assets" -Headers $headers
if ($assets.Count -ne 3) { throw 'Expected exactly three verified release assets; draft retained.' }
$published = Invoke-RestMethod -Method Patch -Uri "$api/releases/$($release.id)" -Headers $headers -ContentType 'application/json; charset=utf-8' -Body '{"draft":false,"prerelease":true,"make_latest":"false"}'
$tagRef = Invoke-RestMethod -Uri "$api/git/ref/tags/$tag" -Headers $headers
if ($published.draft -or -not $published.prerelease -or -not $published.published_at -or
    $tagRef.object.type -ne 'commit' -or $tagRef.object.sha -ne $target) {
    throw 'Publication result needs inspection; do not repeat the release operation.'
}
Write-Output "Published verified prerelease: $($published.html_url)"
@{
    url = $published.html_url
    releaseId = $published.id
    tag = $tag
    targetCommit = $target
    prerelease = $published.prerelease
    assets = @($assets | Select-Object name, size, digest, browser_download_url)
} | ConvertTo-Json -Depth 5
"Published [$tag]($($published.html_url)) from $target with three verified assets." >> $env:GITHUB_STEP_SUMMARY
