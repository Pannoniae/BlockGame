using System;
using Un4seen.Bass;

namespace MyFirstBass
{
    class Program
    {
        static void Main(string[] args)
        {
            // init BASS using the default output device
            if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero) )
            {
                // create a stream channel from a file
                int stream = Bass.BASS_StreamCreateFile("test.mp3", 0L, 0L, BASSFlag.BASS_DEFAULT);
                if (stream != 0)
                {
                    // play the channel
                    Bass.BASS_ChannelPlay(stream, false);
                }
                else
                {
                    // error
                    Console.WriteLine("Stream error: {0}", Bass.BASS_ErrorGetCode());
                }

                // wait for a key
                Console.WriteLine("Press any key to exit");
                Console.ReadKey(false);

                // free the stream
                Bass.BASS_StreamFree(stream);
                // free BASS
                Bass.BASS_Free();
            }
        }
    }
}
