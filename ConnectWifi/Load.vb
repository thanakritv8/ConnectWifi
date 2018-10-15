Imports System.ServiceProcess
Public Class Load

#Region "Variable"

    Dim cmd() As String
    Dim ssid As String
    Dim ip1 As String
    Dim ip2 As String
    Dim serviceApp As String
    Dim _t As System.Threading.Thread
    Dim _con As SqlClient.SqlConnection
    Dim nameComputer As String
    Dim p() As Process

#End Region

#Region "Information resource"
    Private Sub CheckIfRunning()
        p = Process.GetProcessesByName("ConnectWifi")
        If p.Count > 0 Then
            MsgBox("Process is running", MsgBoxStyle.Information, "Information")
            'Application.Exit()
            ' Process is running
        Else
            ' Process is not running
        End If
    End Sub
    Public Sub GetCPU(ByRef cCpu As Double, ByRef cRam As Double)
        Dim cpuCounter As New PerformanceCounter("Processor", "% Processor Time", "_Total")
        Dim ramCounter As New PerformanceCounter("Memory", "Available MBytes")
        cCpu = getCurrentCpuUsage(cpuCounter).ToString("n2")
        cRam = getAvailableRAM(ramCounter)
    End Sub
    Public Function getCurrentCpuUsage(ByVal cpuCounter As PerformanceCounter) As Double
        cpuCounter.NextValue()
        Threading.Thread.Sleep(100)
        Return cpuCounter.NextValue()
    End Function

    Public Function getAvailableRAM(ByVal ramCounter As PerformanceCounter) As Double
        Return ramCounter.NextValue()
    End Function

#End Region

#Region "Record Log"

    Private Sub SendLog(ByVal Problem As String)
        Dim cCpu As Double
        Dim cRam As Double
        GetCPU(cCpu, cRam)
        InsertLog(Format(Now, "yyyy-MM-dd HH:mm:ss") & vbTab & Problem & vbTab & "CPU Use " & cCpu & " %" & vbTab & "Ram " & cRam & " Available MBytes")
        'WriteTxt(Format(Now, "yyyy-MM-dd HH:mm:ss") & vbTab & Problem & vbTab & "CPU Use " & cCpu & " %" & vbTab & "Ram " & cRam & " Available MBytes")
    End Sub
    Public Sub WriteTxt(ByVal Msg As String)
        Dim FILE_NAME As String = Application.StartupPath & "\Log.txt"
        If System.IO.File.Exists(FILE_NAME) = True Then
            Dim objWriter As New System.IO.StreamWriter(FILE_NAME, True)
            objWriter.WriteLine(Msg)
            objWriter.Close()
        End If
    End Sub
    Private Sub InsertLog(ByVal _msg As String)
        Dim _SQL As String = "INSERT INTO [WiFiLog].[dbo].[ProblemLog] (Machine, ListData, Status) VALUES ('" & nameComputer & "', '" & _msg & "', 0)"
        objDB.ExecuteSQL(_SQL, _con)
    End Sub

#End Region

#Region "Thread"
    Private Sub Run()
        While _t.IsAlive
            'Connect Wifi
            Try
                Dim access_points As List(Of SimpleWifi.AccessPoint) = Wifi.GetAccessPoints()
                Dim numAccess As Integer = 0
                Dim boolStatus As Boolean = False
                Dim ssidErr As String = String.Empty

#Region "Check Connect"
                'Check ssid and ping 
                For i As Integer = 0 To access_points.Count - 1
                    If access_points(i).IsConnected Then
                        ssidErr = access_points(i).Name
                        If access_points(i).Name.Contains(ssid) Then
                            Dim intPing As Integer = 0
                            For numPing As Integer = 0 To 10
                                If Not My.Computer.Network.Ping(ip1) And Not My.Computer.Network.Ping(ip2) Then
                                    intPing += 1
                                End If
                                Threading.Thread.Sleep(100)
                            Next
                            If intPing >= 10 Then
                                Wifi.Disconnect()
                                boolStatus = False
                                Exit For
                            End If
                            boolStatus = True
                            Exit For
                        Else
                            boolStatus = False
                        End If
                    End If
                Next
#End Region

#Region "Reconect and record log"
                'Connect wifi and record
                If Not boolStatus Then
                    For i As Integer = 0 To access_points.Count - 1
                        If access_points(i).Name.Contains(ssid) Then
                            numAccess = i
                            Exit For
                        End If
                    Next

                    Dim access_point = access_points(numAccess)
                    Dim auth_access = New SimpleWifi.AuthRequest(access_point)

#Region "Have Password"
                    'If auth_access.IsPasswordRequired Then
                    '    If Not access_point.HasProfile Then
                    '        'TODO: request password, then
                    '        auth_access.Password = "typed password"
                    '    End If
                    'End If
#End Region

                    If Not Wifi.Connect(access_point, auth_access) Then
                        access_point.DeleteProfile()
                        Threading.Thread.Sleep(10000)
                    Else
                        Threading.Thread.Sleep(10000)
                        SendLog("Wifi Reconnect " & ssidErr)
                    End If

                End If
#End Region

            Catch ex As Exception
                Wifi.Disconnect()
                Threading.Thread.Sleep(2500)
            End Try
            'End Connect Wifi

#Region "Check SQL Server"
            'Start SQL Server Auto
            Try
                Dim service As ServiceController = New ServiceController(serviceApp)
                If service.Status = ServiceControllerStatus.Paused Then
                    service.Stop()
                    SendLog("SQL Paused")
                ElseIf service.Status <> ServiceControllerStatus.Running Then
                    service.Start()
                    SendLog("SQL Stop")
                End If
            Catch ex As Exception
                Application.Restart()
            End Try
#End Region

            'End Start SQL Server Auto
            Threading.Thread.Sleep(2500)
        End While
    End Sub

#End Region

#Region "Event"

    Private Sub Form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'CheckIfRunning()
        Try
            My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).SetValue(Application.ProductName, Application.ExecutablePath)
        Catch ex As Exception
            MsgBox("Please run administrator", MsgBoxStyle.Information, "Information")
        End Try

        nameComputer = Environment.MachineName
        'serviceApp = "MSSQL$SQLEXPRESS"
        serviceApp = "MSSQLSERVER"
        'cmd = "Roki-Barcode,192.168.100.38,192.168.100.33".Split(",") 'Command().Split(",")  'ex nameSSID,IP1,IP2
        cmd = "Roki-Client,192.168.100.38,192.168.100.33".Split(",")
        ssid = cmd(0)
        ip1 = cmd(1)
        ip2 = cmd(2)
        _con = objDB.ConnectDB("192.168.100.199", "sa", "SysTem@dmin", "ReconnectWifi")
        _t = New Threading.Thread(AddressOf Run)
        _t.IsBackground = True
        _t.Start()
    End Sub
    Private Sub btHide_Click(sender As Object, e As EventArgs) Handles btHide.Click
        Hide()
    End Sub
    Private Sub ShowToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowToolStripMenuItem.Click
        Show()
    End Sub
    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Dim frm As MsgClose = New MsgClose
        Me.Hide()
        If frm.ShowDialog = DialogResult.OK Then
            Application.Exit()
        End If
    End Sub
    Private Sub niIcon_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles niIcon.MouseDoubleClick
        Show()
    End Sub

#End Region

End Class
