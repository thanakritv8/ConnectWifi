Taskkill /IM ConnectWifi.exe /F
xcopy \\192.168.100.209\e-OPS\ReconnectWifi\ConnectWifi.exe c:\ReconnectWifi /Y
xcopy \\192.168.100.209\e-OPS\ReconnectWifi\SimpleWifi.dll c:\ReconnectWifi /Y
start C:\ReconnectWifi\ConnectWifi.exe


