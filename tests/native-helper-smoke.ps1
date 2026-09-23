param(
    [string]$Directory = "$PSScriptRoot/../BedrockLauncher/bin/bedli"
)
$ErrorActionPreference = 'Stop'
$helper = Join-Path $Directory 'LeviLauncher.exe'
if (!(Test-Path -LiteralPath $helper)) {
    throw 'Run a GDK install first to create the compatibility helper.'
}
$inputFile = [IO.Path]::GetTempFileName()
$errorFile = [IO.Path]::GetTempFileName()
$outputDirectory = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
try {
    # An existing invalid package reaches the caller check before parsing.
    # A missing file would return INPUT_NOT_FOUND and would not test authorization.
    [IO.File]::WriteAllBytes($inputFile, [byte[]](0..63))
    $ErrorActionPreference = 'Continue'
    & $helper --bedrock-msixvc-extract $inputFile $outputDirectory $errorFile 2>$null
    $actualCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $reason = [IO.File]::ReadAllText($errorFile)
    if ($actualCode -ne 3 -or $reason -ne 'NH_ERR_PARSE_FAILED') {
        throw "Compatibility helper failed: exit $actualCode, $reason"
    }
    Write-Output 'Passed native helper caller-compatibility test.'
} finally {
    Remove-Item -LiteralPath $inputFile, $errorFile
    if (Test-Path -LiteralPath $outputDirectory) {
        # A parse failure must not produce extracted files.
        [IO.Directory]::Delete($outputDirectory, $false)
    }
}
