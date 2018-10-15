Public Class MsgClose
    Private Sub btConfirm_Click(sender As Object, e As EventArgs) Handles btConfirm.Click
        lbError.Text = String.Empty
        lbError.Visible = False
        If txtPassword.Text = "P@ssw0rd" Then
            DialogResult = DialogResult.OK
        ElseIf txtPassword.Text <> String.Empty Then
            lbError.Text = "Password Incorrect"
            lbError.Visible = True
            'MsgBox("Password Incorrect", MsgBoxStyle.Critical, "Alert")
        End If
    End Sub

    Private Sub btCancel_Click(sender As Object, e As EventArgs) Handles btCancel.Click
        Me.Close()
    End Sub

    Private Sub txtPassword_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtPassword.KeyPress
        If Asc(e.KeyChar) = 13 Then
            btConfirm_Click(sender, e)
        End If
    End Sub
End Class