# Set the mod version in every place it is hardcoded:
#   - the six AssemblyInfo.cs files (AssemblyVersion / AssemblyFileVersion)
#   - the [MelonInfo] version string in MelonEntry.cs
#   - Info.json + JipperKeyViewer-FileBased\Info.json ("Version")
#   - Repository.json ("Version" fields + releases/download/<ver>/ URLs)
#
# Usage:  ./tools/SetVersion.ps1 -Version 1.7.1
# The release workflow runs this on the build runner before msbuild, so release artifacts always
# carry the dispatched/tag version even though the repo files are bumped manually (classic
# non-SDK csproj ignores msbuild's /p:Version, which is why the sources are patched instead).
#
# 一键修改所有硬编码版本号：6 个 AssemblyInfo、MelonEntry 的 [MelonInfo] 版本串、两个
# Info.json、Repository.json（版本字段 + 下载 URL）。发版工作流在 msbuild 之前于构建机上运行
# 本脚本（经典 csproj 不吃 /p:Version，故直接改源码）；本地发版前也可手动运行。

param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$Version = $Version.TrimStart('v', 'V')
if ($Version -notmatch '^\d+(\.\d+)+$') { throw "Invalid version: '$Version' (expected e.g. 1.7.1)" }
$assemblyVersion = "$Version.0"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Update-File([string]$relPath, [scriptblock]$edit) {
    $path = Join-Path $repoRoot $relPath
    if (-not (Test-Path $path)) { throw "File not found: $path" }
    # Normalize the trailing newline so Set-Content's single appended newline never accumulates
    # extra blank lines across repeated runs. / 规范结尾换行，避免 Set-Content 追加的换行在
    # 多次运行后累积出多余空行。
    $text = (Get-Content $path -Raw).TrimEnd("`r", "`n")
    $new = & $edit $text
    if ($new -ne $text) {
        Set-Content -Path $path -Value $new -Encoding UTF8
        Write-Host "updated $relPath"
    } else {
        Write-Host "no change in $relPath"
    }
}

$assemblyInfos = @(
    'JipperKeyViewer\Properties\AssemblyInfo.cs',
    'JipperKeyViewer-FileBased\Properties\AssemblyInfo.cs',
    'JipperKeyViewer.Loader.UMM\Properties\AssemblyInfo.cs',
    'JipperKeyViewer.Loader.Melon\Properties\AssemblyInfo.cs',
    'JipperKeyViewer-FileBased.Loader.UMM\Properties\AssemblyInfo.cs',
    'JipperKeyViewer-FileBased.Loader.Melon\Properties\AssemblyInfo.cs'
)
foreach ($rel in $assemblyInfos) {
    Update-File $rel { param($t)
        $t -replace 'AssemblyVersion\("[^"]+"\)', ('AssemblyVersion("' + $assemblyVersion + '")') `
           -replace 'AssemblyFileVersion\("[^"]+"\)', ('AssemblyFileVersion("' + $assemblyVersion + '")')
    }
}

Update-File 'JipperKeyViewer.Loader.Melon\MelonEntry.cs' { param($t)
    $t -replace '("Jipper Key Viewer", ")[\d.]+(")', ('${1}' + $Version + '${2}')
}

foreach ($rel in @('Info.json', 'JipperKeyViewer-FileBased\Info.json')) {
    Update-File $rel { param($t)
        $t -replace '("Version"\s*:\s*")[^"]+(")', ('${1}' + $Version + '${2}')
    }
}

Update-File 'Repository.json' { param($t)
    $t -replace '("Version"\s*:\s*")[^"]+(")', ('${1}' + $Version + '${2}') `
       -replace '(releases/download/)[^/]+(/)', ('${1}' + $Version + '${2}')
}

Write-Host "Version set to $Version everywhere."
