param(
    [int]$CameraIndex = 0,
    [ValidateSet("auto", "dshow", "msmf", "default")]
    [string]$CameraBackend = "auto",
    [string]$ServerHost = "127.0.0.1",
    [int]$Port = 5053,
    [string]$ConfigPath = "",
    [string]$SaveConfig = "",
    [ValidateSet("", "Left", "Right")]
    [string]$PrimaryLabel = "",
    [switch]$ShowPreview
)

$python = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $python)) {
    throw "Virtual environment not found. Run .\bootstrap.ps1 first."
}

function Test-ModuleAvailable {
    param(
        [string]$ModuleName
    )

    & $python -c "import importlib.util, sys; raise SystemExit(0 if importlib.util.find_spec('$ModuleName') else 1)" | Out-Null
    return $LASTEXITCODE -eq 0
}

$missingModules = @()
foreach ($moduleName in @("cv2", "mediapipe", "numpy")) {
    if (-not (Test-ModuleAvailable -ModuleName $moduleName)) {
        $missingModules += $moduleName
    }
}

if ($missingModules.Count -gt 0) {
    $missingList = $missingModules -join ", "
    throw "Missing Python modules: $missingList. Run .\bootstrap.ps1 or .\.venv\Scripts\python.exe -m pip install -r requirements.txt first."
}

$env:PYTHONPATH = Join-Path $PSScriptRoot "src"
$args = @(
    "-m",
    "motion_gun.main",
    "--camera-index",
    $CameraIndex,
    "--camera-backend",
    $CameraBackend,
    "--host",
    $ServerHost,
    "--port",
    $Port
)

if ($ConfigPath) {
    $args += @("--config", $ConfigPath)
}

if ($SaveConfig) {
    $args += @("--save-config", $SaveConfig)
}

if ($PrimaryLabel) {
    $args += @("--primary-label", $PrimaryLabel)
}

if ($ShowPreview) {
    $args += "--show-preview"
}

& $python @args
