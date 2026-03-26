$python = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $python)) {
    throw "Virtual environment not found. Run python -m venv .venv first."
}

& $python -m unittest discover -s tests -v
