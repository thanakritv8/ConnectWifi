Imports SimpleWifi
Public Class Wifi

    Public Shared Event ConnectionStatusChanged(isConnected As Boolean)

    Public Shared ReadOnly Property IsConnected() As Boolean
        Get
            Return _wifi.ConnectionStatus = SimpleWifi.WifiStatus.Connected
        End Get
    End Property

    Public Shared Function GetAccessPoints() As List(Of SimpleWifi.AccessPoint)
        Return _wifi.GetAccessPoints().
                OrderByDescending(Function(ap) ap.SignalStrength).
                ToList()
    End Function

    Public Shared Function Connect()
        If IsConnected Then Return True

        For Each ap In GetAccessPoints().Where(Function(x) x.HasProfile)
            If Connect(ap, New SimpleWifi.AuthRequest(ap)) Then Return True
        Next

        Return False
    End Function

    Public Shared Function Connect(ap As SimpleWifi.AccessPoint, ar As SimpleWifi.AuthRequest)
        Disconnect()

        AddHandler _wifi.ConnectionStatusChanged, AddressOf Wifi_ConnectionStatusChanged

        Return ap.Connect(ar)
    End Function

    Public Shared Sub Disconnect()
        _wifi.Disconnect()

        RemoveHandler _wifi.ConnectionStatusChanged, AddressOf Wifi_ConnectionStatusChanged
    End Sub

    Private Shared _wifi As SimpleWifi.Wifi =
            New SimpleWifi.Wifi()

    Private Shared Sub Wifi_ConnectionStatusChanged(sender As Object, e As SimpleWifi.WifiStatusEventArgs)
        RaiseEvent ConnectionStatusChanged(e.NewStatus = SimpleWifi.WifiStatus.Connected)
    End Sub

End Class
