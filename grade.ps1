# BugHunt Unity 채점 (명령줄). grade.bat 이 이 스크립트를 부른다. 사용법은 README 의 "채점" 을 보세요.
#   grade.bat                        모든 랩 채점
#   grade.bat -NoGraphics            그래픽 장치 없이 (랩 39 는 SKIP)
#   grade.bat -Labs 23,24,50         이 랩만
#   grade.bat -Unity "D:\Unity\6000.3.27f2\Editor\Unity.exe"
# 결과: 콘솔 표, 프로젝트 폴더의 grade-report.json, Unity 로그 grade.log. 종료 코드: 만점 0, 아니면 1, 채점 실패 2.
param(
    [string]$Unity = "",
    [string]$Labs = "",
    [switch]$NoGraphics,
    [string]$Report = "",
    [int]$TimeoutMinutes = 20
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$project = $PSScriptRoot
if ($Report -eq "") { $Report = Join-Path $project "grade-report.json" }
$log = Join-Path $project "grade.log"

# 한글은 콘솔에서 두 칸을 차지한다. 표를 맞추려고 화면 폭으로 채운다.
function Pad-Display([string]$text, [int]$width) {
    $w = 0
    foreach ($ch in $text.ToCharArray()) { if ([int]$ch -ge 0x1100) { $w += 2 } else { $w += 1 } }
    return $text + (" " * [Math]::Max(1, $width - $w))
}

function Find-Unity {
    if ($Unity -ne "") { return $Unity }
    if ($env:BUGHUNT_UNITY) { return $env:BUGHUNT_UNITY }
    $versionLine = Get-Content (Join-Path $project "ProjectSettings\ProjectVersion.txt") | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    $version = ($versionLine -split ":", 2)[1].Trim()
    $roots = @("$env:ProgramFiles\Unity\Hub\Editor")
    $hubPath = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path $hubPath) {
        $secondary = (Get-Content $hubPath -Raw | ConvertFrom-Json)
        if ($secondary) { $roots = @($secondary) + $roots }
    }
    foreach ($root in $roots) {
        $candidate = Join-Path $root "$version\Editor\Unity.exe"
        if (Test-Path $candidate) { return $candidate }
    }
    throw "Unity $version 을(를) 찾지 못했습니다. -Unity 로 Unity.exe 경로를 주거나 환경 변수 BUGHUNT_UNITY 를 설정하세요."
}

if (Test-Path (Join-Path $project "Temp\UnityLockfile")) {
    try { [IO.File]::Open((Join-Path $project "Temp\UnityLockfile"), "Open", "ReadWrite", "None").Close() }
    catch { Write-Host "이 프로젝트가 Unity 에디터에 열려 있습니다. 에디터를 닫거나, 에디터 메뉴 BugHunt → 채점 을 쓰세요." -ForegroundColor Yellow; exit 2 }
}

$unityExe = Find-Unity
$arguments = @("-batchmode", "-projectPath", "`"$project`"", "-executeMethod", "BugHunt.Editor.Grader.RunFromCommandLine",
               "-bugHuntReport", "`"$Report`"", "-logFile", "`"$log`"")
if ($NoGraphics) { $arguments += "-nographics" }
if ($Labs -ne "") { $arguments += @("-bugHuntLabs", $Labs) }

Write-Host "BugHunt Unity 채점: $unityExe"
Write-Host "  (처음 한 번은 프로젝트를 불러오느라 몇 분 걸립니다. 진행 상황: $log)"
if (Test-Path $Report) { Remove-Item $Report }
$started = Get-Date
$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -PassThru -NoNewWindow
$null = $process.Handle   # 이걸 한 번 읽어 두어야 끝난 뒤 ExitCode 를 읽을 수 있다
if (-not $process.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    # 랩이 정말로 에디터를 굳혔다(HangGuard 를 지웠거나 다른 곳에서 멈춤). 채점기가 랩마다 미리 써 둔 결과가 남아 있다.
    Write-Host "$TimeoutMinutes 분이 지나도 끝나지 않아 Unity 를 강제로 끕니다." -ForegroundColor Yellow
    Stop-Process -Id $process.Id -Force
    $exitCode = 1
} else {
    $exitCode = $process.ExitCode
}

if (-not (Test-Path $Report)) {
    Write-Host "채점 결과($Report)가 없습니다. 컴파일 에러일 수 있습니다. $log 를 보세요." -ForegroundColor Red
    Select-String -Path $log -Pattern "error CS" | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
    exit 2
}

$result = Get-Content $Report -Raw -Encoding UTF8 | ConvertFrom-Json
$colors = @{ PASS = "Green"; FAIL = "Red"; CRASH = "Red"; TIMEOUT = "DarkYellow"; SKIP = "Yellow" }
$part = -1
foreach ($lab in $result.labs) {
    if ($lab.part -ne $part) { $part = $lab.part; Write-Host ""; Write-Host "  [$part 부]" }
    $line = "  {0,3}. {1}" -f $lab.id, $lab.title
    Write-Host (Pad-Display $line 30) -NoNewline
    Write-Host ("{0,-8}" -f $lab.result) -ForegroundColor $colors[$lab.result] -NoNewline
    Write-Host ("{0,7}ms  " -f $lab.ms) -NoNewline
    Write-Host $lab.detail -ForegroundColor DarkGray
}
$skipped = @($result.labs | Where-Object { $_.result -eq "SKIP" }).Count
$gradable = $result.total - $skipped
Write-Host ""
$score = "총점: $($result.passed) / $gradable"
if ($skipped -gt 0) { $score += " (SKIP ${skipped}개 제외)" }
Write-Host $score -ForegroundColor Cyan
Write-Host ("걸린 시간 {0:0}초, 결과 파일 {1}" -f ((Get-Date) - $started).TotalSeconds, $Report)
if ($exitCode -eq $null) { $exitCode = 2 }
exit $exitCode
