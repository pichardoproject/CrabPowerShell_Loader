# Help Desk Admin Setup – Windows 11 Pro 24H2
# Compatible with ScriptRunner EXE (live log output + confirmation dialog)

Import-Module Microsoft.PowerShell.LocalAccounts -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- Unified Log Output (works inside ScriptRunner EXE) ---
function Write-Log {
    param([string]$Message,[string]$Color="White")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$timestamp] $Message"
    Write-Output $line
}

# --- Confirmation Dialog ---
$dialog = [System.Windows.Forms.MessageBox]::Show(
    "This will create Help Desk administrator accounts, remove all other admin users, and modify login screen settings.`n`nAre you sure you want to continue?",
    "Confirm Help Desk Setup",
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Warning
)

if ($dialog -ne [System.Windows.Forms.DialogResult]::Yes) {
    Write-Log "Operation cancelled by user. No changes made."
    exit
}

# --- Variables ---
$PrimaryName  = "Help_Desk_Admin"
$PrimaryPass  = "crab1234"
$BackupName   = "Help_Desk_CRAB"
$BackupPass   = "CRAB!@#$"
$AllowedAdmins = @($PrimaryName, $BackupName)

function To-Secure($p) { ConvertTo-SecureString $p -AsPlainText -Force }

Write-Log "=== HELP DESK ADMIN SETUP STARTED ==="

# --- Create or update accounts ---
foreach ($acct in @($PrimaryName, $BackupName)) {
    $pass = if ($acct -eq $PrimaryName) { $PrimaryPass } else { $BackupPass }
    try {
        if (-not (Get-LocalUser -Name $acct -ErrorAction SilentlyContinue)) {
            New-LocalUser -Name $acct -Password (To-Secure $pass) -PasswordNeverExpires
            Write-Log ("Created {0}" -f $acct)
        } else {
            Set-LocalUser -Name $acct -Password (To-Secure $pass)
            Set-LocalUser -Name $acct -PasswordNeverExpires $true
            Write-Log ("Updated password for {0}" -f $acct)
        }
    } catch {
        Write-Log ("Error creating/updating {0}: {1}" -f $acct, $_.Exception.Message)
    }
}

# --- Add to Administrators group ---
foreach ($u in @($PrimaryName, $BackupName)) {
    try {
        if (-not (Get-LocalGroupMember -Group "Administrators" -Member $u -ErrorAction SilentlyContinue)) {
            Add-LocalGroupMember -Group "Administrators" -Member $u
            Write-Log ("Added {0} to Administrators group" -f $u)
        } else {
            Write-Log ("{0} already in Administrators group" -f $u)
        }
    } catch {
        Write-Log ("Error adding {0} to Administrators group: {1}" -f $u, $_.Exception.Message)
    }
}

# --- Hide only backup account ---
try {
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts\UserList"
    if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
    New-ItemProperty -Path $regPath -Name $BackupName -PropertyType DWord -Value 0 -Force | Out-Null
    Write-Log ("Hid {0} from login screen" -f $BackupName)
} catch {
    Write-Log ("Error hiding {0}: {1}" -f $BackupName, $_.Exception.Message)
}

# --- Remove admin rights from everyone else safely ---
try {
    $members = Get-LocalGroupMember -Group "Administrators" | Where-Object { $_.ObjectClass -eq "User" }

    foreach ($m in $members) {
        $short = if ($m.Name -like "*\*") { $m.Name.Split('\')[-1] } else { $m.Name }

        if ($AllowedAdmins -contains $short) { continue }
        if ($short -in @("DefaultAccount","WDAGUtilityAccount","Guest","SYSTEM","Administrator")) { continue }

        try {
            Remove-LocalGroupMember -Group "Administrators" -Member $m -Confirm:$false -ErrorAction Stop
            Write-Log ("Removed admin rights from {0}" -f $short)

            # Ensure user is still in Users group
            if (-not (Get-LocalGroupMember -Group "Users" -Member $short -ErrorAction SilentlyContinue)) {
                Add-LocalGroupMember -Group "Users" -Member $short -ErrorAction SilentlyContinue
                Write-Log ("Ensured {0} is part of Users group" -f $short)
            }
        } catch {
            Write-Log ("Skipped {0} (protected or locked account)" -f $short)
        }
    }
} catch {
    Write-Log ("Error processing Administrators group: {0}" -f $_.Exception.Message)
}

# --- Disable built-in Administrator ---
try {
    if (Get-LocalUser -Name "Administrator" -ErrorAction SilentlyContinue) {
        Disable-LocalUser -Name "Administrator"
        Write-Log "Disabled built-in Administrator account"
    }
} catch {
    Write-Log ("Error disabling Administrator: {0}" -f $_.Exception.Message)
}

# --- Enable both visible users + 'Other user' hybrid mode ---
try {
    $systemReg = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
    if (-not (Test-Path $systemReg)) { New-Item -Path $systemReg -Force | Out-Null }

    # Allow showing user tiles while keeping "Other user" button
    Set-ItemProperty -Path $systemReg -Name "dontdisplaylastusername" -Value 0 -Type DWord -Force

    # Enable user switching to show all visible accounts
    $switchReg = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\UserSwitch"
    if (-not (Test-Path $switchReg)) { New-Item -Path $switchReg -Force | Out-Null }
    Set-ItemProperty -Path $switchReg -Name "Enabled" -Value 1 -Type DWord -Force

    Write-Log "Enabled both user tiles and 'Other user' on sign-in screen"
} catch {
    Write-Log ("Error enabling hybrid login mode: {0}" -f $_.Exception.Message)
}

# --- Ensure remaining users are visible & enabled ---
try {
    Get-LocalUser | Where-Object {
        $_.Name -notin @($PrimaryName, $BackupName, "Administrator","DefaultAccount","Guest","WDAGUtilityAccount")
    } | ForEach-Object {
        try {
            if (-not $_.Enabled) {
                Enable-LocalUser -Name $_.Name
                Write-Log ("Enabled user {0}" -f $_.Name)
            }
            # Remove hidden flag if exists
            if (Test-Path $regPath) {
                Remove-ItemProperty -Path $regPath -Name $_.Name -ErrorAction SilentlyContinue
            }
            Write-Log ("Ensured user {0} is visible on login screen" -f $_.Name)
        } catch {
            Write-Log ("Skipped user {0} (permission or system-locked)" -f $_.Name)
        }
    }
} catch {
    Write-Log ("Error ensuring user visibility: {0}" -f $_.Exception.Message)
}

Write-Log "Setup complete."
Write-Log "Primary account:  Help_Desk_Admin / crab1234"
Write-Log "Backup account:   Help_Desk_CIAB / CRAB!@#$  (hidden)"
Write-Log "=== HELP DESK ADMIN SETUP FINISHED ==="
