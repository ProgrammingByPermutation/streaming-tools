# Notification App

## Getting TTS Voices

1. Open regedit
1. Navigate to `Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Speech_OneCore\Voices\Tokens`
1. Export the ones you want
1. Navigate to `Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices\Tokens`
1. Edit the exported regedit file changing `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Speech_OneCore\Voices\Tokens` to `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices\Tokens`
1. Run the regedit file

TODO: Send pull request to `https://github.com/dotnet/runtime`