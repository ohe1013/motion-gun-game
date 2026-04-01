$venvPython = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
$python = $null

if (Test-Path $venvPython) {
    $python = $venvPython
} else {
    $pythonCommand = Get-Command python -ErrorAction SilentlyContinue
    if ($pythonCommand) {
        $python = $pythonCommand.Source
    }
}

if (-not $python) {
    throw "Python executable not found. Create python\\.venv or add python to PATH."
}

& $python -m unittest discover -s tests -v
