# Khởi chạy WMS Python AI Service trên Windows
Write-Host "Starting WMS Python AI Service..." -ForegroundColor Cyan

if (-not (Test-Path "venv")) {
    Write-Host "Creating virtual environment..." -ForegroundColor Yellow
    python -m venv venv
}

Write-Host "Activating virtual environment..." -ForegroundColor Green
& ".\venv\Scripts\Activate.ps1"

Write-Host "Installing dependencies..." -ForegroundColor Green
pip install -r requirements.txt

Write-Host "Running FastAPI via Uvicorn on http://localhost:8000..." -ForegroundColor Cyan
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
