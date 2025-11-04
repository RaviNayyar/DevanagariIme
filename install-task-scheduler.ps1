# PowerShell script to install Devanagari IME using Task Scheduler
# This works better for tray applications than Windows Services
# Run this script as Administrator (optional, but recommended)

param(
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

$taskName = "DevanagariIME"

# Try to find the executable in multiple locations
# First, try the installed location (when run from installer)
$exePath = Join-Path $PSScriptRoot "DevanagariIME.exe"

# If not found, try the development build location
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "bin\Debug\net6.0-windows\DevanagariIME.exe"
}

# If still not found, try Release build location
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "bin\Release\net6.0-windows\DevanagariIME.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Host "Error: Executable not found." -ForegroundColor Red
    Write-Host "Searched in:" -ForegroundColor Yellow
    Write-Host "  - $PSScriptRoot\DevanagariIME.exe" -ForegroundColor Gray
    Write-Host "  - $PSScriptRoot\bin\Debug\net6.0-windows\DevanagariIME.exe" -ForegroundColor Gray
    Write-Host "  - $PSScriptRoot\bin\Release\net6.0-windows\DevanagariIME.exe" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Please build the project first using: dotnet build" -ForegroundColor Yellow
    exit 1
}

if ($Uninstall) {
    Write-Host "Uninstalling Task Scheduler task..." -ForegroundColor Yellow
    
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($task) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "Task uninstalled successfully" -ForegroundColor Green
    } else {
        Write-Host "Task $taskName not found" -ForegroundColor Yellow
    }
} else {
    Write-Host "Installing Task Scheduler task..." -ForegroundColor Yellow
    
    # Check if task already exists
    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($existingTask) {
        Write-Host "Task already exists. Uninstalling first..." -ForegroundColor Yellow
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Start-Sleep -Seconds 1
    }
    
    # Create action to run the executable
    $action = New-ScheduledTaskAction -Execute $exePath -WorkingDirectory (Split-Path $exePath)
    
    # Create trigger to run at log on for any user
    $trigger = New-ScheduledTaskTrigger -AtLogOn
    
    # Create settings - run hidden and start when available
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable `
        -RunOnlyIfNetworkAvailable:$false `
        -Hidden
    
    # Create principal (run as current user when they log on)
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Highest
    
    # Register the task
    try {
        Register-ScheduledTask `
            -TaskName $taskName `
            -Action $action `
            -Trigger $trigger `
            -Settings $settings `
            -Principal $principal `
            -Description "Devanagari Input Method Editor - Automatically runs the IME tray application on user login" `
            -ErrorAction Stop
        
        Write-Host "Task installed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Starting the task now..." -ForegroundColor Cyan
        
        # Start the task immediately
        try {
            Start-ScheduledTask -TaskName $taskName -ErrorAction Stop
            Start-Sleep -Seconds 2
            
            $task = Get-ScheduledTask -TaskName $taskName
            if ($task.State -eq 'Running') {
                Write-Host "Task started successfully! The IME tray icon should appear in your system tray." -ForegroundColor Green
            } else {
                Write-Host "Task started (state: $($task.State)). The IME should appear shortly." -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "Warning: Could not start task automatically: $($_.Exception.Message)" -ForegroundColor Yellow
            Write-Host "You can start it manually with: Start-ScheduledTask -TaskName $taskName" -ForegroundColor Yellow
        }
        
        Write-Host ""
        Write-Host "The IME will also start automatically when you log in." -ForegroundColor Cyan
    }
    catch {
        Write-Host "Error: Failed to install task" -ForegroundColor Red
        Write-Host "Error message: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

