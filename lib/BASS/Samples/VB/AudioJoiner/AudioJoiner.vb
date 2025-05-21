'*** File Details *****************************************************************
'    File        : MP3Joiner.vb
'    Requires    : None
'    Type        : VB.NET Class
'    Description : Joins audio files back to back into one MP3
'--- Change Log   -----------------------------------------------------------------
'    11/13/2005 by ACTIVEASP01\Rick - Initial Creation
'**********************************************************************************
' Needs:
'       - bass.dll
'       - basswma.dll
'       - bassmix.dll
' COPY TO your bin directory FIRST!!!

#Region " Library Imports "

Imports System.Collections.Generic

Imports Un4seen.Bass

#End Region

Module Module1

#Region " Declarations "

    Private EncodeCommands(1) As String
    Private OutputFile As String = "C:\Temp\Output.mp3"

    Public Enum EncodeType
        OGG = 0
        MP3 = 1
        WMA = 2
    End Enum

    Dim MusicTemp(20000) As Byte
    Dim EncHandle As Integer = 0
    Dim RetVal As Integer = 0
    Dim tmp As String = ""

    Private Chan As Integer = 0
    Private sTime As Double = 0
    Private level As Integer = 0
    Private A As Integer = 0
    Private pos As Long = 0
    Private sLength As Double = 0

#End Region

    Sub Main()

        Dim argv() As String = System.Environment.GetCommandLineArgs
        Dim StreamChan As Integer = 0
        Dim isWMA As Boolean = False

        printf(GetBannerText())

        ' check that BASS 2.2 was loaded
        If Bass.BASS_GetVersion < Utils.MakeLong(0, &H203) Then
            printf("BASS version 2.3 was not loaded\n")
            Return
        End If

        If argv.Length < 4 Then
            printf("usage: " + IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly.Location) + " <Freq> <Channels> <OutputFile> <File> <File> ...\n")
            printf("   or  " + IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly.Location) + " <Freq> <Channels> <OutputFile> <TextFile> ...\n")

            printf("\n")
            printf("<Freq> Frequency, e.g. 44100\n")
            printf("<Channels> Channels, e.g. 1 (mono) or 2 (stereo) \n")
            printf("<OutputFile> Audio file to create \n")
            printf("<File> Audio file(s) to convert \n")
            printf("<TextFile> List of CRLF terminated audio files to convert \n")

            printf("\nInput and output files can be ogg, wma, or mp3\n")

            printf("\nPress Enter to close this application\n")
            Console.Read()

            Return
        End If

        OutputFile = argv(3)

        ' not playing anything, so don't need an update thread
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0)

		If Not (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero)) Then
			printf("error: can't open output device\n")
			printf("\nPress Enter to close this application\n")
			Console.Read()
			Return
		End If

        Dim ht As Dictionary(Of Integer, String) = Bass.BASS_PluginLoadDirectory(IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly.Location))

        Try
            ht.Clear()
        Catch ex As Exception
        Finally
            ht = Nothing
        End Try

        printf("Output File: " + OutputFile + "\n")

        If IO.File.Exists(OutputFile) Then
            Try
                IO.File.Delete(OutputFile)
            Catch ex As Exception

            End Try
        End If

        Chan = AddOn.Mix.BassMix.BASS_Mixer_StreamCreate(atol(argv(1)), atol(argv(2)), BASSFlag.BASS_STREAM_DECODE)
        If Chan = 0 Then
            printf("error: can't create mixer output\n")
            printf("\nPress Enter to close this application\n")
            Console.Read()
            Bass.BASS_Free()
            Return
        End If

        printf("\n")

        'Encoder = EncodeType.MP3
        EncodeCommands(EncodeType.OGG) = "oggenc.exe -b 128 -o """ + OutputFile + """ -"
        EncodeCommands(EncodeType.MP3) = "lame.exe -b 128 - """ + OutputFile + """"

        If OutputFile.ToLower(System.Globalization.CultureInfo.CurrentCulture).EndsWith(".wma") Then
            isWMA = True
        Else
            isWMA = False
        End If

        If isWMA = False Then
            If OutputFile.ToLower(System.Globalization.CultureInfo.CurrentCulture).EndsWith(".mp3") Then
                EncHandle = AddOn.Enc.BassEnc.BASS_Encode_Start(Chan, EncodeCommands(1), AddOn.Enc.BASSEncode.BASS_ENCODE_FP_16BIT, Nothing, IntPtr.Zero)
            ElseIf OutputFile.ToLower(System.Globalization.CultureInfo.CurrentCulture).EndsWith(".ogg") Then
                EncHandle = AddOn.Enc.BassEnc.BASS_Encode_Start(Chan, EncodeCommands(0), AddOn.Enc.BASSEncode.BASS_ENCODE_FP_16BIT, Nothing, IntPtr.Zero)
            End If

            If EncHandle = 0 Then
                printf(BASS_GetErrorDescription(Bass.BASS_ErrorGetCode) + "\n")
                printf("\nPress Enter to close this application\n")
                Console.Read()
                Bass.BASS_Free()
            End If
        Else

            Dim T() As Integer = AddOn.Wma.BassWma.BASS_WMA_EncodeGetRates(CType(argv(1), Integer), CType(argv(2), Integer), AddOn.Wma.BASSWMAEncode.BASS_WMA_ENCODE_RATES_CBR)
            'For A As Integer = 0 To T.Length - 1
            '    MsgBox(T(A).ToString)
            'Next

            EncHandle = AddOn.Wma.BassWma.BASS_WMA_EncodeOpenFile(CType(argv(1), Integer), CType(argv(2), Integer), 0, 128, OutputFile)
        End If

        argv = Split(GetListOfFiles(), "|")

        For A = 0 To argv.Length - 1
            ' insert channels into mixer
            If IO.File.Exists(argv(A)) Then
                StreamChan = Bass.BASS_StreamCreateFile(argv(A), 0, 0, BASSFlag.BASS_STREAM_DECODE Or BASSFlag.BASS_STREAM_PRESCAN)
                sLength = Bass.BASS_ChannelBytes2Seconds(StreamChan, Bass.BASS_ChannelGetLength(StreamChan))

                If AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(Chan, StreamChan, BASSFlag.BASS_STREAM_DECODE Or BASSFlag.BASS_MIXER_CHAN_DOWNMIX) Then
                    printf(argv(A) + "\n")

                    Do While Bass.BASS_ChannelIsActive(Chan) = Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING
                        ' display some stuff and wait a bit

                        RetVal = Bass.BASS_ChannelGetData(Chan, MusicTemp, 20000)

                        If isWMA Then
                            If AddOn.Wma.BassWma.BASS_WMA_EncodeWrite(EncHandle, MusicTemp, RetVal) = False Then
                                Exit Do
                            End If
                        End If

                        pos = Bass.BASS_ChannelGetPosition(Chan)
                        sTime = Bass.BASS_ChannelBytes2Seconds(Chan, pos)

                        If tmp <> Utils.FixTimespan(sTime, "MMSS") Then
                            tmp = Utils.FixTimespan(sTime, "MMSS")
                        End If

                        If tmp <> Percentage.PercentageDone(pos, CType(sLength, Long)).ToString Then
                            tmp = Percentage.PercentageDone(pos, CType(sLength, Long)).ToString
                            If Percentage.PercentageDone(pos, CType(sLength, Long)).ToString.Trim.EndsWith("0") Then
                                printf("-")
                            End If
                        End If

                        System.Threading.Thread.Sleep(1)
                        If RetVal < 20000 Then Exit Do
                    Loop
                    printf("\n")

                End If
            End If
        Next
        printf("\n")

        If isWMA = False Then
            AddOn.Enc.BassEnc.BASS_Encode_Stop(Chan)
        Else
            AddOn.Wma.BassWma.BASS_WMA_EncodeClose(EncHandle)
        End If


        Bass.BASS_StreamFree(Chan)

        Bass.BASS_Stop()
        Bass.BASS_Free()

        printf("Finished.\n")
        'printf("Press Enter to close this application\n")
        'Console.Read()

        Return
    End Sub

#Region " Helper Functions "

    Private Sub printf(ByVal text As String)
        Console.Write(text.Replace("\n", vbCrLf))
    End Sub

    Private Function atol(ByVal text As String) As Integer
        Return CType(text, Integer)
    End Function

    Private Function GetBannerText() As String
        Dim tmp As String = ""
        Dim VR As New VersionResource

        VR.AssemblyName = System.Reflection.Assembly.GetExecutingAssembly.Location
        VR.Refresh()

        tmp = VR.ProductName + " v" + VR.ProductVersion + vbCrLf + VR.Copyright + vbCrLf + vbCrLf
        VR = Nothing

        Return tmp
    End Function

    Private Function GetListOfFiles() As String
        Dim argv() As String = System.Environment.GetCommandLineArgs
        Dim tmp As String = ""

        If argv(4).ToLower(System.Globalization.CultureInfo.CurrentCulture).EndsWith(".txt") Then
            tmp = TextFiles.ReadTextFile(argv(4), "")
            tmp = tmp.Replace(vbCrLf, vbLf).Replace(vbLf, "|")
        Else
            For A As Integer = 4 To argv.Length - 1
                tmp += argv(A) + "|"
            Next
        End If
        Return tmp
    End Function

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
        Return "Unknown Error"
    End Function

#End Region

End Module
