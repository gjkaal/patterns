# ============================================================================================
# Script:  Using PS scripts and Curls to call webhooks
# Version: 1.0
# Owner:   GJH Kaal / Nice2Experience
# -----------------------------------
# 
# Plan as webjob : https://www.grax.com/2015/12/ridiculously-easy-scheduled-azure
# - extract libcurl zip in the same folder as this script, or add to cmd-path
# - script writes responses in the same folder, check access rights or modify the script
# - sample contains code to call an identityprovider, to explain dependent calls

# Use an identity provider, return an access token for the services
Write-Output 'Create basic authorization scheme'
$userName = 'gj.kaal@nice2experience.nl'
$password = 'secret'
$basicScheme = $userName + ':' + $password
$bytes = [System.Text.Encoding]::UTF8.GetBytes($basicScheme)
$encodedScheme =[Convert]::ToBase64String($bytes)
$basicToken = 'authorization: Basic ' + $encodedScheme

Write-Output 'Read access token from identity services'
$identityProvider = 'https://identity.acme.com/api/v1/oauth/?='
curl.exe --request GET --url $identityProvider --header $basicToken -o '.\Bearer.txt' -s
$jwtBearer = Get-Content -Path '.\Bearer.txt' -Raw
$authorizationToken = "authorization: Bearer " + $jwtBearer

Write-Output 'Call a webhook, using the access-token'
$serviceEndPoint = 'https://myfabulousService.com/api/v1'
$commandPath = '/myservice/command'
$executePath = $serviceEndPoint + $commandPath
$outputPath = '.\Command-Result.json'
curl.exe --request GET --url $executePath --header $authorizationToken -o $outputPath -s
Write-Output 'Call completed : ' + $executePath

$jsonBaseResult = Get-Content -Path $outputPath -Raw
Write-Output $jsonBaseResult

# Exit with error, if !Success
#  "Success":true
$matchResult = Get-ChildItem -Path $outputPath | Select-String -Pattern '"Success":true'

If ( $matchResult.Matches.Length -eq 0 )
{
    Write-Error ('Failed to complete : ' + $commandPath)
    exit 1
}
Else {
    Write-Output ('Completed successful : ' + $commandPath)
    exit 0
}