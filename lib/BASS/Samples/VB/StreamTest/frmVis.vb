'*** File Details *****************************************************************
'*   File        : frmVis.vb
'*   Author      : Bernd Niedergesaeee used code from Rick Ratayczak
'*   Sorry I am not a VB guy and this is my first VB code ;-)
'*   But it should be a good starting point
'*
'*   Created On  : 10/31/2005
'* 
'*   Requires    : Bass.Net.dll
'*   Type        : VB.NET Class
'*   Description : Stream Example
'*
'* Before using: copy the bass.dll in your exe directory, e.g. bin!!!
'**********************************************************************************

Imports System.Drawing.Drawing2D
Imports Bass = Un4seen.Bass.Bass

Public Class frmVis
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
	Friend WithEvents PicVis As System.Windows.Forms.PictureBox
	Friend WithEvents Timer1 As System.Windows.Forms.Timer
	Friend WithEvents ContextMenu1 As System.Windows.Forms.ContextMenu
	Friend WithEvents MenuItem1 As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItem2 As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItem3 As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItem4 As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItem5 As System.Windows.Forms.MenuItem
	Friend WithEvents MenuItem6 As System.Windows.Forms.MenuItem
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container
		Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(frmVis))
		Me.PicVis = New System.Windows.Forms.PictureBox
		Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
		Me.ContextMenu1 = New System.Windows.Forms.ContextMenu
		Me.MenuItem1 = New System.Windows.Forms.MenuItem
		Me.MenuItem6 = New System.Windows.Forms.MenuItem
		Me.MenuItem2 = New System.Windows.Forms.MenuItem
		Me.MenuItem3 = New System.Windows.Forms.MenuItem
		Me.MenuItem4 = New System.Windows.Forms.MenuItem
		Me.MenuItem5 = New System.Windows.Forms.MenuItem
		Me.SuspendLayout()
		'
		'PicVis
		'
		Me.PicVis.BackColor = System.Drawing.Color.Black
		Me.PicVis.Location = New System.Drawing.Point(196, 144)
		Me.PicVis.Name = "PicVis"
		Me.PicVis.Size = New System.Drawing.Size(196, 148)
		Me.PicVis.TabIndex = 0
		Me.PicVis.TabStop = False
		'
		'Timer1
		'
		Me.Timer1.Interval = 75
		'
		'ContextMenu1
		'
		Me.ContextMenu1.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.MenuItem1, Me.MenuItem6, Me.MenuItem2, Me.MenuItem3, Me.MenuItem4, Me.MenuItem5})
		'
		'MenuItem1
		'
		Me.MenuItem1.Index = 0
		Me.MenuItem1.Text = "&Always On Top"
		'
		'MenuItem6
		'
		Me.MenuItem6.Index = 1
		Me.MenuItem6.Text = "-"
		'
		'MenuItem2
		'
		Me.MenuItem2.Index = 2
		Me.MenuItem2.Text = "&Normal Size"
		'
		'MenuItem3
		'
		Me.MenuItem3.Index = 3
		Me.MenuItem3.Text = "&Double Size"
		'
		'MenuItem4
		'
		Me.MenuItem4.Index = 4
		Me.MenuItem4.Text = "&Triple Size"
		'
		'MenuItem5
		'
		Me.MenuItem5.Index = 5
		Me.MenuItem5.Text = "&Full Screen"
		'
		'frmVis
		'
		Me.AutoScaleBaseSize = New System.Drawing.Size(8, 19)
		Me.BackColor = System.Drawing.Color.Black
		Me.ClientSize = New System.Drawing.Size(396, 294)
		Me.ContextMenu = Me.ContextMenu1
		Me.Controls.Add(Me.PicVis)
		Me.Font = New System.Drawing.Font("Arial", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow
		Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
		Me.Name = "frmVis"
		Me.Text = "frmVis"
		Me.ResumeLayout(False)

	End Sub

#End Region

#Region " Declarations "

	Private Declare Function GetTickCount Lib "kernel32" () As Integer

	Dim Y As Integer
	Dim RetVal As Long
	Dim VisNumber As Integer
	Dim HasCDG As Boolean = False
	Dim HasPlayed As Boolean = False

	Dim GO As Graphics
	Dim T As Threading.Thread

	Dim StartTime As Integer = 0
	Dim sTime As Integer = 0

    ' 09/24/2004 - Used for throttling graphics to particular FPS
    Dim StartTimeDate As DateTime
    Dim PlaybackCount As Integer = 0
    Private FPS As Integer = 15

#End Region

#Region " Private Property Declarations "

	Private m_ActiveFilename As String
	Private m_ActiveStream As Integer = -1
	Private m_StatusText As String
	Private m_Title As String
	Private m_Artist As String
	Private m_ElapsedTime As Short = 0
	Private m_TotalTime As Short = 0

#End Region

#Region " Public Properties "

	Public Property ActiveFilename() As String
		Get
			Return m_ActiveFilename
		End Get
		Set(ByVal Value As String)
			If m_ActiveFilename = Value Then Exit Property
			If Value = "" Then Exit Property
			m_ActiveFilename = Value

			HasCDG = False

		End Set
	End Property

	Public Property ActiveStream() As Integer
		Get
			Return m_ActiveStream
		End Get
		Set(ByVal Value As Integer)
			If m_ActiveStream <> Value Then
				BumpVisual()
				m_ActiveStream = Value
				Timer1.Enabled = True
			End If
		End Set
	End Property

	Public Property StatusText() As String
		Get
			Return m_StatusText
		End Get
		Set(ByVal Value As String)
			If m_StatusText <> Value Then
				UpdateStatusText()
				m_StatusText = Value
			End If
		End Set
	End Property

	Public Property Title() As String
		Get
			Return m_Title
		End Get
		Set(ByVal Value As String)
			If m_Title <> Value Then
				UpdateStatusText()
				m_Title = Value
			End If
		End Set
	End Property

	Public Property Artist() As String
		Get
			Return m_Artist
		End Get
		Set(ByVal Value As String)
			If m_Artist <> Value Then
				UpdateStatusText()
				m_Artist = Value
			End If

		End Set
	End Property

	Public Property ElapsedTime() As Short
		Get
			Return m_ElapsedTime
		End Get
		Set(ByVal Value As Short)
			m_ElapsedTime = Value
		End Set
	End Property

	Public Property TotalTime() As Short
		Get
			Return m_TotalTime
		End Get
		Set(ByVal Value As Short)
			m_TotalTime = Value
		End Set
	End Property

#End Region

#Region " Form Events "

	Private Sub frmVis_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
		LoadWindowSize("frmVis_")
		MenuItem1.Checked = Me.TopMost

	End Sub

	Private Sub frmVis_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Resize
		If Me.WindowState = FormWindowState.Minimized Then Exit Sub

		PicVis.SuspendLayout()
		PicVis.Visible = True
		PicVis.Height = (Me.ClientSize.Height / 3) * 2
		PicVis.Width = (Me.ClientSize.Width / 3) * 2
		PicVis.Left = Me.ClientSize.Width / 2 - PicVis.Width / 2
		PicVis.Top = 0
		PicVis.SizeMode = PictureBoxSizeMode.StretchImage
		PicVis.ResumeLayout()
		Dim FSize As Single = ((Me.ClientSize.Height - PicVis.Height) / 7)

		Me.Font = New System.Drawing.Font("Tahoma", FSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))


	End Sub

	Private Sub frmVis_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
		SaveWindowSize("frmVis_")

		If Not T Is Nothing Then
			If T.IsAlive Then
				T.Abort()
				T = Nothing
			End If
		End If

	End Sub

	Protected Sub SaveWindowSize(ByVal formName As String)
		If Me.WindowState = FormWindowState.Normal Then

			SaveSetting(Me.ProductName, formName + "Location", "Left", Me.Left.ToString)
			SaveSetting(Me.ProductName, formName + "Location", "Top", Me.Top.ToString)
			SaveSetting(Me.ProductName, formName + "Location", "Height", Me.Height.ToString)
			SaveSetting(Me.ProductName, formName + "Location", "Width", Me.Width.ToString)
		End If
		SaveSetting(Me.ProductName, formName + "Location", "TopMost", Me.TopMost.ToString)
		SaveSetting(Me.ProductName, formName + "Location", "State", CType(Me.WindowState, Integer).ToString)
	End Sub

	Protected Sub LoadWindowSize(ByVal formName As String)
		Me.SuspendLayout()

		Me.Left = CType(GetSetting(Me.ProductName, formName + "Location", "Left", Me.Left.ToString), Integer)
		Me.Top = CType(GetSetting(Me.ProductName, formName + "Location", "Top", Me.Top.ToString), Integer)
		Me.Height = CType(GetSetting(Me.ProductName, formName + "Location", "Height", Me.Height.ToString), Integer)
		Me.Width = CType(GetSetting(Me.ProductName, formName + "Location", "Width", Me.Width.ToString), Integer)
		Me.WindowState = CType(GetSetting(Me.ProductName, formName + "Location", "State", CType(Me.WindowState, Integer).ToString), Integer)
		Me.TopMost = CType(GetSetting(Me.ProductName, formName + "Location", "TopMost", CType(Me.TopMost, Boolean).ToString), Boolean)

		Me.ResumeLayout(False)
	End Sub

	Private Sub frmVis_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.DoubleClick
		BumpVisual()
	End Sub

	Private Sub PicVis_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles PicVis.DoubleClick
		BumpVisual()
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

	Public Sub UpdateStatusText()
		If HasCDG Then
			PicVis.Visible = False
			Exit Sub
		Else
			PicVis.Visible = True
		End If
		Dim FSize As Integer = ((Me.ClientSize.Height - PicVis.Height) / 16)
		Dim G As Graphics = Me.CreateGraphics

		G.Clear(System.Drawing.Color.Black)
		G.DrawString(m_Artist, Me.Font, Brushes.White, FSize, (PicVis.Top + PicVis.Height) + FSize)
		G.DrawString(m_Title, Me.Font, Brushes.White, FSize, (PicVis.Top + PicVis.Height) + Me.Font.Height + (FSize * 2))
		G.DrawString(m_StatusText, Me.Font, Brushes.White, FSize, (PicVis.Top + PicVis.Height) + (Me.Font.Height * 2) + (FSize * 3))
		G.Dispose()

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

		playing = Bass.BASS_ChannelIsActive(m_ActiveStream)
		If playing = 0 Then Exit Sub

        Bass.BASS_ChannelGetData(m_ActiveStream, d, Un4seen.Bass.BASSData.BASS_DATA_FFT1024)

		For X = 0 To PicVis.Width Step 4

			Y = Sqrt(d(X + 1)) * 3 * PicVis.Height			 ' - 4

			If Y > PicVis.Height Then Y = PicVis.Height
			If Y < 0 Then Y = 0
			graph.DrawLine(WhitePen, X + 2, PicVis.Height \ 2, X + 2, PicVis.Height \ 2 - Y)			 ' \ 2)

			graph.DrawLine(PurplePen, X + 2, PicVis.Height \ 2 + Y, X + 2, PicVis.Height \ 2)			  '- Y \ 2)
		Next

		'draw the visual onto the picturebox
		PicVis.Image = bit

        Try
            d = Nothing

            WhitePen.Dispose()
            PurplePen.Dispose()
            graph.Dispose()
        Catch ex As Exception
        Finally
            WhitePen = Nothing
            PurplePen = Nothing
            bit = Nothing
            graph = Nothing
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

        playing = Bass.BASS_ChannelIsActive(m_ActiveStream)
        If playing = 0 Then Exit Sub

        Bass.BASS_ChannelGetData(m_ActiveStream, d, Un4seen.Bass.BASSData.BASS_DATA_FFT1024)

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
            d = Nothing

            RedPen.Dispose()
            YellowPen.Dispose()
            graph.Dispose()

        Catch ex As Exception
        Finally
            RedPen = Nothing
            YellowPen = Nothing
            bit = Nothing
            graph = Nothing
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

        playing = Bass.BASS_ChannelIsActive(m_ActiveStream)
        If playing = 0 Then Exit Sub

        Bass.BASS_ChannelGetData(m_ActiveStream, d, Un4seen.Bass.BASSData.BASS_DATA_FFT1024)

        For X = 0 To PicVis.Width Step 4

            Y = Sqrt(d(X + 1)) * 3 * PicVis.Height - 4
            If Y > PicVis.Height Then Y = PicVis.Height
            graph.DrawEllipse(GreenPen, X, PicVis.Height - Y, 5, 10)    'step4 jumping beans?

            'graph.DrawEllipse(GreenPen, X, PicVis.Height - d(X) * 1000, 5, 10) 'step4 jumping beans?
        Next

        'draw the visual onto the picturebox
        PicVis.Image = bit

        Try
            d = Nothing

            GreenPen.Dispose()
            graph.Dispose()

        Catch ex As Exception
        Finally
            GreenPen = Nothing
            bit = Nothing
            graph = Nothing
        End Try
    End Sub

#End Region

#Region " Timer Events "

	Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
		If m_ActiveStream = -1 Then Exit Sub

        ' Throttle playback to frames per second
        If Now <> StartTimeDate Then
            PlaybackCount = 0
            StartTimeDate = Now
        Else
            PlaybackCount += 1
            If PlaybackCount > FPS Then Exit Sub
        End If


        If VisNumber = 0 Then
            Visual1()
        ElseIf VisNumber = 1 Then
            Visual2()
        ElseIf VisNumber = 2 Then
            Visual3()
        End If
	End Sub

#End Region

#Region " Menu Clicks "

	Private Sub MenuItem1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuItem1.Click
		MenuItem1.Checked = Not MenuItem1.Checked
		Me.TopMost = MenuItem1.Checked
	End Sub

	Private Sub MenuItem2_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuItem2.Click
		Dim H As Integer = Me.Height - Me.ClientSize.Height
		Dim W As Integer = Me.Width - Me.ClientSize.Width

		Me.WindowState = FormWindowState.Normal
		Me.Width = 300 + W
		Me.Height = 216 + H
	End Sub

	Private Sub MenuItem3_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuItem3.Click
		Dim H As Integer = Me.Height - Me.ClientSize.Height
		Dim W As Integer = Me.Width - Me.ClientSize.Width

		Me.WindowState = FormWindowState.Normal
		Me.Width = 600 + W
		Me.Height = 432 + H
	End Sub

	Private Sub MenuItem4_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuItem4.Click
		Dim H As Integer = Me.Height - Me.ClientSize.Height
		Dim W As Integer = Me.Width - Me.ClientSize.Width

		Me.WindowState = FormWindowState.Normal
		Me.Width = 800 + W
		Me.Height = 632 + H

	End Sub

	Private Sub MenuItem5_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuItem5.Click
		Me.WindowState = FormWindowState.Maximized
	End Sub

#End Region

End Class
