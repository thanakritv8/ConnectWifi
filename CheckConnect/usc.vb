Imports System.Data.SqlClient
Imports objDB = ConnectWifi.objDB
Public Class usc
    Dim _Loc As String
    Dim _t As Threading.Thread
    Dim _dtMach As DataTable
    Dim _con As SqlConnection

    Public WriteOnly Property Loc As String
        Set(value As String)
            _Loc = value
            lbLoc.Text = _Loc
            _con = objDB.ConnectDB("192.168.100.199", "sa", "SysTem@dmin", "ReconnectWifi")
            _dtMach = GetMach()
            _t = New Threading.Thread(AddressOf Run)
            _t.IsBackground = True
            _t.Start()
        End Set
    End Property

    Public WriteOnly Property con As SqlConnection
        Set(value As SqlConnection)
            _con = value
        End Set
    End Property

    Private Function GetMach() As DataTable
        Dim _SQL As String = "SELECT lm.NameMachine, lm.Resp FROM [WiFiLog].[dbo].[ListMachine] AS lm JOIN [WiFiLog].[dbo].[ListLocation] AS ll ON lm.idLoc = ll.SEQ WHERE ll.NameLoc = '" & _Loc & "'"
        Return objDB.SelectSQL(_SQL, _con)
    End Function

    Delegate Sub delUplb(ByVal _msg As String)
    Private Sub Uplb(ByVal _msg As String)
        If InvokeRequired Then
            Invoke(New delUplb(AddressOf Uplb), _msg)
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
                Uplb("Clear")
                For Each _Item In _dtMach.Rows
                    Dim intPing As Integer = 0
                    For numPing As Integer = 0 To 10
                        If Not My.Computer.Network.Ping(_Item("NameMachine")) Then
                            intPing += 1
                        Else
                            Exit For
                        End If
                        Threading.Thread.Sleep(100)
                    Next
                    If intPing >= 10 Then
                        If GetStatusMachine(_Item("NameMachine")) Then
                            Uplb(_Item("NameMachine") & " => Timeout Send Line")
                            InsertLog(_Item("Resp") & " : " & _Item("NameMachine"), "Timeout server to client")
                            UpdateStatus(_Item("NameMachine"), 1)
                        Else
                            Uplb(_Item("NameMachine") & " => Timeout")
                        End If
                        'Update Status Offline
                    Else
                        Uplb(_Item("NameMachine") & " => Success")
                        'Update Status Online
                        If Not GetStatusMachine(_Item("NameMachine")) Then
                            InsertLog(_Item("Resp") & " : " & _Item("NameMachine"), "Online")
                        End If
                        UpdateStatus(_Item("NameMachine"), 0)
                    End If
                    Threading.Thread.Sleep(500)
                Next
                Threading.Thread.Sleep(10000)
            Catch ex As Exception

            End Try

        End While
    End Sub

    Private Function GetStatusMachine(ByVal _Machine As String) As Boolean
        Dim _SQL As String = "SELECT * FROM [WiFiLog].[dbo].[ListMachine] WHERE NameMachine = '" & _Machine & "' AND ([Status] = 0 OR [Status] IS NULL)"
        Dim dt As DataTable = objDB.SelectSQL(_SQL, _con)
        If dt.Rows.Count > 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub UpdateStatus(ByVal _Machine As String, ByVal StatusOnline As Integer)
        Dim _SQL As String = "UPDATE [WiFiLog].[dbo].[ListMachine] SET [Status] = " & StatusOnline & " WHERE NameMachine = '" & _Machine & "'"
        objDB.ExecuteSQL(_SQL, _con)
    End Sub

    Private Sub InsertLog(ByVal nameComputer As String, ByVal _msg As String)
        Dim _SQL As String = "INSERT INTO [WiFiLog].[dbo].[ProblemLog] (Machine, ListData, Status) VALUES ('" & nameComputer & "', '" & _msg & "', 0)"
        objDB.ExecuteSQL(_SQL, _con)
    End Sub
End Class
