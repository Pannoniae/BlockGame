'*** File Details *****************************************************************
'*   File        : frmCD.vb
'*   Author      : Bernd Niedergesaeee used code from Rick Ratayczak
'*   Sorry I am not a VB guy and this is my first VB code ;-)
'*   But it should be a good starting point
'*
'*   Created On  : 11/01/2005
'* 
'*   Requires    : Bass.Net.dll
'*   Type        : VB.NET Class
'*   Description : CD Example
'*
'* Before using: copy the bass.dll and basscd.dll in your bin directory!!!
'**********************************************************************************

Imports System.Drawing.Drawing2D
Imports Un4seen.Bass
Imports Un4seen.Bass.AddOn.Cd

Public Class frmCD
	Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

	Public Sub New()
		MyBase.New()

		'This call is required by the Windows Form Designer.
		InitializeComponent()

		'Add any initialization after the InitializeComponent() call

	End Sub

	'Form overrides dispose to clean up the component list.
	Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
		If disposing Then
			If Not (components Is Nothing) Then
				components.Dispose()
			End If
		End If
		MyBase.Dispose(disposing)
	End Sub

	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer

	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.  
	'Do not modify it using the code editor.
	Friend WithEvents btnStop As System.Windows.Forms.Button
	Friend WithEvents btnPlay As System.Windows.Forms.Button
	Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
	Friend WithEvents Label1 As System.Windows.Forms.Label
	Friend WithEvents btnBrowse As System.Windows.Forms.Button
	Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
	Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
	Friend WithEvents btnDriveInfo As System.Windows.Forms.Button
	Friend WithEvents Timer1 As System.Windows.Forms.Timer
	Friend WithEvents PicVis As System.Windows.Forms.PictureBox
    Friend WithEvents lAbout As System.Windows.Forms.LinkLabel
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Me.btnStop = New System.Windows.Forms.Button
        Me.btnPlay = New System.Windows.Forms.Button
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.btnBrowse = New System.Windows.Forms.Button
        Me.ListBox1 = New System.Windows.Forms.ListBox
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.btnDriveInfo = New System.Windows.Forms.Button
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.PicVis = New System.Windows.Forms.PictureBox
        Me.lAbout = New System.Windows.Forms.LinkLabel
        Me.SuspendLayout()
        '
        'btnStop
        '
        Me.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnStop.Location = New System.Drawing.Point(184, 72)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(56, 23)
        Me.btnStop.TabIndex = 0
        Me.btnStop.Text = "Stop"
        '
        'btnPlay
        '
        Me.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnPlay.Location = New System.Drawing.Point(120, 72)
        Me.btnPlay.Name = "btnPlay"
        Me.btnPlay.Size = New System.Drawing.Size(56, 23)
        Me.btnPlay.TabIndex = 1
        Me.btnPlay.Text = "Play"
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.Black
        Me.PictureBox1.Location = New System.Drawing.Point(8, 8)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(388, 32)
        Me.PictureBox1.TabIndex = 2
        Me.PictureBox1.TabStop = False
        '
        'Label1
        '
        Me.Label1.BackColor = System.Drawing.Color.Black
        Me.Label1.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.White
        Me.Label1.Location = New System.Drawing.Point(12, 12)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(380, 24)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "Bass .NET CD Example"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'btnBrowse
        '
        Me.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnBrowse.Location = New System.Drawing.Point(248, 72)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(64, 23)
        Me.btnBrowse.TabIndex = 4
        Me.btnBrowse.Text = "&Browse..."
        '
        'ListBox1
        '
        Me.ListBox1.Location = New System.Drawing.Point(8, 44)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(108, 147)
        Me.ListBox1.TabIndex = 6
        '
        'ComboBox1
        '
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.Location = New System.Drawing.Point(120, 44)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(320, 21)
        Me.ComboBox1.TabIndex = 5
        '
        'btnDriveInfo
        '
        Me.btnDriveInfo.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnDriveInfo.Location = New System.Drawing.Point(320, 72)
        Me.btnDriveInfo.Name = "btnDriveInfo"
        Me.btnDriveInfo.Size = New System.Drawing.Size(120, 23)
        Me.btnDriveInfo.TabIndex = 7
        Me.btnDriveInfo.Text = "Drive Information"
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 50
        '
        'PicVis
        '
        Me.PicVis.BackColor = System.Drawing.Color.Black
        Me.PicVis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PicVis.Location = New System.Drawing.Point(120, 104)
        Me.PicVis.Name = "PicVis"
        Me.PicVis.Size = New System.Drawing.Size(320, 88)
        Me.PicVis.TabIndex = 8
        Me.PicVis.TabStop = False
        '
        'lAbout
        '
        Me.lAbout.AutoSize = True
        Me.lAbout.Location = New System.Drawing.Point(404, 16)
        Me.lAbout.Name = "lAbout"
        Me.lAbout.Size = New System.Drawing.Size(33, 17)
        Me.lAbout.TabIndex = 29
        Me.lAbout.TabStop = True
        Me.lAbout.Text = "About"
        '
        'frmCD
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 14)
        Me.ClientSize = New System.Drawing.Size(448, 198)
        Me.Controls.Add(Me.lAbout)
        Me.Controls.Add(Me.PicVis)
        Me.Controls.Add(Me.btnDriveInfo)
        Me.Controls.Add(Me.ListBox1)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.btnBrowse)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.btnPlay)
        Me.Controls.Add(Me.btnStop)
        Me.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "frmCD"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Bass .NET CD Example"
        Me.ResumeLayout(False)

    End Sub

#End Region

#Region " Declarations "

	Dim Y As Integer
	Dim GO As Graphics
	Dim VisNumber As Integer

    ' Our Play Stream
    Dim strmPlay As Integer
    ' Bytes sent
    Dim strmPos As Long = 0
    Dim CDMode As Short = 0

    Dim sFilename As String = ""

#End Region

#Region " Public Properties "

    Private m_sElapsedSeconds As Integer

    Public Property sElapsedSeconds() As Integer
        Get
            Return m_sElapsedSeconds
        End Get
        Set(ByVal Value As Integer)
            m_sElapsedSeconds = Value
        End Set
    End Property

    Private m_sRemainSeconds As Integer

    Public Property sRemainSeconds() As Integer
        Get
            Return m_sRemainSeconds
        End Get
        Set(ByVal Value As Integer)
            m_sRemainSeconds = Value
        End Set
    End Property

    Private m_RippedBytes As Long

    Public Property RippedBytes() As Long
        Get
            Return m_RippedBytes
        End Get
        Set(ByVal Value As Long)
            m_RippedBytes = Value
        End Set
    End Property

    Private m_TotalSeconds As Long

    Public Property TotalSeconds() As Long
        Get
            Return m_TotalSeconds
        End Get
        Set(ByVal Value As Long)
            m_TotalSeconds = Value
        End Set
    End Property

#End Region

#Region " Drives "

    Private Sub LoadCDDrives()
        Dim DL As Char
        Dim DESCR As String

        ComboBox1.Items.Clear()

        Dim drives As BASS_CD_INFO() = BassCd.BASS_CD_GetInfos()
        Dim info As BASS_CD_INFO
        For Each info In drives
            DL = info.DriveLetter
            DESCR = info.ToString()
            ComboBox1.Items.Add(DL + " " + DESCR)
        Next info

        If ComboBox1.Items.Count > 0 Then ComboBox1.SelectedIndex = 0


    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged
        If Not BassCd.BASS_CD_IsReady(ComboBox1.SelectedIndex) Then
            ListBox1.Items.Clear()
            Exit Sub
        End If

        Dim nTracks As Integer = BassCd.BASS_CD_GetTracks(ComboBox1.SelectedIndex)
        GetDriveInfo()
        If nTracks = -1 Then
            ListBox1.Items.Clear()
            Exit Sub
        End If

        ListBox1.Items.Clear()
        For A As Integer = 0 To nTracks - 1
            If A <= 8 Then
                ListBox1.Items.Add("Track 0" + (A + 1).ToString)
            Else
                ListBox1.Items.Add("Track " + (A + 1).ToString)
            End If
        Next


    End Sub

    Private Function GetDriveInfo() As String
        Dim T As New Un4seen.Bass.AddOn.Cd.BASS_CD_INFO
        Dim OutText As String

        BassCd.BASS_CD_GetInfo(ComboBox1.SelectedIndex, T)

        OutText = ComboBox1.Text + vbCrLf + vbCrLf
        OutText += " Cache: " + T.cache.ToString + " KB" + vbCrLf
        OutText += " Max Speed: " + T.maxspeed.ToString + " KB/s or " + (CType(T.maxspeed / 150, Short)).ToString + "x" + vbCrLf
        OutText += " Can Lock: " + CType(T.canlock, Boolean).ToString + vbCrLf
        OutText += " Can Open: " + CType(T.canopen, Boolean).ToString + vbCrLf + vbCrLf

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READSUBCHAN Then
            OutText += " Read Subcodes: True" + vbCrLf
        Else
            OutText += " Read Subcodes: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READSUBCHANDI Then
            OutText += " Read Subcodes Interlaced: True" + vbCrLf
        Else
            OutText += " Read Subcodes Interlaced: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READCDRW Then
            OutText += " Read CD/RW: True" + vbCrLf
        Else
            OutText += " Read CD/RW: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READDVD Then
            OutText += " Read DVD: True" + vbCrLf
        Else
            OutText += " Read DVD: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READDVDR Then
            OutText += " Read DVD-+R: True" + vbCrLf
        Else
            OutText += " Read DVD-+R: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READDVDRAM Then
            OutText += " Read DVD-RAM: True" + vbCrLf
        Else
            OutText += " Read DVD-RAM: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READUPC Then
            OutText += " Read UPC: True" + vbCrLf
        Else
            OutText += " Read UPC: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READANALOG Then
            OutText += " Read Analog: True" + vbCrLf
        Else
            OutText += " Read Analog: False" + vbCrLf
        End If

        If T.rwflags And Un4seen.Bass.AddOn.Cd.BASSCDRWFlags.BASS_CD_RWFLAG_READMULTI Then
            OutText += " Read Multi: True" + vbCrLf
        Else
            OutText += " Read Multi: False" + vbCrLf
        End If

        Return OutText
    End Function

#End Region

#Region " Misc Code "

    Private Function Percentage(ByVal IntDone As Long, ByVal IntMax As Long) As Int16
        Dim D As Int16
        On Error Resume Next
        D = CType(100 * IntDone / IntMax, System.Int16)

        Percentage = D
    End Function

    Public Function BrowseForFile() As String
        Dim ofd1 As New System.Windows.Forms.OpenFileDialog
        Dim Dr As DialogResult

        ofd1.Filter = "CD Audio (*.cda)|*.cda"
        ofd1.DefaultExt = "*.cda"

        Dr = ofd1.ShowDialog(Me)
        If Dr = Windows.Forms.DialogResult.OK Then
            Return ofd1.FileName
        End If

        Return ""
    End Function

    Public Function BrowseForFolder() As String
        Dim bfd1 As New System.Windows.Forms.FolderBrowserDialog
        Dim Dr As DialogResult

        bfd1.ShowNewFolderButton = True
        bfd1.Description = "Select a folder..."

        Dr = bfd1.ShowDialog()
        If Dr = Windows.Forms.DialogResult.OK Then
            Return bfd1.SelectedPath.ToString
        End If
        Return ""
    End Function

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged
        CDMode = 0
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Dim OutText As String = ""
        If Bass.BASS_ChannelIsActive(strmPlay) <> Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING Then
            ListBox1.Enabled = True
            ComboBox1.Enabled = True
            btnPlay.Enabled = True
            btnStop.Enabled = False
            btnBrowse.Enabled = True
            btnDriveInfo.Enabled = True
            If Label1.Text <> "Bass .NET CD Example" Then Label1.Text = "Bass .NET CD Example"

            Exit Sub
        End If

        If btnPlay.Enabled = True Then btnPlay.Enabled = False
        If btnBrowse.Enabled = True Then btnBrowse.Enabled = False
        If btnDriveInfo.Enabled = True Then btnDriveInfo.Enabled = False
        If btnStop.Enabled = False Then btnStop.Enabled = True
        If ListBox1.Enabled = True Then ListBox1.Enabled = False
        If ComboBox1.Enabled = True Then ComboBox1.Enabled = False

        m_sElapsedSeconds = Bass.BASS_ChannelBytes2Seconds(strmPlay, Bass.BASS_ChannelGetPosition(strmPlay))
        m_TotalSeconds = Bass.BASS_ChannelBytes2Seconds(strmPlay, Bass.BASS_ChannelGetLength(strmPlay))
        m_sRemainSeconds = m_TotalSeconds - m_sElapsedSeconds

        OutText = Un4seen.Bass.Utils.FixTimespan(m_sElapsedSeconds, "MMSS") + " -" + Un4seen.Bass.Utils.FixTimespan(m_TotalSeconds - m_sElapsedSeconds, "MMSS") + "  " + Un4seen.Bass.Utils.FixTimespan(m_TotalSeconds, "MMSS")
        If Label1.Text <> OutText Then Label1.Text = OutText

        If GO Is Nothing Then Exit Sub
        If VisNumber = 0 Then
            Visual1()
        ElseIf VisNumber = 1 Then
            Visual2()
        ElseIf VisNumber = 2 Then
            Visual3()
        End If

    End Sub

    Private Sub PicVis_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles PicVis.Paint
        GO = e.Graphics
    End Sub

    Private Sub PicVis_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PicVis.Click
        BumpVisual()
    End Sub

#End Region

#Region " Form Events "

    Private Sub frmCD_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim BB As Boolean
        ' Initializes the bass object
		BB = Bass.BASS_Init(1, 44100, BASSInit.BASS_DEVICE_DEFAULT, Me.Handle)
        If Not BB Then
            MsgBox("BASS-Init failed!")
        End If

        ' Starts digital output
        BB = Bass.BASS_Start()
        If Not BB Then
            MsgBox("Bass could not be started!")
        End If

        LoadCDDrives()

    End Sub

    Private Sub frmCD_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        ' Closes our bass object
        Bass.BASS_Stop()
        Bass.BASS_Free()

    End Sub

#End Region

#Region " Buttons "

    Private Sub btnDriveInfo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDriveInfo.Click
        MsgBox(GetDriveInfo, MsgBoxStyle.Information, "Drive Information")
    End Sub

    Private Sub btnBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBrowse.Click
        Dim Filename As String = BrowseForFile()

        If Filename = "" Then Exit Sub
        CDMode = 1
        sFilename = Filename
    End Sub

    Private Sub btnPlay_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPlay.Click
        Dim DriveNum As Integer = 0
        Dim TrackNum As Integer = 0

        Select Case CDMode
            Case 0    ' BASS_CD_StreamCreate

                DriveNum = ComboBox1.SelectedIndex

                If ListBox1.SelectedItem Is Nothing Then ListBox1.SelectedIndex = 0
                TrackNum = ListBox1.SelectedIndex

                If Not BassCd.BASS_CD_IsReady(DriveNum) Then
                    Dim info As BASS_CD_INFO = BassCd.BASS_CD_GetInfo(DriveNum)
                    MsgBox(" Drive: " + info.DriveLetter + " Is Not Ready", MsgBoxStyle.Critical)
                    Exit Sub
                End If

                strmPlay = BassCd.BASS_CD_StreamCreate(DriveNum, TrackNum, BASSFlag.BASS_STREAM_AUTOFREE)

            Case 1    ' BASS_CD_StreamCreateFile

                If sFilename = "" Then
                    MsgBox("Filename is not valid", MsgBoxStyle.Critical)
                    Exit Sub
                End If

                If Not System.IO.File.Exists(sFilename) Then
                    MsgBox("Filename: " + sFilename + vbCrLf + "Does not exist!", MsgBoxStyle.Critical)
                    Exit Sub
                End If

                strmPlay = BassCd.BASS_CD_StreamCreateFile(sFilename, BASSFlag.BASS_STREAM_AUTOFREE)

        End Select

        If strmPlay = 0 Then
            MsgBox("Stream could not be created!")
            Exit Sub
        End If

        If Not Bass.BASS_ChannelPlay(strmPlay, False) Then
            MsgBox("Stream could not be played!")
        End If

        BumpVisual()
    End Sub

    Private Sub btnStop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStop.Click
        If Not Bass.BASS_ChannelStop(strmPlay) Then
            MsgBox("Stream could not be stopped!")
        End If
    End Sub

    Private Sub lAbout_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles lAbout.LinkClicked
        ' ...
    End Sub

#End Region

#Region " Visuals "

    Public Function Sqrt(ByVal num As Double) As Double
        Sqrt = num ^ 0.5
    End Function

    Public Sub BumpVisual()
        VisNumber += 1
        If VisNumber = 3 Then VisNumber = 0
    End Sub

    Public Sub Visual1()

        Dim bit As Bitmap = New Bitmap(PicVis.Width, PicVis.Height)
        Dim graph As Graphics = Graphics.FromImage(bit)
        Dim WhitePen As New Pen(Color.Azure, 2)
        Dim PurplePen As New Pen(Color.BlueViolet, 2)
        Dim d(1023) As Single
        Dim playing As Integer
        Dim X As Integer
        'Dim Y As Integer

        graph.SmoothingMode = SmoothingMode.AntiAlias

        playing = Bass.BASS_ChannelIsActive(strmPlay)
        If playing = 0 Then Exit Sub

        Bass.BASS_ChannelGetData(strmPlay, d, Un4seen.Bass.BASSData.BASS_DATA_FFT1024)

        For X = 0 To PicVis.Width Step 4

            Y = Sqrt(d(X + 1)) * 3 * PicVis.Height    ' - 4

            If Y > PicVis.Height Then Y = PicVis.Height
            If Y < 0 Then Y = 0
            graph.DrawLine(WhitePen, X + 2, PicVis.Height \ 2, X + 2, PicVis.Height \ 2 - Y)    ' \ 2)

            graph.DrawLine(PurplePen, X + 2, PicVis.Height \ 2 + Y, X + 2, PicVis.Height \ 2)     '- Y \ 2)
        Next

        'draw the visual onto the picturebox
        PicVis.Image = bit

        Try
            graph.Dispose()
            'bit.Dispose()
            WhitePen.Dispose()
            PurplePen.Dispose()

            bit = Nothing
            graph = Nothing
            WhitePen = Nothing
            PurplePen = Nothing
        Catch ex As Exception

        End Try

    End Sub

    Public Sub Visual2()
        Dim bit As Bitmap = New Bitmap(PicVis.Width, PicVis.Height)
        Dim graph As Graphics = Graphics.FromImage(bit)
        Dim RedPen As New Pen(Color.Red, 5)
        Dim YellowPen As New Pen(Color.Yellow, 5)
        Dim d(1023) As Single
        Dim playing As Integer
        Dim X As Integer
        'Dim Y As Integer

        graph.SmoothingMode = SmoothingMode.AntiAlias

        playing = Bass.BASS_ChannelIsActive(strmPlay)
        If playing = 0 Then Exit Sub

        Bass.BASS_ChannelGetData(strmPlay, d, Un4seen.Bass.BASSData.BASS_DATA_FFT1024)

        For X = 0 To PicVis.Width Step 8

            Y = Sqrt(d(X + 1)) * 3 * PicVis.Height    ' - 4

            If Y > PicVis.Height Then Y = PicVis.Height

            graph.DrawLine(RedPen, X + 2, PicVis.Height, X + 2, PicVis.Height - Y)
            'graph.DrawArc(RedPen, X + 2, PicVis.Height, PicVis.Width, PicVis.Height - Y, Y, Y)
            graph.DrawLine(YellowPen, X + 2, PicVis.Height - Y, X + 2, PicVis.Height - Y - 2)
        Next

        'draw the visual onto the picturebox
        PicVis.Image = bit

        Try
            graph.Dispose()
            'bit.Dispose()
            RedPen.Dispose()
            YellowPen.Dispose()

            bit = Nothing
            graph = Nothing
            RedPen = Nothing
            YellowPen = Nothing
        Catch ex As Exception

        End Try
    End Sub

    Public Sub Visual3()
        Dim bit As Bitmap = New Bitmap(PicVis.Width, PicVis.Height)
        Dim graph As Graphics = Graphics.FromImage(bit)
        Dim GreenPen As New Pen(Color.Green, 2)
        Dim d(1023) As Single
        Dim playing As Integer
        Dim X As Integer

        graph.SmoothingMode = SmoothingMode.AntiAlias

        playing = Bass.BASS_ChannelIsActive(strmPlay)
        If playing = 0 Then Exit Sub

        Bass.BASS_ChannelGetData(strmPlay, d, Un4seen.Bass.BASSData.BASS_DATA_FFT1024)

        For X = 0 To PicVis.Width Step 4

            Y = Sqrt(d(X + 1)) * 3 * PicVis.Height - 4
            If Y > PicVis.Height Then Y = PicVis.Height
            graph.DrawEllipse(GreenPen, X, PicVis.Height - Y, 5, 10)    'step4 jumping beans?

            'graph.DrawEllipse(GreenPen, X, PicVis.Height - d(X) * 1000, 5, 10) 'step4 jumping beans?
        Next

        'draw the visual onto the picturebox
        PicVis.Image = bit

        Try
            graph.Dispose()
            'bit.Dispose()
            GreenPen.Dispose()

            bit = Nothing
            graph = Nothing
            GreenPen = Nothing
        Catch ex As Exception

        End Try
    End Sub

#End Region

End Class
