Imports System.Data.SqlClient
Imports System.ServiceProcess
Public Class Load

#Region "Variable"

    Dim cmd() As String
    Dim ssid As String
    Dim ip1 As String
    Dim serviceApp As String
    Dim _t As System.Threading.Thread
    Dim _con As SqlClient.SqlConnection
    Dim nameComputer As String
    Dim Resp As String
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
        ShowList(Format(Now, "yyyy-MM-dd HH:mm:ss") & vbTab & Problem & vbTab & "CPU Use " & cCpu & " %" & vbTab & "Ram " & cRam & " Available MBytes")
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
        Dim mach As String = Resp & " : " & nameComputer
        Dim _SQL As String = "INSERT INTO [WiFiLog].[dbo].[ProblemLog] (Machine, ListData, Status) VALUES ('" & mach & "', '" & _msg & "', 3)"
        objDB.ExecuteSQL(_SQL, _con)
    End Sub

#End Region

#Region "Thread"
    Delegate Sub DelUpdateStatusRun(ByVal _numPro As Integer)
    Private Sub UpdateStatusRun(ByVal _numPro As Integer)
        If InvokeRequired Then
            Invoke(New DelUpdateStatusRun(AddressOf UpdateStatusRun), _numPro)
        Else
            pb.Value = _numPro
        End If
    End Sub

    Delegate Sub DelShowList(ByVal _msg As String)
    Private Sub ShowList(ByVal _msg As String)
        If InvokeRequired Then
            Invoke(New DelShowList(AddressOf ShowList), _msg)
        Else
            If _msg = "Clear" Then
                lb.Items.Clear()
            Else
                lb.Items.Add(_msg)
            End If
        End If
    End Sub

    Private Sub Run()
        While _t.IsAlive
            Try
                'Connect Wifi
                UpdateStatusRun(0)
                Try
                    Dim access_points As List(Of SimpleWifi.AccessPoint) = Wifi.GetAccessPoints()
                    Dim numAccess As Integer = 0
                    Dim boolStatus As Boolean = False
                    Dim ssidErr As String = String.Empty

                    'Check ssid and ping 
                    For i As Integer = 0 To access_points.Count - 1
                        If access_points(i).IsConnected Then
                            ssidErr = access_points(i).Name
                            If access_points(i).Name.Contains(ssid) Then
                                Dim intPing As Integer = 0
                                For numPing As Integer = 0 To 10
                                    If Not My.Computer.Network.Ping(ip1) Then
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
                    UpdateStatusRun(50)
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

                        'If auth_access.IsPasswordRequired Then
                        '    If Not access_point.HasProfile Then
                        '        'TODO: request password, then
                        '        auth_access.Password = "typed password"
                        '    End If
                        'End If

                        If Not Wifi.Connect(access_point, auth_access) Then
                            access_point.DeleteProfile()
                            Threading.Thread.Sleep(10000)
                        Else
                            Threading.Thread.Sleep(10000)
                            SendLog("Wifi Reconnect " & ssidErr)
                        End If

                    End If
                    UpdateStatusRun(75)

                Catch ex As Exception
                    Wifi.Disconnect()
                    Threading.Thread.Sleep(2500)
                    ShowList(Format(Now, "yyyy-MM-dd HH:mm:ss") & " " & ex.Message)
                End Try
                'End Connect Wifi

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
                    ShowList(Format(Now, "yyyy-MM-dd HH:mm:ss") & " " & ex.Message)
                    'Application.Restart()
                End Try
                UpdateStatusRun(80)

                Try
                    ClearOffline(nameComputer)
                    CheckDateTime()
                Catch ex As Exception

                End Try
                UpdateStatusRun(100)
                'End Start SQL Server Auto
                Threading.Thread.Sleep(2500)
            Catch ex As Exception

            End Try

        End While
    End Sub

#End Region

#Region "Function Status Program"
    Private Sub ClearOffline(ByVal _Machine As String)
        Dim _SQL As String = "UPDATE [WiFiLog].[dbo].[ListMachine] SET CountOnline = 0 WHERE NameMachine = '" & _Machine & "'"
        objDB.ExecuteSQL(_SQL, _con)
    End Sub

    Private Sub CheckDateTime()
        Dim _SQL As String = "SELECT GETDATE() AS dateNow"
        Dim dtDateNow As DataTable = objDB.SelectSQL(_SQL, _con)
        If dtDateNow.Rows.Count > 0 Then
            Dim DateNow As DateTime = Now
            Dim intDiff As Integer = DateDiff(DateInterval.Minute, DateNow, dtDateNow.Rows(0)("dateNow"))
            If intDiff > 5 Or intDiff < -5 Then
                InsertLog("Date Time Error " & DateNow & " Diff " & dtDateNow.Rows(0)("dateNow"))
            End If
        End If
    End Sub

    Private Function GetResp() As String
        Dim _SQL As String = "SELECT Resp [WiFiLog].[dbo].[ListMachine] WHERE NameMachine = '" & nameComputer & "'"
        Dim dt As DataTable = objDB.SelectSQL(_SQL, _con)
        If dt.Rows.Count > 0 Then
            Return dt.Rows(0)("Resp").ToString
        Else
            Return String.Empty
        End If
    End Function
#End Region

#Region "Event"

    Public Const WM_QUERYENDSESSION As Integer = &H11
    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        If m.Msg = WM_QUERYENDSESSION Then
            Dim _SQL As String = "UPDATE [WiFiLog].[dbo].[ListMachine] SET StatusShutdown = 1 WHERE Resp = '" & Resp & "'"
            objDB.ExecuteSQL(_SQL, _con)
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            lbDateStart.Text = Now
            ShowInTaskbar = False
            WindowState = FormWindowState.Minimized
            'CheckIfRunning()
            Try
                My.Computer.Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).SetValue(Application.ProductName, Application.ExecutablePath)
            Catch ex As Exception
                MsgBox("Please run administrator", MsgBoxStyle.Information, "Information")
            End Try

            nameComputer = Environment.MachineName

            serviceApp = "MSSQL$SQLEXPRESS"
            'serviceApp = "MSSQLSERVER"
            'cmd = "Roki-Barcode,192.168.100.38,192.168.100.33".Split(",") 'Command().Split(",")  'ex nameSSID,IP1,IP2
            cmd = "Roki-Client,192.168.100.38".Split(",")
            ssid = cmd(0)
            ip1 = cmd(1)
            _con = objDB.ConnectDB("192.168.100.199", "sa", "SysTem@dmin", "ReconnectWifi")
            Dim _SQL As String = "UPDATE [WiFiLog].[dbo].[ListMachine] SET StatusShutdown = 0 WHERE Resp = '" & Resp & "'"
            objDB.ExecuteSQL(_SQL, _con)
            Resp = GetResp()
            ' CheckDateTime()
            _t = New Threading.Thread(AddressOf Run)
            _t.IsBackground = True
            _t.Start()
        Catch ex As Exception

        End Try
    End Sub
    Private Sub btHide_Click(sender As Object, e As EventArgs)
        ShowInTaskbar = False
        Hide()
    End Sub
    Private Sub ShowToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowToolStripMenuItem.Click
        Me.ShowInTaskbar = True
        Show()
    End Sub
    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Dim frm As MsgClose = New MsgClose
        Hide()
        If frm.ShowDialog = DialogResult.OK Then
            Application.Exit()
        End If
    End Sub
    Private Sub niIcon_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles niIcon.MouseDoubleClick
        Show()
        ShowInTaskbar = True
    End Sub
    Private Sub Load_Resize(sender As System.Object, e As System.EventArgs) Handles MyBase.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            ShowInTaskbar = False
            Hide()
        End If
    End Sub
    Private Sub Button1_Click(sender As System.Object, e As System.EventArgs) Handles Button1.Click
        Me.Hide()
    End Sub

#End Region

End Class
