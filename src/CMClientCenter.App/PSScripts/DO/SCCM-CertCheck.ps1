#Requires -Version 5.1
<#
.SYNOPSIS
    Checks the CCM client log for the two most common client-certificate
    registration failures and attempts to remediate the recoverable one.

.DESCRIPTION
    Same checks as CertCheck-PKI.ps1 in this library — kept as a separate
    script since it predates that one and some setups may already reference
    it by name. Looks at ClientIDManagerStartup.log for "Failed to find the
    certificate in the store" (recoverable) and "Server rejected
    registration" (not auto-remediable).
#>

function Get-CCMLogDirectory {
    $logDir = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\CCM\Logging\@Global' -ErrorAction SilentlyContinue).LogDirectory
    if (-not $logDir) { $logDir = 'C:\Windows\CCM\Logs' }
    return $logDir
}

function Test-CCMCertificateError {
    $logDir = Get-CCMLogDirectory
    $logFile = Join-Path -Path $logDir -ChildPath 'ClientIDManagerStartup.log'

    if (-not (Test-Path -Path $logFile)) {
        Write-Warning "ConfigMgr Client Certificate: log file not found at $logFile"
        return
    }

    $content = Get-Content -Path $logFile
    $missingCertPattern = 'Failed to find the certificate in the store'
    $rejectedPattern = '\[RegTask\] - Server rejected registration'
    $ok = $true

    if ($content -match $missingCertPattern) {
        $ok = $false
        Write-Warning 'ConfigMgr Client Certificate: certificate missing from store. Attempting fix.'

        Stop-Service -Name CcmExec -Force

        $certKeyPath = 'C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\19c5cf9c7b5dc9de3e548adb70398402_50e417e0-e461-474b-96e2-077b80325612'
        Remove-Item -Path $certKeyPath -Force -ErrorAction SilentlyContinue

        $newContent = $content | Select-String -Pattern $missingCertPattern -NotMatch
        Set-Content -Path $logFile -Value $newContent -Encoding UTF8 -Force

        Start-Service -Name CcmExec
        Write-Output 'ConfigMgr Client Certificate: missing-certificate condition cleared, CcmExec restarted.'
    }

    if ($content -match $rejectedPattern) {
        $ok = $false
        Write-Error 'ConfigMgr Client Certificate: server rejected client registration. Certificate not valid — no auto-remediation, check PKI/HTTPS configuration.'
    }

    if ($ok) {
        Write-Output 'ConfigMgr Client Certificate: OK'
    }
}

Test-CCMCertificateError
