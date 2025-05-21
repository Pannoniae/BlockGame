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
using Un4seen.Bass.Misc;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.BassAsio;


namespace Sample
{
	// NOTE: Needs 'bassasio.dll' - copy it to your output directory first!
	//  also needs 'bass.dll' - copy it to your output directory first!
	//  also needs 'bassenc.dll' - copy it to your output directory first!
	//  also needs 'oggenc2.exe' - copy it to your output directory first!
	//  also needs 'lame.exe' - copy it to your output directory first!

	public class AsioRecording : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonStart;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.ProgressBar progressBarLeft;
		private System.Windows.Forms.ProgressBar progressBarRight;
		private System.Windows.Forms.RadioButton radioButtonLAME;
		private System.Windows.Forms.RadioButton radioButtonOGG;
		private System.Windows.Forms.RadioButton radioButtonWAVE;
		private System.Windows.Forms.RadioButton radioButtonAAC;
		private System.Windows.Forms.TrackBar trackBarPan;
		private System.Windows.Forms.TrackBar trackBarVol;
		private System.Windows.Forms.CheckBox checkBoxMonitor;

		public AsioRecording()
		{
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
		}

		#region Vom Windows Form-Designer generierter Code
		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung. 
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonStart = new System.Windows.Forms.Button();
			this.buttonStop = new System.Windows.Forms.Button();
			this.progressBarLeft = new System.Windows.Forms.ProgressBar();
			this.progressBarRight = new System.Windows.Forms.ProgressBar();
			this.radioButtonLAME = new System.Windows.Forms.RadioButton();
			this.radioButtonOGG = new System.Windows.Forms.RadioButton();
			this.radioButtonWAVE = new System.Windows.Forms.RadioButton();
			this.radioButtonAAC = new System.Windows.Forms.RadioButton();
			this.trackBarPan = new System.Windows.Forms.TrackBar();
			this.trackBarVol = new System.Windows.Forms.TrackBar();
			this.checkBoxMonitor = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.trackBarPan)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarVol)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonStart
			// 
			this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonStart.Location = new System.Drawing.Point(12, 14);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.TabIndex = 5;
			this.buttonStart.Text = "START";
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
			// 
			// buttonStop
			// 
			this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonStop.Location = new System.Drawing.Point(195, 14);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.TabIndex = 15;
			this.buttonStop.Text = "STOP";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// progressBarLeft
			// 
			this.progressBarLeft.Location = new System.Drawing.Point(12, 46);
			this.progressBarLeft.Maximum = 32768;
			this.progressBarLeft.Name = "progressBarLeft";
			this.progressBarLeft.Size = new System.Drawing.Size(256, 12);
			this.progressBarLeft.Step = 1;
			this.progressBarLeft.TabIndex = 16;
			// 
			// progressBarRight
			// 
			this.progressBarRight.Location = new System.Drawing.Point(12, 64);
			this.progressBarRight.Maximum = 32768;
			this.progressBarRight.Name = "progressBarRight";
			this.progressBarRight.Size = new System.Drawing.Size(256, 12);
			this.progressBarRight.Step = 1;
			this.progressBarRight.TabIndex = 17;
			// 
			// radioButtonLAME
			// 
			this.radioButtonLAME.Checked = true;
			this.radioButtonLAME.Location = new System.Drawing.Point(20, 94);
			this.radioButtonLAME.Name = "radioButtonLAME";
			this.radioButtonLAME.Size = new System.Drawing.Size(90, 16);
			this.radioButtonLAME.TabIndex = 18;
			this.radioButtonLAME.TabStop = true;
			this.radioButtonLAME.Text = "To LAME";
			// 
			// radioButtonOGG
			// 
			this.radioButtonOGG.Location = new System.Drawing.Point(20, 114);
			this.radioButtonOGG.Name = "radioButtonOGG";
			this.radioButtonOGG.Size = new System.Drawing.Size(90, 16);
			this.radioButtonOGG.TabIndex = 19;
			this.radioButtonOGG.Text = "To OGG";
			// 
			// radioButtonWAVE
			// 
			this.radioButtonWAVE.Location = new System.Drawing.Point(20, 134);
			this.radioButtonWAVE.Name = "radioButtonWAVE";
			this.radioButtonWAVE.Size = new System.Drawing.Size(90, 16);
			this.radioButtonWAVE.TabIndex = 20;
			this.radioButtonWAVE.Text = "To WAVE";
			// 
			// radioButtonAAC
			// 
			this.radioButtonAAC.Location = new System.Drawing.Point(20, 156);
			this.radioButtonAAC.Name = "radioButtonAAC";
			this.radioButtonAAC.Size = new System.Drawing.Size(90, 16);
			this.radioButtonAAC.TabIndex = 21;
			this.radioButtonAAC.Text = "To AAC";
			// 
			// trackBarPan
			// 
			this.trackBarPan.Location = new System.Drawing.Point(124, 146);
			this.trackBarPan.Maximum = 100;
			this.trackBarPan.Minimum = -100;
			this.trackBarPan.Name = "trackBarPan";
			this.trackBarPan.Size = new System.Drawing.Size(104, 45);
			this.trackBarPan.TabIndex = 22;
			this.trackBarPan.TickFrequency = 25;
			this.trackBarPan.ValueChanged += new System.EventHandler(this.trackBarPan_ValueChanged);
			// 
			// trackBarVol
			// 
			this.trackBarVol.Location = new System.Drawing.Point(234, 79);
			this.trackBarVol.Maximum = 100;
			this.trackBarVol.Name = "trackBarVol";
			this.trackBarVol.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarVol.Size = new System.Drawing.Size(45, 104);
			this.trackBarVol.TabIndex = 23;
			this.trackBarVol.TickFrequency = 25;
			this.trackBarVol.Value = 100;
			this.trackBarVol.ValueChanged += new System.EventHandler(this.trackBarVol_ValueChanged);
			// 
			// checkBoxMonitor
			// 
			this.checkBoxMonitor.Location = new System.Drawing.Point(132, 94);
			this.checkBoxMonitor.Name = "checkBoxMonitor";
			this.checkBoxMonitor.Size = new System.Drawing.Size(82, 24);
			this.checkBoxMonitor.TabIndex = 24;
			this.checkBoxMonitor.Text = "Monitor";
			this.checkBoxMonitor.Click += new System.EventHandler(this.checkBoxMonitor_Click);
			// 
			// AsioRecording
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(278, 202);
			this.Controls.Add(this.checkBoxMonitor);
			this.Controls.Add(this.trackBarVol);
			this.Controls.Add(this.trackBarPan);
			this.Controls.Add(this.radioButtonAAC);
			this.Controls.Add(this.radioButtonWAVE);
			this.Controls.Add(this.radioButtonOGG);
			this.Controls.Add(this.radioButtonLAME);
			this.Controls.Add(this.progressBarRight);
			this.Controls.Add(this.progressBarLeft);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.buttonStart);
			this.Name = "AsioRecording";
			this.Text = "ASIO Recording";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.AsioRecording_Closing);
			this.Load += new System.EventHandler(this.AsioRecording_Load);
			((System.ComponentModel.ISupportInitialize)(this.trackBarPan)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarVol)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
			Application.EnableVisualStyles();
			Application.DoEvents();

			Application.Run(new AsioRecording());
		}

		private void AsioRecording_Load(object sender, System.EventArgs e)
		{
			// we don't play via Bass, but only via BassAsio
			// not playing anything via BASS, so don't need an update thread
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);
			// setup BASS - "no sound" device and Init first Asio device
			if ( Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) && 
				 BassAsio.BASS_ASIO_Init(0, BASSASIOInit.BASS_ASIO_DEFAULT) )
			{
				// just here as a demo ;-)
				BASS_ASIO_CHANNELINFO info = new BASS_ASIO_CHANNELINFO();
				int chan = 0;
				while (true)
				{
					if (!BassAsio.BASS_ASIO_ChannelGetInfo(false, chan, info))
						break;
					Console.WriteLine( info.ToString() );
					chan++;
				}
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void AsioRecording_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// close bass
			BassAsio.BASS_ASIO_Free();
			Bass.BASS_Free();
		}

		private BassAsioHandler asio;
		private EncoderLAME lame;
		private EncoderOGG ogg;
		private EncoderWAV wav;
		private EncoderNeroAAC aac;
		private BaseEncoder enc;
		private DSP_PeakLevelMeter plm;
		
		private void buttonStart_Click(object sender, System.EventArgs e)
		{
			asio = new BassAsioHandler(true, 0, 0, 2, BASSASIOFormat.BASS_ASIO_FORMAT_16BIT, 44100);

			// set a mirror on output channel 0 (for monitoring)
			asio.SetMirror(0);
			this.checkBoxMonitor.Checked = true;

            // start ASIO (actually starts recording/playing)
            BASS_ASIO_INFO info = BassAsio.BASS_ASIO_GetInfo();
            bool ok = asio.Start(info.bufmax, 1);
            if (!ok)
                MessageBox.Show("Could not start ASIO : " + BassAsio.BASS_ASIO_ErrorGetCode().ToString());

			// now setup the encoder on the provided asio bass channel (which will be created by the above SetFullDuplex call)
			if (radioButtonLAME.Checked)
			{
				lame = new EncoderLAME(asio.InputChannel);
				lame.InputFile = null;	//STDIN
				lame.OutputFile = "test.mp3";
				lame.LAME_Bitrate = (int)EncoderLAME.BITRATE.kbps_128;
				lame.LAME_Mode = EncoderLAME.LAMEMode.Default;
				lame.LAME_Quality = EncoderLAME.LAMEQuality.Quality;
				lame.Start(null, IntPtr.Zero, false);
				enc = lame;
			}
			else if (radioButtonOGG.Checked)
			{
				ogg = new EncoderOGG(asio.InputChannel);
				ogg.InputFile = null;	//STDIN
				ogg.OutputFile = "test.ogg";
				ogg.OGG_UseQualityMode = true;
				ogg.OGG_Quality = 4.0f;
                ogg.Start(null, IntPtr.Zero, false);
				enc = ogg;
			}
			else if (radioButtonAAC.Checked)
			{
				aac = new EncoderNeroAAC(asio.InputChannel);
				aac.InputFile = null;	//STDIN
				aac.OutputFile = "test.mp4";
				aac.NERO_Bitrate = 64;
                aac.Start(null, IntPtr.Zero, false);
				enc = aac;
			}
			else if (radioButtonWAVE.Checked)
			{
				// writing 16-bit wave file here (even if we use a float asio channel)
				wav = new EncoderWAV(asio.InputChannel);
				wav.InputFile = null;  // STDIN
				wav.OutputFile = "test.wav";
                wav.Start(null, IntPtr.Zero, false);
				enc = wav;
			}

			this.buttonStart.Enabled = false;

			// display the level
			plm = new DSP_PeakLevelMeter(asio.InputChannel, 0);
			plm.UpdateTime = 0.1f; // 100ms
			plm.Notification += new EventHandler(plm_Notification);
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			if (plm != null)
			{
				plm.Notification -= new EventHandler(plm_Notification);
				plm.Stop();
				plm = null;
			}
			this.progressBarLeft.Value = 0;
			this.progressBarRight.Value = 0;

			if (enc != null)
			{
				enc.Stop();  // finish encoding
				enc.Dispose();
				enc = null;
			}
			if (asio != null)
			{
				asio.RemoveMirror();
			}

			asio.Stop();
			this.buttonStart.Enabled = true;
		}

		private void plm_Notification(object sender, EventArgs e)
		{
			if (plm == null)
				return;
			// sender will be the DSP_PeakLevelMeter instance
			this.progressBarLeft.Value = plm.LevelL;
			this.progressBarRight.Value = plm.LevelR;
		}

		private void trackBarVol_ValueChanged(object sender, System.EventArgs e)
		{
			if (asio != null)
				asio.VolumeMirror = (trackBarVol.Value / 100f);
		}

		private void trackBarPan_ValueChanged(object sender, System.EventArgs e)
		{
			if (asio != null)
				asio.PanMirror = (trackBarPan.Value / 100f);
		}

		private void checkBoxMonitor_Click(object sender, System.EventArgs e)
		{
			if (asio != null)
				asio.PauseMirror(!checkBoxMonitor.Checked);
		}
	}


}
