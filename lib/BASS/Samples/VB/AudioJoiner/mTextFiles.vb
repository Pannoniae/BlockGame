'*** File Details *****************************************************************
'    File        : TextFiles.vb
'    Requires    : None
'    Type        : VB.NET Class
'    Description : Reads and writes text files
'--- Change Log   -----------------------------------------------------------------
'    7/5/2004 by ACTIVEASPMP\Rick - Initial Creation
'**********************************************************************************

Module TextFiles

    Public Sub WriteTextFile(ByVal filename As String, ByVal inData As String)
        Dim T As System.IO.StreamWriter
        If System.IO.File.Exists(filename) Then
            Kill(filename)
        End If

        T = New System.IO.StreamWriter(filename)
        T.AutoFlush = True
        T.Write(inData)
        T.Close()
        T = Nothing
    End Sub

    Public Function ReadTextFile(ByVal filename As String, ByVal defaultValue As String) As String
        Dim T As System.IO.StreamReader
        Dim retVal As String
        If System.IO.File.Exists(filename) Then
            T = New System.IO.StreamReader(filename)
            retVal = T.ReadToEnd()
            T.Close()
            T = Nothing
            Return retVal
        Else
            Return defaultValue
        End If
    End Function

End Module
