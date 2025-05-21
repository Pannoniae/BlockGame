
#Region " Library Imports "

Imports System.Collections.Generic

Imports Un4seen.Bass
Imports Un4seen.Bass.Misc
Imports Un4seen.Bass.AddOn.Fx
Imports Un4seen.Bass.AddOn.Tags

#End Region

Public Class wfExample
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
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel2 As System.Windows.Forms.Panel
    Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
    Friend WithEvents Panel5 As System.Windows.Forms.Panel
    Friend WithEvents bRemoveCue As System.Windows.Forms.Button
    Friend WithEvents bAddCue As System.Windows.Forms.Button
    Friend WithEvents bStop As System.Windows.Forms.Button
    Friend WithEvents bPause As System.Windows.Forms.Button
    Friend WithEvents bPlay As System.Windows.Forms.Button
    Friend WithEvents bStart As System.Windows.Forms.Button
    Friend WithEvents bClose As System.Windows.Forms.Button
    Friend WithEvents bOpen As System.Windows.Forms.Button
    Friend WithEvents pPlay As System.Windows.Forms.PictureBox
    Friend WithEvents pPause As System.Windows.Forms.PictureBox
    Friend WithEvents Panel4 As System.Windows.Forms.Panel
    Friend WithEvents lPos As System.Windows.Forms.Label
    Friend WithEvents ComboBoxItem1 As System.Windows.Forms.ComboBox
    Friend WithEvents bZoomFit As System.Windows.Forms.Button
    Friend WithEvents bZoomIn As System.Windows.Forms.Button
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(wfExample))
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.Panel5 = New System.Windows.Forms.Panel
        Me.lPos = New System.Windows.Forms.Label
        Me.ComboBoxItem1 = New System.Windows.Forms.ComboBox
        Me.bZoomFit = New System.Windows.Forms.Button
        Me.bZoomIn = New System.Windows.Forms.Button
        Me.Panel4 = New System.Windows.Forms.Panel
        Me.bRemoveCue = New System.Windows.Forms.Button
        Me.bAddCue = New System.Windows.Forms.Button
        Me.Panel2 = New System.Windows.Forms.Panel
        Me.bStop = New System.Windows.Forms.Button
        Me.bPause = New System.Windows.Forms.Button
        Me.bPlay = New System.Windows.Forms.Button
        Me.bStart = New System.Windows.Forms.Button
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.bClose = New System.Windows.Forms.Button
        Me.bOpen = New System.Windows.Forms.Button
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.pPlay = New System.Windows.Forms.PictureBox
        Me.pPause = New System.Windows.Forms.PictureBox
        Me.Panel1.SuspendLayout()
        Me.Panel5.SuspendLayout()
        Me.SuspendLayout()
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.White
        Me.PictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PictureBox1.Location = New System.Drawing.Point(0, 36)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(632, 330)
        Me.PictureBox1.TabIndex = 0
        Me.PictureBox1.TabStop = False
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.Panel5)
        Me.Panel1.Controls.Add(Me.bZoomFit)
        Me.Panel1.Controls.Add(Me.bZoomIn)
        Me.Panel1.Controls.Add(Me.Panel4)
        Me.Panel1.Controls.Add(Me.bRemoveCue)
        Me.Panel1.Controls.Add(Me.bAddCue)
        Me.Panel1.Controls.Add(Me.Panel2)
        Me.Panel1.Controls.Add(Me.bStop)
        Me.Panel1.Controls.Add(Me.bPause)
        Me.Panel1.Controls.Add(Me.bPlay)
        Me.Panel1.Controls.Add(Me.bStart)
        Me.Panel1.Controls.Add(Me.Panel3)
        Me.Panel1.Controls.Add(Me.bClose)
        Me.Panel1.Controls.Add(Me.bOpen)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.Panel1.DockPadding.All = 2
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(632, 36)
        Me.Panel1.TabIndex = 1
        '
        'Panel5
        '
        Me.Panel5.Controls.Add(Me.lPos)
        Me.Panel5.Controls.Add(Me.ComboBoxItem1)
        Me.Panel5.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel5.Location = New System.Drawing.Point(352, 2)
        Me.Panel5.Name = "Panel5"
        Me.Panel5.Size = New System.Drawing.Size(278, 32)
        Me.Panel5.TabIndex = 20
        '
        'lPos
        '
        Me.lPos.AutoSize = True
        Me.lPos.Location = New System.Drawing.Point(180, 8)
        Me.lPos.Name = "lPos"
        Me.lPos.Size = New System.Drawing.Size(46, 16)
        Me.lPos.TabIndex = 1
        Me.lPos.Text = "Stopped"
        '
        'ComboBoxItem1
        '
        Me.ComboBoxItem1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxItem1.ItemHeight = 13
        Me.ComboBoxItem1.Items.AddRange(New Object() {"Intro Start", "Main Part Start", "Outro Start", "Outro End", "Verse Start", "Refrain Start", "Interlude Start", "Theme Start", "Variation Start", "Key Change", "Time Change", "Unwanted Noise", "Sustained Noise", "Sustained Noise End", "Intro End", "Main Part End", "Verse End", "Refrain End", "Theme End", "Profanity", "Profanity End"})
        Me.ComboBoxItem1.Location = New System.Drawing.Point(8, 6)
        Me.ComboBoxItem1.Name = "ComboBoxItem1"
        Me.ComboBoxItem1.Size = New System.Drawing.Size(160, 21)
        Me.ComboBoxItem1.TabIndex = 0
        '
        'bZoomFit
        '
        Me.bZoomFit.Dock = System.Windows.Forms.DockStyle.Left
        Me.bZoomFit.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bZoomFit.Image = CType(resources.GetObject("bZoomFit.Image"), System.Drawing.Image)
        Me.bZoomFit.Location = New System.Drawing.Point(320, 2)
        Me.bZoomFit.Name = "bZoomFit"
        Me.bZoomFit.Size = New System.Drawing.Size(32, 32)
        Me.bZoomFit.TabIndex = 24
        Me.ToolTip1.SetToolTip(Me.bZoomFit, "Zoom Out Full")
        '
        'bZoomIn
        '
        Me.bZoomIn.Dock = System.Windows.Forms.DockStyle.Left
        Me.bZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bZoomIn.Image = CType(resources.GetObject("bZoomIn.Image"), System.Drawing.Image)
        Me.bZoomIn.Location = New System.Drawing.Point(288, 2)
        Me.bZoomIn.Name = "bZoomIn"
        Me.bZoomIn.Size = New System.Drawing.Size(32, 32)
        Me.bZoomIn.TabIndex = 23
        Me.ToolTip1.SetToolTip(Me.bZoomIn, "Zoom In")
        '
        'Panel4
        '
        Me.Panel4.Dock = System.Windows.Forms.DockStyle.Left
        Me.Panel4.Location = New System.Drawing.Point(278, 2)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(10, 32)
        Me.Panel4.TabIndex = 19
        '
        'bRemoveCue
        '
        Me.bRemoveCue.Dock = System.Windows.Forms.DockStyle.Left
        Me.bRemoveCue.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bRemoveCue.Image = CType(resources.GetObject("bRemoveCue.Image"), System.Drawing.Image)
        Me.bRemoveCue.Location = New System.Drawing.Point(246, 2)
        Me.bRemoveCue.Name = "bRemoveCue"
        Me.bRemoveCue.Size = New System.Drawing.Size(32, 32)
        Me.bRemoveCue.TabIndex = 18
        Me.ToolTip1.SetToolTip(Me.bRemoveCue, "Clear All Cue Points")
        '
        'bAddCue
        '
        Me.bAddCue.Dock = System.Windows.Forms.DockStyle.Left
        Me.bAddCue.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bAddCue.Image = CType(resources.GetObject("bAddCue.Image"), System.Drawing.Image)
        Me.bAddCue.Location = New System.Drawing.Point(214, 2)
        Me.bAddCue.Name = "bAddCue"
        Me.bAddCue.Size = New System.Drawing.Size(32, 32)
        Me.bAddCue.TabIndex = 17
        Me.ToolTip1.SetToolTip(Me.bAddCue, "Add Cue Point")
        '
        'Panel2
        '
        Me.Panel2.Dock = System.Windows.Forms.DockStyle.Left
        Me.Panel2.Location = New System.Drawing.Point(204, 2)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(10, 32)
        Me.Panel2.TabIndex = 16
        '
        'bStop
        '
        Me.bStop.Dock = System.Windows.Forms.DockStyle.Left
        Me.bStop.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bStop.Image = CType(resources.GetObject("bStop.Image"), System.Drawing.Image)
        Me.bStop.Location = New System.Drawing.Point(172, 2)
        Me.bStop.Name = "bStop"
        Me.bStop.Size = New System.Drawing.Size(32, 32)
        Me.bStop.TabIndex = 15
        Me.ToolTip1.SetToolTip(Me.bStop, "Stop")
        '
        'bPause
        '
        Me.bPause.Dock = System.Windows.Forms.DockStyle.Left
        Me.bPause.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bPause.Image = CType(resources.GetObject("bPause.Image"), System.Drawing.Image)
        Me.bPause.Location = New System.Drawing.Point(140, 2)
        Me.bPause.Name = "bPause"
        Me.bPause.Size = New System.Drawing.Size(32, 32)
        Me.bPause.TabIndex = 14
        Me.ToolTip1.SetToolTip(Me.bPause, "Play / Pause")
        '
        'bPlay
        '
        Me.bPlay.Dock = System.Windows.Forms.DockStyle.Left
        Me.bPlay.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bPlay.Image = CType(resources.GetObject("bPlay.Image"), System.Drawing.Image)
        Me.bPlay.Location = New System.Drawing.Point(108, 2)
        Me.bPlay.Name = "bPlay"
        Me.bPlay.Size = New System.Drawing.Size(32, 32)
        Me.bPlay.TabIndex = 13
        Me.ToolTip1.SetToolTip(Me.bPlay, "Play")
        '
        'bStart
        '
        Me.bStart.Dock = System.Windows.Forms.DockStyle.Left
        Me.bStart.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bStart.Image = CType(resources.GetObject("bStart.Image"), System.Drawing.Image)
        Me.bStart.Location = New System.Drawing.Point(76, 2)
        Me.bStart.Name = "bStart"
        Me.bStart.Size = New System.Drawing.Size(32, 32)
        Me.bStart.TabIndex = 12
        Me.ToolTip1.SetToolTip(Me.bStart, "Rewind To Beginning")
        '
        'Panel3
        '
        Me.Panel3.Dock = System.Windows.Forms.DockStyle.Left
        Me.Panel3.Location = New System.Drawing.Point(66, 2)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(10, 32)
        Me.Panel3.TabIndex = 11
        '
        'bClose
        '
        Me.bClose.Dock = System.Windows.Forms.DockStyle.Left
        Me.bClose.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bClose.Image = CType(resources.GetObject("bClose.Image"), System.Drawing.Image)
        Me.bClose.Location = New System.Drawing.Point(34, 2)
        Me.bClose.Name = "bClose"
        Me.bClose.Size = New System.Drawing.Size(32, 32)
        Me.bClose.TabIndex = 3
        Me.ToolTip1.SetToolTip(Me.bClose, "Close File")
        '
        'bOpen
        '
        Me.bOpen.Dock = System.Windows.Forms.DockStyle.Left
        Me.bOpen.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.bOpen.Image = CType(resources.GetObject("bOpen.Image"), System.Drawing.Image)
        Me.bOpen.Location = New System.Drawing.Point(2, 2)
        Me.bOpen.Name = "bOpen"
        Me.bOpen.Size = New System.Drawing.Size(32, 32)
        Me.bOpen.TabIndex = 2
        Me.ToolTip1.SetToolTip(Me.bOpen, "Open File")
        '
        'pPlay
        '
        Me.pPlay.Image = CType(resources.GetObject("pPlay.Image"), System.Drawing.Image)
        Me.pPlay.Location = New System.Drawing.Point(112, 44)
        Me.pPlay.Name = "pPlay"
        Me.pPlay.Size = New System.Drawing.Size(24, 24)
        Me.pPlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pPlay.TabIndex = 2
        Me.pPlay.TabStop = False
        Me.pPlay.Visible = False
        '
        'pPause
        '
        Me.pPause.Image = CType(resources.GetObject("pPause.Image"), System.Drawing.Image)
        Me.pPause.Location = New System.Drawing.Point(144, 44)
        Me.pPause.Name = "pPause"
        Me.pPause.Size = New System.Drawing.Size(24, 24)
        Me.pPause.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pPause.TabIndex = 3
        Me.pPause.TabStop = False
        Me.pPause.Visible = False
        '
        'wfExample
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(632, 366)
        Me.Controls.Add(Me.pPause)
        Me.Controls.Add(Me.pPlay)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.Panel1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MaximumSize = New System.Drawing.Size(1024, 400)
        Me.MinimumSize = New System.Drawing.Size(640, 200)
        Me.Name = "wfExample"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Waveform Example"
        Me.Panel1.ResumeLayout(False)
        Me.Panel5.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

#End Region

#Region " Declarations "

    Private wf As WaveForm = Nothing
    Private _zoomStart As Integer = -1
    Private _zoomStartBytes As Long = -1
    Private _zoomEnd As Integer = -1
    Private _zoomDistance As Single = 5.0F
    Private _filename As String = ""
    Private _stream As Integer = 0
    Private _deviceLatencyBytes As Integer = 0
    Private _zoomed As Boolean = False
    Private _deviceLatencyMS As Integer = 0

    Private _updateTimer As BASSTimer = Nothing
    Private _updateInterval As Integer = 50
    Private _tickCounter As Integer = 0
    Private dAmp As New BASS_BFX_DAMP

    Private IsDrawing As Boolean = False

#End Region

#Region " Wave Form Drawing "

    Private Sub SetupWaveForm()
        wf = New WaveForm(Me._filename, New WAVEFORMPROC(AddressOf MyWaveFormCallback), Me)
        wf.FrameResolution = 0.01F
        ' 10ms are nice
        wf.CallbackFrequency = 500
        ' every 5 seconds rendered (500*10ms=5sec)
        wf.ColorBackground = Color.White
        wf.ColorLeft = Color.FromArgb(199, 241, 214)
        wf.ColorRight = Color.FromArgb(199, 241, 214)
        wf.ColorLeftEnvelope = Color.FromArgb(85, 211, 129)
        wf.ColorRightEnvelope = Color.FromArgb(85, 211, 129)

        'wf.ColorBase = Color.FromArgb(128, 128, 255)
        'wf.ColorPeak = Color.FromArgb(0, 0, 255)
        wf.ColorMarker = Color.FromArgb(0, 0, 0)
        wf.DrawMarker = WaveForm.MARKERDRAWTYPE.Line Or WaveForm.MARKERDRAWTYPE.Name Or WaveForm.MARKERDRAWTYPE.NamePositionMiddle
        ' it is important to use the same resolution flags as for the playing stream!!!
        ' our already playing stream is 32-bit float - so this one will also need to be...
        If IO.File.Exists(Me._filename + ".wf") Then
            wf.WaveFormLoadFromFile(Me._filename + ".wf")
            DrawWave()
            'wf.RenderStart(True, BASSFlag.BASS_SAMPLE_FLOAT Or BASSFlag.BASS_STREAM_PRESCAN)
        Else
            wf.RenderStart(True, BASSFlag.BASS_SAMPLE_FLOAT Or BASSFlag.BASS_STREAM_PRESCAN)
        End If

        wf.SyncPlayback(_stream)

        ' from here on we might add markers...just examples here!!!
        'wf.AddMarker("RampUp", Bass.BASS_ChannelSeconds2Bytes(_stream, 17.5F))
        'WF.AddMarker( "Outro", Bass.BASS_ChannelSeconds2Bytes(_stream,100.7f))
        'WF.AddMarker( "Mix", Bass.BASS_ChannelSeconds2Bytes(_stream,130.3f))
        'WF.AddMarker( "End", Bass.BASS_ChannelSeconds2Bytes(_stream,136.9f))
    End Sub

    Private Sub MyWaveFormCallback(ByVal framesDone As Integer, ByVal framesTotal As Integer, ByVal elapsedTime As TimeSpan, ByVal finished As Boolean)
        ' will be called during rendering...
        DrawWave(False)
        If finished Then
            Console.WriteLine("Finished rendering in {0}sec.", elapsedTime)
            ' eg.g use this to save the rendered wave form...

            If wf.FramesRendered = wf.FramesToRender Then
                ' TODO: Save to file
                'If wf.WaveFormSaveToFile(Me._filename + ".wf") = False Then
                '    Console.WriteLine("MyWaveFormCallback: Couldn't Save Wave Form")
                'End If
            End If
            'Threading.Thread.Sleep(50)
            Console.WriteLine("FramesRendered={0} of {1}", wf.FramesRendered, wf.FramesToRender)
            DrawWave(True)
        End If
    End Sub

    Private Sub DrawWave(Optional ByVal highQuality As Boolean = False)
        ' Show the drawing every other time for CPU and speed.
        If highQuality = False Then
            IsDrawing = Not IsDrawing
            If IsDrawing Then Exit Sub
        End If


        If Not wf Is Nothing Then
            Me.PictureBox1.BackgroundImage = wf.CreateBitmap(Me.PictureBox1.Width, Me.PictureBox1.Height, _zoomStart, _zoomEnd, highQuality)

            If wf.IsRenderingInProgress Then Exit Sub

            ' TODO: Save to file again
            'If wf.FramesRendered = wf.FramesToRender Then
            '    If IO.File.Exists(Me._filename + ".wf") = False Then
            '        If wf.WaveFormSaveToFile(Me._filename + ".wf") = False Then
            '            Console.WriteLine("DrawWave: Couldn't Save Wave Form")
            '        End If
            '    End If
            'End If

        Else
            Me.PictureBox1.BackgroundImage = Nothing
        End If

    End Sub

    Private Sub DrawWavePosition(ByVal pos As Long, ByVal len As Long)
        ' Note: we might take the latency of the device into account here!
        ' so we show the position as heard, not played.
        ' That's why we called Bass.Bass_Init with the BASS_DEVICE_LATENCY flag
        ' and then used the BASS_INFO structure to get the latency of the device

        ' Show the drawing every other time.
        'IsDrawing = Not IsDrawing
        'If IsDrawing Then Exit Sub

        Dim PenColor As Color = wf.ColorRightEnvelope

        If len = 0 OrElse pos < 0 Then
            Me.PictureBox1.Image = Nothing
            Return
        End If
        Dim bitmap As Bitmap = Nothing
        Dim g As Graphics = Nothing
        Dim p As Pen = Nothing
        Dim bpp As Double = 0
        Try
            If _zoomed Then
                ' total length doesn't have to be _zoomDistance sec. here
                len = wf.Frame2Bytes(_zoomEnd) - _zoomStartBytes
                Dim scrollOffset As Integer = 10
                ' 10*20ms = 200ms.
                ' if we scroll out the window...(scrollOffset*20ms before the zoom window ends)
                If pos > (_zoomStartBytes + len - scrollOffset * wf.Wave.bpf) Then
                    ' we 'scroll' our zoom with a little offset
                    _zoomStart = wf.Position2Frames(pos - scrollOffset * wf.Wave.bpf)
                    _zoomStartBytes = wf.Frame2Bytes(_zoomStart)
                    _zoomEnd = _zoomStart + wf.Position2Frames(_zoomDistance) - 1
                    If _zoomEnd >= wf.Wave.data.Length Then
                        ' beyond the end, so we zoom from end - _zoomDistance.
                        _zoomEnd = wf.Wave.data.Length - 1
                        _zoomStart = _zoomEnd - wf.Position2Frames(_zoomDistance) + 1
                        If _zoomStart < 0 Then
                            _zoomStart = 0
                        End If
                        _zoomStartBytes = wf.Frame2Bytes(_zoomStart)
                        ' total length doesn't have to be _zoomDistance sec. here
                        len = wf.Frame2Bytes(_zoomEnd) - _zoomStartBytes
                    End If
                    ' get the new wave image for the new zoom window
                    DrawWave(True)
                End If
                ' zoomed: starts with _zoomStartBytes and is _zoomDistance long
                pos -= _zoomStartBytes
                ' offset of the zoomed window
                ' bytes per pixel
                bpp = len / CType(Me.PictureBox1.Width, Double)
            Else
                ' not zoomed: width = length of stream
                ' bytes per pixel
                bpp = len / CType(Me.PictureBox1.Width, Double)
            End If
            ' we take the device latency into account
            ' Not really needed, but if you have a real slow device, you might need the next line
            ' so the BASS_ChannelGetPosition might return a position ahead of what we hear
            pos -= _deviceLatencyBytes
            p = New Pen(PenColor)
            bitmap = New Bitmap(Me.PictureBox1.Width, Me.PictureBox1.Height)
            g = Graphics.FromImage(bitmap)
            g.Clear(Color.White)
            Dim x As Integer = CType(Math.Round(pos / bpp), Integer)
            ' position (x) where to draw the line
            g.DrawLine(p, x, 0, x, Me.PictureBox1.Height - 1)
            bitmap.MakeTransparent(Color.White)
        Catch
            bitmap = Nothing
        Finally
            ' clean up graphics resources
            If Not p Is Nothing Then
                p.Dispose()
            End If
            If Not g Is Nothing Then
                g.Dispose()
            End If
        End Try
        Me.PictureBox1.Image = bitmap
    End Sub

    Private Sub ZoomIn()
        If wf Is Nothing Then
            Return
        End If
        ' WF is not null, so the stream must be playing...
        If _zoomed Then
            ' unzoom...(display the whole wave form)
            _zoomStart = -1
            _zoomStartBytes = -1
            _zoomEnd = -1
        Else
            ' zoom...(display only a partial wave form)
            Dim pos As Long = Bass.BASS_ChannelGetPosition(Me._stream)
            ' calculate the window to display
            _zoomStart = wf.Position2Frames(pos)
            _zoomStartBytes = wf.Frame2Bytes(_zoomStart)
            _zoomEnd = _zoomStart + wf.Position2Frames(_zoomDistance) - 1
            If _zoomEnd >= wf.Wave.data.Length Then
                ' beyond the end, so we zoom from end - _zoomDistance.
                _zoomEnd = wf.Wave.data.Length - 1
                _zoomStart = _zoomEnd - wf.Position2Frames(_zoomDistance) + 1
                _zoomStartBytes = wf.Frame2Bytes(_zoomStart)
            End If
        End If
        _zoomed = Not _zoomed
        ' and display this new wave form
        DrawWave()
    End Sub

    Private Sub UpdateMarkers()
        'TODO: Save marker positions between start/stops

    End Sub

#End Region

#Region " Bass Error Codes "

    Public Enum Bass_ErrorCodes
        ' Fields
        Bass_Error_Already = 14
        Bass_Error_Buflost = 4
        Bass_Error_Create = 33
        Bass_Error_Decode = 38
        Bass_Error_Device = 23
        Bass_Error_Driver = 3
        Bass_Error_Dx = 39
        Bass_Error_Empty = 31
        Bass_Error_Fileform = 41
        Bass_Error_Fileopen = 2
        Bass_Error_Format = 6
        Bass_Error_Freq = 25
        Bass_Error_Handle = 5
        Bass_Error_Illparam = 20
        Bass_Error_Illtype = 19
        Bass_Error_Init = 8
        Bass_Error_Mem = 1
        Bass_Error_No3D = 21
        Bass_Error_Nochan = 18
        Bass_Error_Noeax = 22
        Bass_Error_Nofx = 34
        Bass_Error_Nohw = 29
        Bass_Error_Nonet = 32
        Bass_Error_Nopause = 16
        Bass_Error_Noplay = 24
        Bass_Error_Notaudio = 17
        Bass_Error_Notavail = 37
        Bass_Error_Notfile = 27
        Bass_Error_Ok = 0
        Bass_Error_Playing = 35
        Bass_Error_Position = 7
        Bass_Error_Speaker = 42
        Bass_Error_Start = 9
        Bass_Error_Timeout = 40
        Bass_Error_Unknown = -1
    End Enum

    Public Function BASS_GetErrorDescription(ByRef ErrorCode As Integer) As String
        Dim text1 As String = ""
        Select Case ErrorCode

            Case Bass_ErrorCodes.Bass_Error_Unknown
                Return "Some Other Mystery Error"
            Case Bass_ErrorCodes.Bass_Error_Ok
                Return "All is OK"
            Case Bass_ErrorCodes.Bass_Error_Mem
                Return "Memory Error"
            Case Bass_ErrorCodes.Bass_Error_Fileopen
                Return "Can't Open the File"
            Case Bass_ErrorCodes.Bass_Error_Driver
                Return "Can't Find a Free Sound Driver"
            Case Bass_ErrorCodes.Bass_Error_Buflost
                Return "The Sample Buffer Was Lost - Please Report This!"
            Case Bass_ErrorCodes.Bass_Error_Handle
                Return "Invalid Handle"
            Case Bass_ErrorCodes.Bass_Error_Format
                Return "Unsupported Format"
            Case Bass_ErrorCodes.Bass_Error_Position
                Return "Invalid Playback Position"
            Case Bass_ErrorCodes.Bass_Error_Init
                Return "BASS_Init Has Not Been Successfully Called"
            Case Bass_ErrorCodes.Bass_Error_Start
                Return "BASS_Start Has Not Been Successfully Called"
            Case Bass_ErrorCodes.Bass_Error_Already
                Return "Already Initialized"
            Case Bass_ErrorCodes.Bass_Error_Nopause
                Return "Not Paused"
            Case Bass_ErrorCodes.Bass_Error_Notaudio
                Return "Not An Audio Track"
            Case Bass_ErrorCodes.Bass_Error_Nochan
                Return "Can't Get a Free Channel"
            Case Bass_ErrorCodes.Bass_Error_Illtype
                Return "An Illegal Type Was Specified"
            Case Bass_ErrorCodes.Bass_Error_Illparam
                Return "An Illegal Parameter Was Specified"
            Case Bass_ErrorCodes.Bass_Error_No3D
                Return "No 3D Support"
            Case Bass_ErrorCodes.Bass_Error_Noeax
                Return "No EAX Support"
            Case Bass_ErrorCodes.Bass_Error_Device
                Return "Illegal Device Number"
            Case Bass_ErrorCodes.Bass_Error_Noplay
                Return "Not Playing"
            Case Bass_ErrorCodes.Bass_Error_Freq
                Return "Illegal Sample Rate"
            Case Bass_ErrorCodes.Bass_Error_Notfile
                Return "The Stream is Not a File Stream (WAV/MP3)"
            Case Bass_ErrorCodes.Bass_Error_Nohw
                Return "No Hardware Voices Available"
            Case Bass_ErrorCodes.Bass_Error_Empty
                Return "The MOD music has no sequence data"
            Case Bass_ErrorCodes.Bass_Error_Nonet
                Return "No Internet connection could be opened"
            Case Bass_ErrorCodes.Bass_Error_Create
                Return "Couldn't create the file"
            Case Bass_ErrorCodes.Bass_Error_Nofx
                Return "Effects are not enabled"
            Case Bass_ErrorCodes.Bass_Error_Playing
                Return "The channel is playing"
            Case Bass_ErrorCodes.Bass_Error_Notavail
                Return "The requested data is not available"
            Case Bass_ErrorCodes.Bass_Error_Decode
                Return "The channel is a 'decoding channel'"
        End Select
        Return text1
    End Function

#End Region

#Region " Button Clicks "

    Private Sub bPlay_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bPlay.Click
        Bass.BASS_StreamFree(_stream)

        If _filename = "" Then
            Exit Sub
        End If

        If (System.IO.Path.GetExtension(_filename).ToLower = ".wmv") Or (System.IO.Path.GetExtension(_filename).ToLower = ".wma") Then
            _stream = Un4seen.Bass.AddOn.Wma.BassWma.BASS_WMA_StreamCreateFile(Me._filename, 0, 0, Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT)
        Else
            _stream = Bass.BASS_StreamCreateFile(Me._filename, 0, 0, Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT)
        End If

        If _stream <> 0 Then
            '// used in RMS
            Dim _30mslength As Long = 0
            _30mslength = Bass.BASS_ChannelSeconds2Bytes(_stream, 0.03F) '// 30ms window
            '// latency from milliseconds to bytes
            _deviceLatencyBytes = CType(Bass.BASS_ChannelSeconds2Bytes(_stream, _deviceLatencyMS / 1000.0F), Integer)

            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_FLOATDSP, True)

            Dim fxDamp As Integer = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_BFX_DAMP, 0)
            If fxDamp <> 0 Then
                Bass.BASS_FXGetParameters(fxDamp, dAmp)
                dAmp.Preset_Medium()
                Bass.BASS_FXSetParameters(fxDamp, dAmp)
            End If
        End If


        If _stream <> 0 AndAlso Bass.BASS_ChannelPlay(_stream, False) Then
            'Me.PictureBox1.ImageLayout = ImageLayout.Center
            'Me.PictureBox1.Image = My.Resources.rendering
            SetupWaveForm()
            _updateTimer.[Start]()
            UpdateMarkers()
        Else
            MsgBox(BASS_GetErrorDescription(Bass.BASS_ErrorGetCode()))
        End If
    End Sub

    Private Sub bStop_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bStop.Click
        If _stream <> 0 Then
            If wf.IsRenderingInProgress Then
                wf.RenderStop()
            End If
            Bass.BASS_ChannelStop(_stream)
            Bass.BASS_StreamFree(_stream)
        End If
    End Sub

    Private Sub bPause_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bPause.Click
        If _stream <> 0 Then
            If Bass.BASS_ChannelIsActive(_stream) <> BASSActive.BASS_ACTIVE_PLAYING Then
                Bass.BASS_ChannelPlay(_stream, False)
                _updateTimer.Start()
                bPause.Image = pPause.Image
                'bPause.Image = My.Resources.pause
                DrawWave(True)
            Else
                Bass.BASS_ChannelPause(_stream)
                bPause.Image = pPlay.Image
                'bPause.Image = My.Resources.play
                DrawWave(True)
            End If
        End If

    End Sub

    Private Sub PictureBox1_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PictureBox1.MouseDown
        If wf Is Nothing Then
            Return
        End If
        Console.WriteLine("Rendering Progress: " + wf.IsRenderingInProgress.ToString + " " + wf.IsRendered.ToString)

        Dim pos As Long = wf.GetBytePositionFromX(e.X, Me.PictureBox1.Width, _zoomStart, _zoomEnd)
        Console.WriteLine("Byte Position = {0}; Seconds = {1} - {2}", pos, Bass.BASS_ChannelBytes2Seconds(_stream, pos), e.X)
        Console.WriteLine("Pos: " + pos.ToString)
        If pos > -1 Then
            Bass.BASS_ChannelSetPosition(_stream, pos)

            Dim len As Long = Bass.BASS_ChannelGetLength(_stream)
            ' length in bytes

            lPos.Text = Utils.FixTimespan(Bass.BASS_ChannelBytes2Seconds(_stream, pos), "MMSSF")

            ' update the wave position
            DrawWavePosition(pos, len)

        End If
    End Sub

    Private Sub bZoomIn_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bZoomIn.Click
        ' I don't know why it's making me do this twice
        _zoomed = False
        ZoomIn()
        _zoomed = False
        ZoomIn()
    End Sub

    Private Sub bZoomFit_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bZoomFit.Click
        ' I don't know why it's making me do this twice
        _zoomed = True
        ZoomIn()
        _zoomed = True
        ZoomIn()
    End Sub

    Private Sub bAddCue_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bAddCue.Click
        Dim pos As Long = Bass.BASS_ChannelGetPosition(_stream)
        Dim txt As String = Me.ComboBoxItem1.Text
        If txt.StartsWith("Post In") Then txt = "Post In"
        If txt.StartsWith("Post Out") Then txt = "Post Out"


        wf.AddMarker(txt, pos)
        DrawWave()
        If Me.ComboBoxItem1.SelectedIndex < (Me.ComboBoxItem1.Items.Count - 1) Then
            Me.ComboBoxItem1.SelectedIndex += 1
        End If

    End Sub

    Private Sub bRemoveCue_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bRemoveCue.Click
        wf.ClearAllMarker()
        Me.ComboBoxItem1.SelectedIndex = 0
        _zoomed = True
        ZoomIn()
    End Sub

    Private Sub bStart_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bStart.Click
        If _stream <> 0 Then
            Bass.BASS_ChannelSetPosition(_stream, CType(0.0, Single))
            Dim Pos As Long = Bass.BASS_ChannelGetPosition(_stream)

            Dim len As Long = Bass.BASS_ChannelGetLength(_stream)
            ' length in bytes

            lPos.Text = Utils.FixTimespan(Bass.BASS_ChannelBytes2Seconds(_stream, Pos), "MMSSF")

            DrawWave()
            ' update the wave position
            DrawWavePosition(Pos, len)

            If _zoomed = True Then
                _zoomed = True
                ZoomIn()

                _zoomed = False
                ZoomIn()
            End If
        End If
    End Sub

    Private Sub bOpen_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bOpen.Click
        Dim fileFilter As String = "MP3, WAV, WMA, WMV, FLAC, AAC|*.mp3;*.wav;*.wma;*.wmv;*.fla;*.flac;*.aac;|All Files (*.*)|*.*"
        Dim Filename As String = mFileDialogs.ShowOpenDialog("", fileFilter, 0, Me)

        If Filename = "" Then Exit Sub

        _filename = Filename

        Me.Text = Application.ProductName + " - " + System.IO.Path.GetFileNameWithoutExtension(_filename)

        bPlay.Enabled = True
    End Sub

    Private Sub bClose_Activate(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bClose.Click
        If _stream <> 0 Then
            Bass.BASS_ChannelStop(_stream)
            Bass.BASS_StreamFree(_stream)
        End If
        Me.Text = Application.ProductName
    End Sub

#End Region

#Region " Timer and Misc Code "

    Private Sub timerUpdate_Tick(ByVal sender As Object, ByVal e As System.EventArgs)
        If CType(Bass.BASS_ChannelIsActive(_stream), BASSActive) = BASSActive.BASS_ACTIVE_PLAYING Then
            Dim pos As Long = Bass.BASS_ChannelGetPosition(_stream)
            ' position in bytes
            Dim len As Long = Bass.BASS_ChannelGetLength(_stream)
            ' length in bytes

            _tickCounter += 1

            If _tickCounter = 0 Or _tickCounter = 2 Or _tickCounter = 3 Or _tickCounter = 4 Then
                ' update the wave position
                DrawWavePosition(pos, len)
            End If


            If _tickCounter = 5 Then
                _tickCounter = 0
                lPos.Text = Utils.FixTimespan(Bass.BASS_ChannelBytes2Seconds(_stream, pos), "MMSSF")

            End If

            bPlay.Enabled = False
            bPause.Enabled = True
            bStop.Enabled = True
            bStart.Enabled = True
            bPlay.Visible = False
            bPause.Visible = True
            Me.bZoomFit.Enabled = True
            Me.bZoomIn.Enabled = True
            Me.bAddCue.Enabled = True
            Me.bRemoveCue.Enabled = True
            Me.ComboBoxItem1.Enabled = True

        ElseIf CType(Bass.BASS_ChannelIsActive(_stream), BASSActive) = BASSActive.BASS_ACTIVE_PAUSED Then
            Dim pos As Long = Bass.BASS_ChannelGetPosition(_stream)
            ' position in bytes
            Dim len As Long = Bass.BASS_ChannelGetLength(_stream)
            ' length in bytes
            ' update the wave position
            DrawWavePosition(pos, len)

            bPlay.Enabled = False
            bPause.Enabled = True
            bStop.Enabled = True
            bStart.Enabled = True
            bPlay.Visible = False
            bPause.Visible = True
            Me.bZoomFit.Enabled = True
            Me.bZoomIn.Enabled = True
            Me.bAddCue.Enabled = True
            Me.bRemoveCue.Enabled = True
            Me.ComboBoxItem1.Enabled = True
        Else
            _updateTimer.[Stop]()
            bPlay.Enabled = True
            bPlay.Visible = True
            bPause.Enabled = False
            bPause.Visible = False
            bStop.Enabled = False
            bStart.Enabled = False
            Me.bAddCue.Enabled = False
            Me.bRemoveCue.Enabled = False
            Me.bZoomFit.Enabled = False
            Me.bZoomIn.Enabled = False
            Me.ComboBoxItem1.Enabled = False
            Me.lPos.Text = "Stopped"

            Exit Sub
        End If

    End Sub

    Private Function FixSingleText(ByVal text As String) As String
        Dim index As Integer = text.IndexOf(".")
        If index > -1 Then
            Try
                Return text.Substring(0, index + 5)
            Catch ex As Exception
                Return text.Substring(0, index)
            End Try
        Else
            Return text
        End If
    End Function

#End Region

#Region " Form Events "

    Private Sub wfExample_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ' Disable buttons until used
        bPlay.Enabled = False
        bPlay.Visible = True
        bPause.Enabled = False
        bPause.Visible = False
        bStop.Enabled = False
        bStart.Enabled = False
        Me.bAddCue.Enabled = False
        Me.bRemoveCue.Enabled = False
        Me.bZoomFit.Enabled = False
        Me.bZoomIn.Enabled = False
        Me.ComboBoxItem1.Enabled = False
        Me.lPos.Text = "Click Open to Begin"

        Me.ComboBoxItem1.SelectedIndex = 0

        ' Call the init bass function
        StartBass()
    End Sub

    Private Sub wfExample_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        CloseBass()
    End Sub

    Private Sub PictureBox1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles PictureBox1.Resize
        _zoomed = Not _zoomed
        _zoomed = Not _zoomed
        ZoomIn()
    End Sub

#End Region

#Region " Bass Start/Stop "

    Public Sub StartBass()
		If Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY Or BASSInit.BASS_DEVICE_DEFAULT, Me.Handle) Then
			Dim info As BASS_INFO = New BASS_INFO
			Bass.BASS_GetInfo(info)
			_deviceLatencyMS = info.latency
		Else
			If Bass.BASS_ErrorGetCode <> 0 Then
				MsgBox(BASS_GetErrorDescription(Bass.BASS_ErrorGetCode))
				Exit Sub
			End If
		End If

        Dim loadedPlugIns As Dictionary(Of Integer, String) = Bass.BASS_PluginLoadDirectory(IO.Path.GetDirectoryName(Application.ExecutablePath))
        loadedPlugIns = Nothing

        ' Timer stuff
        _updateTimer = New Un4seen.Bass.BASSTimer(_updateInterval)
        AddHandler _updateTimer.Tick, AddressOf timerUpdate_Tick

    End Sub

    Public Sub CloseBass()
        If _stream <> 0 Then
            Bass.BASS_ChannelStop(_stream)
            Bass.BASS_StreamFree(_stream)
        End If

        Bass.BASS_PluginFree(0)
        Un4seen.Bass.Bass.BASS_Stop()
        Un4seen.Bass.Bass.BASS_Free()
        Un4seen.Bass.Bass.FreeMe()
        If Not wf Is Nothing Then
            If wf.IsRenderingInProgress Then
                wf.RenderStop()
            End If
        End If
        wf = Nothing
    End Sub

#End Region

End Class
