'*** File Details *****************************************************************
'    File        : Percentage.vb
'    Requires    : None
'    Type        : VB.NET Module
'    Description : Shows the percentage done for a value
'--- Change Log   -----------------------------------------------------------------
'    9/10/2004 by ACTIVEASPMP\Rick - Initial Creation
'**********************************************************************************

Module Percentage

    Public Function PercentageDone(ByVal IntDone As Long, ByVal IntMax As Long) As Integer
        Dim D As Integer
        On Error Resume Next
        D = CType(100 * IntDone / IntMax, Integer)

        Return D
    End Function

End Module
