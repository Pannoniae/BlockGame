Imports System
Imports Un4seen.Bass

Namespace MyFirstBass
    Class Program
        Shared Sub Main(ByVal args() As String)
            ' init BASS using the default output device
			If Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero) Then
				' create a stream channel from a file
				Dim stream As Integer = Bass.BASS_StreamCreateFile("test.mp3", 0, 0, BASSFlag.BASS_DEFAULT)
				If stream <> 0 Then
					' play the stream channel
					Bass.BASS_ChannelPlay(stream, False)
				Else
					' error creating the stream
					Console.WriteLine("Stream error: {0}", Bass.BASS_ErrorGetCode())
				End If

				' wait for a key
				Console.WriteLine("Press any key to exit")
				Console.ReadKey(False)

				' free the stream
				Bass.BASS_StreamFree(stream)
				' free BASS
				Bass.BASS_Free()
			End If
        End Sub
    End Class
End Namespace