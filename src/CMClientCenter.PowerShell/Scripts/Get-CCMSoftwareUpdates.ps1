# Get-CCMSoftwareUpdates.ps1 — Separate script for the "Updates" page
# PS 5.1 compatible
#
# Anzeige-Quelle: CCM_UpdateStatus (root\ccm\SoftwareUpdates\UpdatesStore).
# Liefert Status="Installed"/"Missing" direkt, deckt "All Updates" und
# "Pending Updates" (= Filter auf Status=Missing in der UI) ab.
#
# WICHTIG: CCM_UpdateStatus ist eine Scan-HISTORIE, kein deduplizierter
# Bestand. Pro Update koennen mehrere Eintraege existieren — beobachtet u.a.
# durch mehrere Sources/SourceUniqueId (z.B. mehrere Management Points oder
# mehrere Scan-Zyklen), die jeweils einen eigenen UniqueId/Instanz-Datensatz
# fuer dasselbe Article+Version anlegen. Das alte "Client Center for
# Configuration Manager"-Tool dedupliziert das offenbar in der Anzeige.
# Wir bilden das hier nach: Gruppierung nach Title+RevisionNumber+Status,
# pro Gruppe wird nur der Eintrag mit dem neuesten ScanTime behalten.
#
# Installierbarkeit: CCM_SoftwareUpdate (ROOT\ccm\clientsdk) ist die einzige
# Klasse mit einer echten UpdateID, die CCM_SoftwareUpdatesManager.
# InstallUpdates benoetigt. CCM_UpdateStatus hat dieses Feld nicht.
#
# Deshalb: Title/Article-Abgleich zwischen beiden Quellen. Nur wenn ein
# Missing-Eintrag eine korrespondierende clientsdk-Instanz hat, wird
# InstallableUpdateId gesetzt — sonst bleibt sie leer (UI deaktiviert dann
# den Install-Button, z.B. weil der Client das Update noch nicht als
# "deployed and applicable" erkannt hat).
#
# Match-Strategie (in dieser Reihenfolge, erster Treffer gewinnt):
#   1. ArticleID-Gleichheit (zuverlaessigster Schluessel)
#   2. Title-Gleichheit als Fallback, falls ArticleID auf einer Seite leer
#      ist (z.B. Edge/WebView2-Updates ohne KB-Nummer, siehe Diagnose-Dump)

try {
    $statusEntries = @(Get-CimInstance -Namespace "root\ccm\SoftwareUpdates\UpdatesStore" `
                           -ClassName "CCM_UpdateStatus" -ErrorAction SilentlyContinue)
    if ($statusEntries.Count -eq 0) { return }

    # --- Deduplizierung: pro Title+RevisionNumber+Status nur den neuesten
    # ScanTime-Eintrag behalten. Group-Object + Sort ist hier klarer als ein
    # Hashtable-Fallthrough, da wir den "besten" Eintrag pro Gruppe brauchen,
    # nicht nur Existenz.
    $deduped = $statusEntries |
        Group-Object -Property Title, RevisionNumber, Status |
        ForEach-Object {
            $_.Group | Sort-Object -Property ScanTime -Descending | Select-Object -First 1
        }

    $installable = @(Get-CimInstance -Namespace "ROOT\ccm\clientsdk" `
                          -ClassName "CCM_SoftwareUpdate" -ErrorAction SilentlyContinue)

    # Lookup-Tabellen fuer den Abgleich, je einmal aufgebaut statt pro Zeile
    # neu zu durchsuchen (Performance bei 100+ Eintraegen).
    $byArticle = @{}
    $byTitle   = @{}
    foreach ($u in $installable) {
        $articleKey = [string]$u.ArticleID
        if ($articleKey -and -not $byArticle.ContainsKey($articleKey)) {
            $byArticle[$articleKey] = $u
        }
        $titleKey = [string]$u.Name
        if ($titleKey -and -not $byTitle.ContainsKey($titleKey)) {
            $byTitle[$titleKey] = $u
        }
    }

    foreach ($s in @($deduped)) {
        $match = $null
        $articleKey = [string]$s.Article
        if ($articleKey -and $byArticle.ContainsKey($articleKey)) {
            $match = $byArticle[$articleKey]
        } elseif ($byTitle.ContainsKey([string]$s.Title)) {
            $match = $byTitle[[string]$s.Title]
        }

        [PSCustomObject]@{
            UniqueId             = [string]$s.UniqueId
            Article              = [string]$s.Article
            Bulletin             = [string]$s.Bulletin
            Title                = [string]$s.Title
            Status               = [string]$s.Status
            RevisionNumber       = [int]$s.RevisionNumber
            ScanTime             = [string]$s.ScanTime
            UpdateClassification = [string]$s.UpdateClassification
            InstallableUpdateId  = if ($match) { [string]$match.UpdateID } else { $null }
        }
    }
} catch {}