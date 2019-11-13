Imports System.Data.SqlClient
Imports System.IO
Imports System.Net
Imports System.Text
Imports ConnectWifi
Public Class Load
    Dim usc(0) As usc
    Dim _con As SqlConnection
    Dim _t As Threading.Thread

    Private Sub CheckPMS()
        Dim _SQL As String = "SELECT lm.NameMachine, lm.Resp, pl.WorkCenterIP, pl.Code FROM [WiFiLog].[dbo].[ListMachine] as lm right join [WMS].[WMS_ROKI].[dbo].[ProductionLine] as pl on lm.NameMachine = pl.WorkCenterIP where pl.WorkCenterIP is not null AND pl.Code not like '%F1' AND (lm.Resp <> pl.Code OR lm.Resp IS NULL) AND pl.PMSStatusId = 2"
        Dim dt As DataTable = objDB.SelectSQL(_SQL, _con)
        If dt.Rows.Count > 0 Then
            For Each _Item In dt.Rows
                If IsDBNull(_Item("NameMachine")) Then
                    _SQL = "INSERT INTO [WiFiLog].[dbo].[ListMachine] ([NameMachine], [Resp], [idLoc], [Status], [CountOnline], [StatusShutdown]) VALUES ('" & _Item("WorkCenterIP") & "', '" & _Item("Code") & "', 4, 0, 0, 0)"
                    objDB.ExecuteSQL(_SQL, _con)
                Else
                    Dim dr() As DataRow = dt.Select("NameMachine = '" & _Item("NameMachine") & "'")
                    _SQL = "UPDATE [WiFiLog].[dbo].[ListMachine] SET Resp = '" & dr(0)("Code") & "' WHERE NameMachine = '" & _Item("NameMachine") & "'"
                End If
            Next
        End If
    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'SendLine("Bu8OtlIW8Jky45ej5jVXFQ3NNrCuMLpOJ7NDgNcjYYZ", "2018-11-13 12:49:44	SQL Stop	CPU Use 27.5 	Ram 2302 Available MBytes => " & "STA028")
        _con = objDB.ConnectDB("192.168.100.199", "sa", "SysTem@dmin", "ReconnectWifi")
        CheckPMS()
        Dim dtLoc As DataTable = GetLocation()
        ReDim usc(dtLoc.Rows.Count - 1)
        Dim interval As Integer = 5
        Dim _rowUsc As Integer = 7
        Dim x As Integer = interval
        Dim y As Integer = interval
        For i As Integer = 0 To usc.Length - 1
            usc(i) = New usc
            usc(i).Location = New Point(x, y)
            'usc(i).con = _con
            'usc(i).Loc = dtLoc.Rows(i)("MRP")
            usc(i).Loc = dtLoc.Rows(i)("NameLoc")
            x += usc(i).Size.Width + interval
            If i = _rowUsc Then
                _rowUsc = (_rowUsc * 2) + 1
                x = interval
                y += usc(i).Size.Height + interval
            End If
            Me.Controls.Add(usc(i))
        Next
        _t = New Threading.Thread(AddressOf Run)
        _t.IsBackground = True
        _t.Start()

    End Sub
    Private Function GetLocation() As DataTable
        Dim _SQL As String = "SELECT * FROM [WiFiLog].[dbo].[ListLocation]"
        'Dim _SQL As String = "SELECT DISTINCT(MRP) FROM [WMS].[WMS_ROKI].[dbo].[ProductionLine] WHERE WorkCenterIP is not null"
        Return objDB.SelectSQL(_SQL, _con)
    End Function
    Private Sub Run()
        While _t.IsAlive
            Try
                'Send Msg to line
                Dim dtMsg As DataTable = GetMsg()
                For Each _Item In dtMsg.Rows
                    Dim _Who As String = Command()
                    SendLine(_Who, _Item("ListData") & " => " & _Item("Machine"))
                    UpdateMSg(_Item("SEQ"))
                Next
                Threading.Thread.Sleep(1000)
                'Count push Online
                Dim dtMach As DataTable = GetMach()
                For Each _Item In dtMach.Rows
                    UpdateCountOnline(_Item("NameMachine"))
                Next
            Catch ex As Exception

            End Try

        End While
    End Sub

    Private Function GetOffline(ByVal _Min As Integer) As DataTable
        Dim _SQL As String = "SELECT * FROM [WiFiLog].[dbo].[ListMachine] WHERE CountOnline > " & _Min
        Return objDB.SelectSQL(_SQL, _con)
    End Function

    Private Function GetMach()
        Dim _SQL As String = "SELECT * FROM [WiFiLog].[dbo].[ListMachine]"
        Return objDB.SelectSQL(_SQL, _con)
    End Function

    Private Sub UpdateCountOnline(ByVal _Machine As String)
        Dim _SQL As String = "UPDATE [WiFiLog].[dbo].[ListMachine] SET CountOnline = CountOnline + 1 WHERE NameMachine = '" & _Machine & "' AND idLoc <> 3"
        objDB.ExecuteSQL(_SQL, _con)
    End Sub

    Private Function GetMsg() As DataTable
        Dim _SQL As String = "SELECT * FROM [WiFiLog].[dbo].[ProblemLog] WHERE [Status] = 0"
        Return objDB.SelectSQL(_SQL, _con)
    End Function

    Private Sub UpdateMSg(ByVal SEQ As Integer)
        Dim _SQL As String = "UPDATE [WiFiLog].[dbo].[ProblemLog] SET Status = 1 WHERE SEQ = " & SEQ
        objDB.ExecuteSQL(_SQL, _con)
    End Sub
    Private Sub SendLine(ByVal _Who As String, ByVal _Msg As String)
        Try
            Cursor.Current = Cursors.WaitCursor
            System.Net.ServicePointManager.Expect100Continue = False
            Dim request = DirectCast(WebRequest.Create("https://notify-api.line.me/api/notify”), HttpWebRequest)
            Dim postData = String.Format("message={0}", _Msg.Replace("%", ""))
            Dim data = Encoding.UTF8.GetBytes(postData)
            request.Method = "POST"
            request.ContentType = "application/x-www-form-urlencoded"
            request.ContentLength = data.Length
            request.Headers.Add("Authorization", "Bearer " & _Who)
            request.AllowWriteStreamBuffering = True
            request.KeepAlive = False
            request.Credentials = CredentialCache.DefaultCredentials
            Using stream = request.GetRequestStream()
                stream.Write(data, 0, data.Length)
            End Using
            Dim response = DirectCast(request.GetResponse(), HttpWebResponse)
            Dim responseString = New StreamReader(response.GetResponseStream()).ReadToEnd()
        Catch ex As Exception
            'MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1)
        Finally
            Cursor.Current = Cursors.Default
        End Try
    End Sub
End Class
