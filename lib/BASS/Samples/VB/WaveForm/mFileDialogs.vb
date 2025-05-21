'*** File Details *****************************************************************
'    File        : mFileDialogs.vb
'    Requires    : None
'    Type        : VB.NET Class
'    Description : Provides Open/Save/Folder Dialogs
'--- Change Log   -----------------------------------------------------------------
'    7/30/2004 by ACTIVEASPMP\Rick - Initial Creation
'    8/11/2004 by ACTIVEASPMP\Rick - Added Folder Browsing Dialog
'**********************************************************************************

Module mFileDialogs

    Public Function ShowOpenDialog(ByVal initialPath As String, ByVal filter As String, ByVal filterIndex As Integer, ByVal frm As Form) As String
        Dim DR As DialogResult = DialogResult.Retry
        Dim ofd1 As New System.Windows.Forms.OpenFileDialog
        Dim RetVal As String = ""

        ofd1.Filter = filter
        ofd1.FilterIndex = filterIndex
        ofd1.InitialDirectory = initialPath

        DR = ofd1.ShowDialog(frm)
        If DR = DialogResult.OK Then
            RetVal = ofd1.FileName
        End If
        ofd1.Dispose()
        ofd1 = Nothing

        Return RetVal
    End Function

    Public Function ShowSaveDialog(ByVal initialPath As String, ByVal filter As String, ByVal filterIndex As Integer, ByVal frm As Form) As String
        Dim DR As DialogResult = DialogResult.Retry
        Dim ofd1 As New System.Windows.Forms.SaveFileDialog
        Dim RetVal As String = ""

        ofd1.Filter = filter
        ofd1.FilterIndex = filterIndex
        ofd1.InitialDirectory = initialPath

        DR = ofd1.ShowDialog(frm)
        If DR = DialogResult.OK Then
            RetVal = ofd1.FileName
        End If
        ofd1.Dispose()
        ofd1 = Nothing

        Return RetVal
    End Function

    Public Function ShowFolderDialog(ByVal initialPath As String, ByVal rootFolder As System.Environment.SpecialFolder, ByVal showNewFolderButton As Boolean, ByVal description As String, ByVal frm As Form) As String
        Dim FBD1 As New FolderBrowserDialog
        Dim DR As DialogResult
        Dim RetVal As String = ""

        FBD1.RootFolder = rootFolder
        FBD1.ShowNewFolderButton = showNewFolderButton
        FBD1.SelectedPath = initialPath
        FBD1.Description = description

        DR = FBD1.ShowDialog(frm)
        If DR = DialogResult.OK Then
            RetVal = FBD1.SelectedPath
        End If
        FBD1.Dispose()
        FBD1 = Nothing

        Return RetVal

    End Function

End Module
