param(
    [string]$Executable = "$PSScriptRoot/../BedrockLauncher/bin/Release/net8.0-windows10.0.17763.0/bedli.exe"
)
$ErrorActionPreference = 'Stop'
$cases = @(
    @{ Arguments = @('--help'); Code = 0 },
    @{ Arguments = @('-h'); Code = 0 },
    @{ Arguments = @('help'); Code = 0 },
    @{ Arguments = @('HELP'); Code = 0 },
    @{ Arguments = @('help', 'extra'); Code = 2 },
    @{ Arguments = @('versions', 'extra'); Code = 2 },
    @{ Arguments = @(); Code = 2 },
    @{ Arguments = @('release'); Code = 2 },
    @{ Arguments = @('nightly', '1.21.100'); Code = 2 },
    @{ Arguments = @('0', '1.21.100'); Code = 2 },
    @{ Arguments = @('release', '../version'); Code = 2 },
    @{ Arguments = @('beta', 'latest'); Code = 2 },
    @{ Arguments = @('preview', '1.2'); Code = 2 },
    @{ Arguments = @('release', '1.21.100', 'extra'); Code = 2 }
)
foreach ($case in $cases) {
    $cliArguments = $case.Arguments
    $errorFile = [IO.Path]::GetTempFileName()
    try {
        # Native stderr is expected for usage errors.
        $ErrorActionPreference = 'Continue'
        $output = & $Executable @cliArguments 2> $errorFile
        $actualCode = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        $text = ($output -join "`n") + [IO.File]::ReadAllText($errorFile)
        if ($actualCode -ne $case.Code -or $text -notmatch 'Usage: bedli') {
            throw "Failed: bedli $cliArguments (exit $actualCode): $text"
        }
    } finally {
        Remove-Item -LiteralPath $errorFile
    }
}
Write-Output "Passed $($cases.Count) CLI smoke tests."
