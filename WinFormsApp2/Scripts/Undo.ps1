# Undo Help Desk Admin Setup – Windows 11 Pro 24H2
# Compatible with ScriptRunner EXE (live log output + confirmation dialog)

Import-Module Microsoft.PowerShell.LocalAccounts -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$timestamp] $Message"
    # Always output to stdout so ScriptRunner can capture it
    Write-Output $line
}

# --- Confirmation Dialog ---
$dialog = [System.Windows.Forms.MessageBox]::Show(
    "This will remove the Help Desk admin accounts, re-enable the built-in Administrator, and restore default settings.`n`nAre you sure you want to continue?",
    "Confirm Undo Help Desk Setup",
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Warning
)

if ($dialog -ne [System.Windows.Forms.DialogResult]::Yes) {
    Write-Log "Operation cancelled by user. No changes made."
    exit
}

# --- Script Variables ---
$PrimaryName  = "Help_Desk_Admin"
$BackupName   = "Help_Desk_CIAB"

Write-Log "=== UNDO SCRIPT STARTED ==="

# --- Re-enable built-in Administrator ---
if (Get-LocalUser -Name "Administrator" -ErrorAction SilentlyContinue) {
    try {
        Enable-LocalUser -Name "Administrator"
        Write-Log "Re-enabled built-in Administrator account."
    } catch {
        Write-Log ("Failed to re-enable Administrator: {0}" -f $_.Exception.Message)
    }
} else {
    Write-Log "Built-in Administrator not found."
}

# --- Unhide the backup Help_Desk_CIAB (if registry key exists) ---
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts\UserList"
if (Test-Path $regPath) {
    if (Get-ItemProperty -Path $regPath -Name $BackupName -ErrorAction SilentlyContinue) {
        try {
            Remove-ItemProperty -Path $regPath -Name $BackupName -ErrorAction SilentlyContinue
            Write-Log ("Unhid {0} from login screen." -f $BackupName)
        } catch {
            Write-Log ("Failed to unhide {0}: {1}" -f $BackupName, $_.Exception.Message)
        }
    } else {
        Write-Log ("{0} was already visible." -f $BackupName)
    }
}

# --- Ensure 'Other user' login option remains available ---
try {
    $sysPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
    if (-not (Test-Path $sysPath)) { New-Item -Path $sysPath -Force | Out-Null }
    New-ItemProperty -Path $sysPath -Name "dontdisplaylastusername" -Value 1 -PropertyType DWord -Force | Out-Null
    Write-Log "Ensured 'Other user' option remains visible at sign-in."
} catch {
    Write-Log ("Failed to enforce 'Other user' visibility: {0}" -f $_.Exception.Message)
}

# --- Remove Help Desk accounts (Primary + Backup) ---
foreach ($acct in @($PrimaryName, $BackupName)) {
    if (Get-LocalUser -Name $acct -ErrorAction SilentlyContinue) {
        try {
            Remove-LocalGroupMember -Group "Administrators" -Member $acct -ErrorAction SilentlyContinue
            Remove-LocalUser -Name $acct -ErrorAction SilentlyContinue
            Write-Log ("Removed local account {0}." -f $acct)
        } catch {
            Write-Log ("Failed to remove {0}: {1}" -f $acct, $_.Exception.Message)
        }
    } else {
        Write-Log ("Account {0} not found." -f $acct)
    }
}

# --- Ensure Administrator is in Administrators group ---
try {
    if (-not (Get-LocalGroupMember -Group "Administrators" -Member "Administrator" -ErrorAction SilentlyContinue)) {
        Add-LocalGroupMember -Group "Administrators" -Member "Administrator"
        Write-Log "Re-added Administrator to Administrators group."
    } else {
        Write-Log "Administrator already in Administrators group."
    }
} catch {
    Write-Log ("Error verifying Administrator group membership: {0}" -f $_.Exception.Message)
}

Write-Log "Undo complete. System restored to default local-admin state."
Write-Log "=== UNDO SCRIPT FINISHED ==="
