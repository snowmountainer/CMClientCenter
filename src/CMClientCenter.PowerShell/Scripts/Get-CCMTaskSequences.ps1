# Get-CCMTaskSequences.ps1 — Separate script for Operating Systems (Task Sequences)
# PS 5.1 compatible
#
# Task Sequences (inkl. OSD) werden im Client SDK NICHT ueber CCM_TaskSequence
# dargestellt (existiert auf vielen Clients nicht), sondern ueber CCM_Program
# im selben Namespace, erkennbar am Bool-Property "TaskSequence" = $true.
#
# HighImpact / CustomHighImpact* werden mitgeliefert, weil ConfigMgr fuer
# Bare-Metal/Wipe-and-Load-TS bereits admin-konfigurierte, mehrsprachige
# Warntexte mitbringt (siehe Site-Server-Konfiguration der Advertisement).
# Diese werden in der App fuer den Bestaetigungsdialog vor dem Ausfuehren
# verwendet statt eines selbst erfundenen Textes.

try {
    $programs = Get-CimInstance -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Program" -ErrorAction SilentlyContinue
    if ($programs -eq $null) { return }

    $taskSequences = @($programs | Where-Object { $_.TaskSequence -eq $true })

    foreach ($ts in $taskSequences) {
        [PSCustomObject]@{
            ProgramID                       = [string]$ts.ProgramID
            PackageID                       = [string]$ts.PackageID
            Name                            = [string]$ts.Name
            FullName                        = [string]$ts.FullName
            PackageName                     = [string]$ts.PackageName
            Description                     = [string]$ts.Description
            Publisher                       = [string]$ts.Publisher
            Version                         = [string]$ts.Version
            HighImpact                      = [bool]$ts.HighImpact
            HighImpactTaskSequence          = [bool]$ts.HighImpactTaskSequence
            CustomHighImpactSet             = [bool]$ts.CustomHighImpactSet
            CustomHighImpactHeadline        = [string]$ts.CustomHighImpactHeadline
            CustomHighImpactWarningTop      = [string]$ts.CustomHighImpactWarningTop
            CustomHighImpactWarning         = [string]$ts.CustomHighImpactWarning
            CustomHighImpactWarningInstall  = [string]$ts.CustomHighImpactWarningInstall
            EvaluationState                 = [int]$ts.EvaluationState
            LastRunStatus                   = [string]$ts.LastRunStatus
            LastRunTime                     = [string]$ts.LastRunTime
            RestartRequired                 = [bool]$ts.RestartRequired
            AdvertisedDirectly              = [bool]$ts.AdvertisedDirectly
            Published                       = [bool]$ts.Published
        }
    }
} catch {}
