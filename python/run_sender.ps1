param(
    [int]$CameraIndex = 0,
    [string]$ServerHost = "127.0.0.1",
    [int]$Port = 5053,
    [ValidateSet("", "Left", "Right")]
    [string]$PrimaryLabel = "",
    [switch]$ShowPreview
)

$python = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $python)) {
    throw "Virtual environment not found. Run python -m venv .venv first."
}

$env:PYTHONPATH = Join-Path $PSScriptRoot "src"
$args = @(
    "-m",
    "motion_gun.main",
    "--camera-index",
    $CameraIndex,
    "--host",
    $ServerHost,
    "--port",
    $Port
)

if ($PrimaryLabel) {
    $args += @("--primary-label", $PrimaryLabel)
}

if ($ShowPreview) {
    $args += "--show-preview"
}

& $python @args
