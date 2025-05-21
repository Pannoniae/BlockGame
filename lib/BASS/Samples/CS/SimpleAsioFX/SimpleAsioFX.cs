using System;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.BassAsio;


namespace Sample
{
	// NOTE: Needs 'bassasio.dll' - copy it to your output directory first!
	//       needs 'bass.dll' - copy it to your output directory first!
	//       needs 'bass_fx.dll' - copy it to your output directory first!


	public class SimpleAsioFx : System.Windows.Forms.Form
	{
		// LOCAL VARS
		private int _stream = 0;
		private int _streamFX = 0;
		private ASIOPROC myAsioProc;		// IMPORTANT! declare it here, otherwise the Garbage Collector will remove it!
		private string _fileName = String.Empty;

		//

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button buttonPlay;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.Timer timerUpdate;
		private System.Windows.Forms.Label labelStatus;
		private System.Windows.Forms.ProgressBar progressBarVULeft;
		private System.Windows.Forms.ProgressBar progressBarVURight;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.Button buttonPause;
		private System.Windows.Forms.TrackBar trackBarPosition;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.TrackBar trackBarPitch;
		private System.Windows.Forms.Label label1;
		private System.ComponentModel.IContainer components;

		public SimpleAsioFx()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Vom Windows Form-Designer generierter Code
		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung. 
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.button1 = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.buttonPlay = new System.Windows.Forms.Button();
			this.buttonStop = new System.Windows.Forms.Button();
			this.timerUpdate = new System.Windows.Forms.Timer(this.components);
			this.progressBarVULeft = new System.Windows.Forms.ProgressBar();
			this.labelStatus = new System.Windows.Forms.Label();
			this.buttonPause = new System.Windows.Forms.Button();
			this.progressBarVURight = new System.Windows.Forms.ProgressBar();
			this.labelTime = new System.Windows.Forms.Label();
			this.trackBarPosition = new System.Windows.Forms.TrackBar();
			this.trackBarPitch = new System.Windows.Forms.TrackBar();
			this.button2 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.trackBarPosition)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarPitch)).BeginInit();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.button1.Location = new System.Drawing.Point(10, 10);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(260, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "Select a file to play (e.g. MP3, OGG or WAV)...";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// openFileDialog
			// 
			this.openFileDialog.Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav";
			this.openFileDialog.Title = "Select an audio file to play";
			// 
			// buttonPlay
			// 
			this.buttonPlay.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonPlay.Location = new System.Drawing.Point(10, 46);
			this.buttonPlay.Name = "buttonPlay";
			this.buttonPlay.TabIndex = 5;
			this.buttonPlay.Text = "PLAY";
			this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
			// 
			// buttonStop
			// 
			this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonStop.Location = new System.Drawing.Point(195, 46);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.TabIndex = 15;
			this.buttonStop.Text = "STOP";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// timerUpdate
			// 
			this.timerUpdate.Tick += new System.EventHandler(this.timerUpdate_Tick);
			// 
			// progressBarVULeft
			// 
			this.progressBarVULeft.Location = new System.Drawing.Point(10, 78);
			this.progressBarVULeft.Maximum = 32768;
			this.progressBarVULeft.Name = "progressBarVULeft";
			this.progressBarVULeft.Size = new System.Drawing.Size(260, 12);
			this.progressBarVULeft.Step = 1;
			this.progressBarVULeft.TabIndex = 16;
			// 
			// labelStatus
			// 
			this.labelStatus.Location = new System.Drawing.Point(10, 160);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(260, 14);
			this.labelStatus.TabIndex = 17;
			this.labelStatus.Text = "Status";
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// buttonPause
			// 
			this.buttonPause.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonPause.Location = new System.Drawing.Point(103, 46);
			this.buttonPause.Name = "buttonPause";
			this.buttonPause.TabIndex = 18;
			this.buttonPause.Text = "PAUSE";
			this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
			// 
			// progressBarVURight
			// 
			this.progressBarVURight.Location = new System.Drawing.Point(10, 94);
			this.progressBarVURight.Maximum = 32768;
			this.progressBarVURight.Name = "progressBarVURight";
			this.progressBarVURight.Size = new System.Drawing.Size(260, 12);
			this.progressBarVURight.Step = 1;
			this.progressBarVURight.TabIndex = 19;
			// 
			// labelTime
			// 
			this.labelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelTime.Location = new System.Drawing.Point(10, 114);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(260, 16);
			this.labelTime.TabIndex = 20;
			this.labelTime.Text = "Time";
			this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// trackBarPosition
			// 
			this.trackBarPosition.AutoSize = false;
			this.trackBarPosition.Location = new System.Drawing.Point(10, 134);
			this.trackBarPosition.Maximum = 100;
			this.trackBarPosition.Name = "trackBarPosition";
			this.trackBarPosition.Size = new System.Drawing.Size(260, 18);
			this.trackBarPosition.TabIndex = 21;
			this.trackBarPosition.TickFrequency = 0;
			this.trackBarPosition.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trackBarPosition_MouseUp);
			this.trackBarPosition.MouseDown += new System.Windows.Forms.MouseEventHandler(this.trackBarPosition_MouseDown);
			this.trackBarPosition.Scroll += new System.EventHandler(this.trackBarPosition_Scroll);
			// 
			// trackBarPitch
			// 
			this.trackBarPitch.Location = new System.Drawing.Point(10, 182);
			this.trackBarPitch.Maximum = 50;
			this.trackBarPitch.Minimum = -50;
			this.trackBarPitch.Name = "trackBarPitch";
			this.trackBarPitch.Size = new System.Drawing.Size(110, 45);
			this.trackBarPitch.TabIndex = 69;
			this.trackBarPitch.TickFrequency = 10;
			this.trackBarPitch.Scroll += new System.EventHandler(this.trackBarPitch_Scroll);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(195, 192);
			this.button2.Name = "button2";
			this.button2.TabIndex = 70;
			this.button2.Text = "Freq.";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(116, 186);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(78, 28);
			this.label1.TabIndex = 71;
			this.label1.Text = "<--FXPitch   ASIO Freq-->";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// SimpleAsioFx
			// 
			this.ClientSize = new System.Drawing.Size(282, 232);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.trackBarPitch);
			this.Controls.Add(this.trackBarPosition);
			this.Controls.Add(this.labelTime);
			this.Controls.Add(this.progressBarVURight);
			this.Controls.Add(this.buttonPause);
			this.Controls.Add(this.progressBarVULeft);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.buttonPlay);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.labelStatus);
			this.Controls.Add(this.label1);
			this.Name = "SimpleAsioFx";
			this.Text = "Simple ASIO FX";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.SimpleAsioFx_Closing);
			this.Load += new System.EventHandler(this.SimpleAsioFx_Load);
			((System.ComponentModel.ISupportInitialize)(this.trackBarPosition)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarPitch)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new SimpleAsioFx());
		}

		private void SimpleAsioFx_Load(object sender, System.EventArgs e)
		{
			// we don't play via Bass, but only via BassAsio
			// setup BASS - "no sound" device but 48000 (default for ASIO)
			if ( !Bass.BASS_Init(0, 48000, 0, this.Handle) || !BassAsio.BASS_ASIO_Init(0, BASSASIOInit.BASS_ASIO_DEFAULT) )
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void SimpleAsioFx_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.timerUpdate.Stop();
			// close bass
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
		}

		public int TrackPosition
		{
			// just to lazy...so just convert it to int, since normally no file is above that size
			get { return (int)Bass.BASS_ChannelGetPosition(_streamFX); }
			set { Bass.BASS_ChannelSetPosition(_streamFX, (long)value); }
		}

		private float _originSampleRate = 44100f;
		private float _currentSampleRate = 44100f;
		public float Pitching
		{
			get { return _currentSampleRate; }
			set 
			{ 
				_currentSampleRate = value;
                Bass.BASS_ChannelSetAttribute(_streamFX, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, _currentSampleRate);
			}
		}

		public float Pitch(float pPercent)
		{
			return _originSampleRate * (1 + pPercent/100f);
		}

		// declare all variables outside the callback for increased performance (no work for the garbage Collector)
		private float _maxL = 0f;
		private float _maxR = 0f;
		private int _peakL = 0;
		private int _peakR = 0;
		private int _decLength = 0;
		private BASSActive _status = 0;
		private int a = 0;

		// the main ASIO callback - filling the ASIO buffer with sample data to play...
		private int AsioCallback(bool input, int channel, IntPtr buffer, int length, IntPtr user)
		{
			// Note: 'user' contains the underlying stream channel (_streamFX)

			// get the status of the playing channel...(Note: you can do this also with a SYNCPORC)!
			_status = Bass.BASS_ChannelIsActive(user.ToInt32());
			// now we evaluate the status...
			if ( _status == BASSActive.BASS_ACTIVE_STOPPED )
			{
				// if the playing channel is at the end, we pause ASIO
				// Note: you need to call BASS_ASIO_ChannelReset to resume the ASIO channel!
				BassAsio.BASS_ASIO_ChannelPause(false, channel);
				
				this.BeginInvoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "stopped" });
				return 0;
			}
			else if ( _status == BASSActive.BASS_ACTIVE_PAUSED || _status == BASSActive.BASS_ACTIVE_STALLED )
			{
				this.BeginInvoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "paused" });
				return 0;
			}
			else
			{
				// if playing, we just get the data from our decoding channel
				// Note: a decoding channel will be 'advanced' automatically, 
				// so the next call to the decoding channel will get the next data...
				_decLength = Bass.BASS_ChannelGetData(user.ToInt32(), buffer, length);
				
                // however, it might be the case, that BASS_ChannelGetData returned less data than requested
                if (_decLength < 0)
                    _decLength = 0;
			}

			// from here on we deal with our buffer...calculating the peak VU
			int l4 = _decLength / 4;
			_maxL = 0;
			_maxR = 0;
			unsafe
			{
				float *data = (float*)buffer;
				for (a=0; a < l4; a++)
				{
					// decide on L/R channel
					if (a % 2 == 0)
					{
						// L channel
						if (data[a] > _maxL)
							_maxL = data[a];
					}
					else
					{
						// R channel
						if (data[a] > _maxR)
							_maxR = data[a];
					}
				}
			}
			// limit the maximum peak levels to 0bB = 32768
			// and a float value of 1.0 also represents 0db.
			_peakL = (int)Math.Round(32768f * _maxL);
			if (_peakL > 32768)
				_peakL = 32768;
			_peakR = (int)Math.Round(32768f * _maxR);
			if (_peakR > 32768)
				_peakR = 32768;

            return _decLength;
		}

		private void buttonPlay_Click(object sender, System.EventArgs e)
		{
			BassAsio.BASS_ASIO_Stop();
			BassAsio.BASS_ASIO_ChannelReset(false, -1, BASSASIOReset.BASS_ASIO_RESET_PAUSE | BASSASIOReset.BASS_ASIO_RESET_JOIN );
			Bass.BASS_StreamFree(_streamFX);
			if (_fileName != String.Empty)
			{
				// create the decoding stream
				_stream = Bass.BASS_StreamCreateFile(_fileName, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
				if (_stream != 0)
				{
					// now we create a Tempo channel (again a decoding one)...the actual playing channel
					_streamFX = BassFx.BASS_FX_TempoCreate(_stream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_FX_FREESOURCE | BASSFlag.BASS_SAMPLE_FLOAT);
					if (_streamFX == 0)
					{
                        MessageBox.Show(this, "Can't create FX stream!", Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()));
						return;
					}

					// now setup ASIO
					myAsioProc = new ASIOPROC(AsioCallback);
				
					// get the stream channel info
					BASS_CHANNELINFO info = new BASS_CHANNELINFO();
					Bass.BASS_ChannelGetInfo(_streamFX, info);
					_originSampleRate = (float)info.freq;
					this.button2.Text = _originSampleRate.ToString("0");
					// enable 1st output channel...(0=first)
					BassAsio.BASS_ASIO_ChannelEnable(false, 0, myAsioProc, new IntPtr(_streamFX));
					// and join the next channels to it
					for (int a=1; a < info.chans; a++)
						BassAsio.BASS_ASIO_ChannelJoin(false, a, 0);
					// since we joined the channels, the next commands will applay to all channles joined
					// so setting the values to the first channels changes them all automatically
					// set the source format (float, as the decoding channel is)
					BassAsio.BASS_ASIO_ChannelSetFormat(false, 0, BASSASIOFormat.BASS_ASIO_FORMAT_FLOAT);
					// set the source rate
					BassAsio.BASS_ASIO_ChannelSetRate(false, 0, (double)info.freq);
					// try to set the device rate too (saves resampling)
					BassAsio.BASS_ASIO_SetRate( (double)info.freq );
					// and start playing it...
					// start output using default buffer/latency
					if (!BassAsio.BASS_ASIO_Start(0))
					{
                        MessageBox.Show(this, "Can't start ASIO output", Enum.GetName(typeof(BASSError), BassAsio.BASS_ASIO_ErrorGetCode()));
					}
					else
					{
						this.labelStatus.Text = "playing";
						this.timerUpdate.Start();
					}
				}
			}
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.openFileDialog.FileName = _fileName;
			if ( DialogResult.OK == this.openFileDialog.ShowDialog(this) )
			{
				if ( File.Exists(this.openFileDialog.FileName) )
					_fileName = this.openFileDialog.FileName;
				else
					_fileName = String.Empty;
			}
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			this.timerUpdate.Stop();
			BassAsio.BASS_ASIO_Stop();
			this.labelStatus.Text = "stopped";
			this.progressBarVULeft.Value = 0;
			this.progressBarVURight.Value = 0;
		}

		private int _tickCounter = 0;
		private void timerUpdate_Tick(object sender, System.EventArgs e)
		{
			_tickCounter++;
			if (_tickCounter == 4)
			{
				_tickCounter = 0;
				long len = Bass.BASS_ChannelGetLength(_streamFX); // length in bytes
				long pos = Bass.BASS_ChannelGetPosition(_streamFX); // position in bytes
				double totaltime = Bass.BASS_ChannelBytes2Seconds(_streamFX, len); // the total time length
                double elapsedtime = Bass.BASS_ChannelBytes2Seconds(_streamFX, pos); // the elapsed time length
                double remainingtime = totaltime - elapsedtime;
				this.labelTime.Text = String.Format( "Elapsed: {0:#0.00} - Total: {1:#0.00} - Remain: {2:#0.00}", Utils.FixTimespan(elapsedtime,"MMSS"), Utils.FixTimespan(totaltime,"MMSS"), Utils.FixTimespan(remainingtime,"MMSS"));
				this.Text = String.Format( "CPU: {0:0.00}%", BassAsio.BASS_ASIO_GetCPU() );
				// set the track bar
				if (_trackBarPositionCanDisplay)
				{
					this.trackBarPosition.Maximum = (int)len;
					this.trackBarPosition.Value = (int)pos;
				}
			}

			this.progressBarVULeft.Value = _peakL;
			this.progressBarVURight.Value = _peakR;
		}

		private void buttonPause_Click(object sender, System.EventArgs e)
		{
            if (BassAsio.BASS_ASIO_ChannelIsActive(false, 0) == BASSASIOActive.BASS_ASIO_ACTIVE_PAUSED)
			{
                // channel is paused...so unpause
                BassAsio.BASS_ASIO_ChannelReset(false, 0, BASSASIOReset.BASS_ASIO_RESET_PAUSE);
                this.labelStatus.Text = "playing";
			}
			else
			{
                // channel is playing...so pause
                BassAsio.BASS_ASIO_ChannelPause(false, 0);
                this.labelStatus.Text = "paused";
                this.progressBarVULeft.Value = 0;
                this.progressBarVURight.Value = 0;
			}
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			// first reset the FX tempo...
			trackBarPitch.Value = 0;
			Pitching = Pitch( (float)trackBarPitch.Value );

			// now wind the frequency down...
			int a = (int)BassAsio.BASS_ASIO_ChannelGetRate(false, 0);
			if (a < _originSampleRate)
			{
				BassAsio.BASS_ASIO_ChannelSetRate( false, 0, this._originSampleRate);
				this.button2.Text = _originSampleRate.ToString("0");
			}
			else
			{
				int b = (a-1000)/25;
				for (int r=a; r>=9000; r-=b) 
				{
					BassAsio.BASS_ASIO_ChannelSetRate( false, 0, r);
					this.button2.Text = r.ToString();
					Thread.Sleep(25);
				}
			}
		}

		private void trackBarPitch_Scroll(object sender, System.EventArgs e)
		{
			Pitching = Pitch( (float)trackBarPitch.Value );
		}

		private bool _trackBarPositionCanDisplay = true;

		private void trackBarPosition_Scroll(object sender, System.EventArgs e)
		{
			this.TrackPosition = this.trackBarPosition.Value;
		}

		private void trackBarPosition_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			_trackBarPositionCanDisplay = false;
			int newTrackPosition = (int)(this.trackBarPosition.Maximum * ((double)e.X / this.trackBarPosition.Width));
			this.TrackPosition = newTrackPosition;
		}

		private void trackBarPosition_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			_trackBarPositionCanDisplay = true;
		}

		public delegate void UpdateMessageDelegate(string txt);
		private void UpdateMessageDisplay(string txt)
		{
			this.labelStatus.Text = txt;
		}

	}
    
}
