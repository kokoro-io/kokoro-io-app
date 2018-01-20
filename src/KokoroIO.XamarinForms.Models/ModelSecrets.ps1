function writeMethod([string]$method,[string]$value){
	if (![System.String]::IsNullOrEmpty($value)) {
		Write-Output "static partial void $method(ref string v) => v = ""$value"";"
	}
}
Write-Output "internal static partial class ModelSecrets {"

writeMethod "SetGyazoClientId" $env:GYAZO_CLIENT_ID
writeMethod "SetGyazoClientSecret" $env:GYAZO_CLIENT_SECRET
writeMethod "SetImgurClientId" $env:IMGUR_CLIENT_ID
writeMethod "SetImgurClientSecret" $env:IMGUR_CLIENT_SECRET

Write-Output "}"