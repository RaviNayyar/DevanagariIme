# PowerShell script to build the Devanagari IME installer
# This builds the self-contained app and then creates the installer

Write-Host "Building Devanagari IME installer..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Build and publish self-contained application
Write-Host "Step 1: Building self-contained application..." -ForegroundColor Yellow
$publishResult = dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to publish application" -ForegroundColor Red
    exit 1
}

Write-Host "Application published successfully!" -ForegroundColor Green
Write-Host ""

# Step 2: Check if Inno Setup is installed
Write-Host "Step 2: Checking for Inno Setup..." -ForegroundColor Yellow
$innoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $innoSetupPath)) {
    # Try alternative location
    $innoSetupPath = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
}

if (-not (Test-Path $innoSetupPath)) {
    Write-Host "Error: Inno Setup not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Inno Setup from:" -ForegroundColor Yellow
    Write-Host "https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or manually compile setup.iss using Inno Setup Compiler" -ForegroundColor Yellow
    exit 1
}

Write-Host "Inno Setup found at: $innoSetupPath" -ForegroundColor Green
Write-Host ""

# Step 3: Compile the installer
Write-Host "Step 3: Compiling installer..." -ForegroundColor Yellow
& $innoSetupPath "setup.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installer built successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Installer location: installer\DevanagariIME-Setup.exe" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now distribute this installer to users." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Error: Failed to compile installer" -ForegroundColor Red
    Write-Host "Check the Inno Setup output above for errors." -ForegroundColor Yellow
    exit 1
}

