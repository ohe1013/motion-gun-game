param(
    [switch]$RunTests
)

$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCommand) {
    throw "Python executable not found in PATH. Install Python 3.12+ first."
}

$venvPython = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $venvPython)) {
    & $pythonCommand.Source -m venv (Join-Path $PSScriptRoot ".venv")
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create python\\.venv."
    }
}

& $venvPython -m pip install --upgrade pip
if ($LASTEXITCODE -ne 0) {
    throw "Failed to upgrade pip in python\\.venv."
}

& $venvPython -m pip install -r (Join-Path $PSScriptRoot "requirements.txt")
if ($LASTEXITCODE -ne 0) {
    throw "Failed to install python dependencies from requirements.txt."
}

if ($RunTests) {
    & (Join-Path $PSScriptRoot "run_tests.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "Python tests failed after bootstrap."
    }
}
