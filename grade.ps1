# BugHunt Unity 채점 (명령줄). grade.bat 이 이 스크립트를 부른다. 사용법은 README 의 "채점" 을 보세요.
# BugHunt Unity 採点 (コマンドライン)。grade.bat がこのスクリプトを呼びます。使い方は README.ja.md の「採点」を見てください。
#   grade.bat                        모든 랩 채점 / 全ラボを採点
#   grade.bat -NoGraphics            그래픽 장치 없이 (랩 39 는 SKIP) / グラフィックスデバイスなしで (ラボ 39 は SKIP)
#   grade.bat -Labs 23,24,50         이 랩만 / このラボだけ
#   grade.bat -Lang ja               출력 언어 ko / ja (없으면 랩 실행 창에서 고른 언어, 그다음 OS 언어) / 出力言語 ko / ja (なければラボ実行ウィンドウで選んだ言語、次に OS の言語)
#   grade.bat -Unity "D:\Unity\6000.3.27f2\Editor\Unity.exe"
# 결과: 콘솔 표, 프로젝트 폴더의 grade-report.json, Unity 로그 grade.log. 종료 코드: 만점 0, 아니면 1, 채점 실패 2.
# 結果: コンソールの表、プロジェクトフォルダーの grade-report.json、Unity のログ grade.log。終了コード: 満点 0、それ以外 1、採点の失敗 2。
param(
    [string]$Unity = "",
    [string]$Labs = "",
    [switch]$NoGraphics,
    [string]$Report = "",
    [string]$Lang = "",
    [int]$TimeoutMinutes = 20
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$project = $PSScriptRoot
if ($Report -eq "") { $Report = Join-Path $project "grade-report.json" }
$log = Join-Path $project "grade.log"

# 이 스크립트가 직접 찍는 글의 언어. -Lang 이 없으면 OS 언어로 정하고, 채점 결과를 읽은 뒤에는 결과의 언어를 따른다.
# このスクリプトが自分で出す文字の言語。-Lang がなければ OS の言語で決め、採点結果を読んだ後は結果の言語に従います。
$script:ja = if ($Lang -ne "") { $Lang -match "^(ja|jp)" } else { (Get-UICulture).TwoLetterISOLanguageName -eq "ja" }
function T([string]$ko, [string]$jaText) { if ($script:ja) { return $jaText } else { return $ko } }

# 한글·가나는 콘솔에서 두 칸을 차지한다. 표를 맞추려고 화면 폭으로 채운다.
# ハングル・かなはコンソールで 2 マス分の幅を取ります。表をそろえるために表示幅で埋めます。
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
    throw (T "Unity $version 을(를) 찾지 못했습니다. -Unity 로 Unity.exe 경로를 주거나 환경 변수 BUGHUNT_UNITY 를 설정하세요." "Unity $version が見つかりませんでした。-Unity で Unity.exe のパスを指定するか、環境変数 BUGHUNT_UNITY を設定してください。")
}

if (Test-Path (Join-Path $project "Temp\UnityLockfile")) {
    try { [IO.File]::Open((Join-Path $project "Temp\UnityLockfile"), "Open", "ReadWrite", "None").Close() }
    catch { Write-Host (T "이 프로젝트가 Unity 에디터에 열려 있습니다. 에디터를 닫거나, 에디터 메뉴 BugHunt → 채점 을 쓰세요." "このプロジェクトが Unity エディターで開かれています。エディターを閉じるか、エディターのメニュー BugHunt → 採点 を使ってください。") -ForegroundColor Yellow; exit 2 }
}

$unityExe = Find-Unity
$arguments = @("-batchmode", "-projectPath", "`"$project`"", "-executeMethod", "BugHunt.Editor.Grader.RunFromCommandLine",
               "-bugHuntReport", "`"$Report`"", "-logFile", "`"$log`"")
if ($NoGraphics) { $arguments += "-nographics" }
if ($Labs -ne "") { $arguments += @("-bugHuntLabs", $Labs) }
if ($Lang -ne "") { $arguments += @("-bughuntLang", $Lang) }

Write-Host ((T "BugHunt Unity 채점: " "BugHunt Unity 採点: ") + $unityExe)
Write-Host (T "  (처음 한 번은 프로젝트를 불러오느라 몇 분 걸립니다. 진행 상황: $log)" "  (初回はプロジェクトの読み込みに数分かかります。進行状況: $log)")
if (Test-Path $Report) { Remove-Item $Report }
$started = Get-Date
$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -PassThru -NoNewWindow
$null = $process.Handle   # 이걸 한 번 읽어 두어야 끝난 뒤 ExitCode 를 읽을 수 있다 / これを一度読んでおかないと、終了後に ExitCode を読めない
if (-not $process.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    # 랩이 정말로 에디터를 굳혔다(HangGuard 를 지웠거나 다른 곳에서 멈춤). 채점기가 랩마다 미리 써 둔 결과가 남아 있다.
    # ラボが本当にエディターを固めた (HangGuard を消したか、別の場所で止まった)。採点ツールがラボごとに先に書いておいた結果が残っている。
    Write-Host (T "$TimeoutMinutes 분이 지나도 끝나지 않아 Unity 를 강제로 끕니다." "$TimeoutMinutes 分たっても終わらないので Unity を強制終了します。") -ForegroundColor Yellow
    Stop-Process -Id $process.Id -Force
    $exitCode = 1
} else {
    $exitCode = $process.ExitCode
}

if (-not (Test-Path $Report)) {
    Write-Host (T "채점 결과($Report)가 없습니다. 컴파일 에러일 수 있습니다. $log 를 보세요." "採点結果 ($Report) がありません。コンパイルエラーかもしれません。$log を見てください。") -ForegroundColor Red
    Select-String -Path $log -Pattern "error CS" | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
    exit 2
}

$result = Get-Content $Report -Raw -Encoding UTF8 | ConvertFrom-Json
# 결과의 랩 이름에 가나가 있으면 일본어로 채점한 것이다 / 結果のラボ名にかながあれば日本語で採点したもの
if ($Lang -eq "") { $script:ja = [bool]($result.labs | Where-Object { $_.title -match "[\u3040-\u30FF]" } | Select-Object -First 1) }
$colors = @{ PASS = "Green"; FAIL = "Red"; CRASH = "Red"; TIMEOUT = "DarkYellow"; SKIP = "Yellow" }
$part = -1
foreach ($lab in $result.labs) {
    if ($lab.part -ne $part) { $part = $lab.part; Write-Host ""; Write-Host (T "  [$part 부]" "  [第 $part 部]") }
    $line = "  {0,3}. {1}" -f $lab.id, $lab.title
    Write-Host (Pad-Display $line 30) -NoNewline
    Write-Host ("{0,-8}" -f $lab.result) -ForegroundColor $colors[$lab.result] -NoNewline
    Write-Host ("{0,7}ms  " -f $lab.ms) -NoNewline
    Write-Host $lab.detail -ForegroundColor DarkGray
}
$skipped = @($result.labs | Where-Object { $_.result -eq "SKIP" }).Count
$gradable = $result.total - $skipped
Write-Host ""
$score = (T "총점: " "総合点: ") + "$($result.passed) / $gradable"
if ($skipped -gt 0) { $score += (T " (SKIP ${skipped}개 제외)" " (SKIP ${skipped} 個を除く)") }
Write-Host $score -ForegroundColor Cyan
Write-Host ((T "걸린 시간 {0:0}초, 결과 파일 {1}" "所要時間 {0:0} 秒、結果ファイル {1}") -f ((Get-Date) - $started).TotalSeconds, $Report)
if ($exitCode -eq $null) { $exitCode = 2 }
exit $exitCode
