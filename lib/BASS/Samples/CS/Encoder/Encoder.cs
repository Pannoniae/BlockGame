using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Wma;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass.Misc;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
	//       needs 'basswma.dll' - copy it to your output directory first!
	//       needs 'bassmix.dll' - copy it to your output directory first!
	//       needs 'lame.exe' - copy it to your output directory first!
	//              e.g. download from www.rarewares.org

	public class Encoder : System.Windows.Forms.Form
	{
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxRate;
		private System.Windows.Forms.Button buttonEncode;
		private System.Windows.Forms.Button buttonPlaySource;
		private System.Windows.Forms.TrackBar trackBarCrossFader;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.Button buttonStartRec;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonStopRec;
		private System.Windows.Forms.Label labelRec;
		private System.Windows.Forms.ProgressBar progressBarRecL;
		private System.Windows.Forms.ProgressBar progressBarRecR;
		private System.Windows.Forms.PictureBox pictureBoxLiveWave;
		private System.Windows.Forms.CheckBox checkBoxMonitor;
		private System.ComponentModel.IContainer components;

		public Encoder()
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
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.button1 = new System.Windows.Forms.Button();
			this.buttonEncode = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxRate = new System.Windows.Forms.ComboBox();
			this.buttonPlaySource = new System.Windows.Forms.Button();
			this.trackBarCrossFader = new System.Windows.Forms.TrackBar();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.buttonStop = new System.Windows.Forms.Button();
			this.buttonStartRec = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.progressBarRecR = new System.Windows.Forms.ProgressBar();
			this.labelRec = new System.Windows.Forms.Label();
			this.progressBarRecL = new System.Windows.Forms.ProgressBar();
			this.buttonStopRec = new System.Windows.Forms.Button();
			this.pictureBoxLiveWave = new System.Windows.Forms.PictureBox();
			this.checkBoxMonitor = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCrossFader)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// openFileDialog
			// 
			this.openFileDialog.Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav";
			this.openFileDialog.Title = "Select an audio file to play";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(16, 10);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(260, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "1. Select a file to play";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// buttonEncode
			// 
			this.buttonEncode.Location = new System.Drawing.Point(16, 92);
			this.buttonEncode.Name = "buttonEncode";
			this.buttonEncode.Size = new System.Drawing.Size(260, 23);
			this.buttonEncode.TabIndex = 8;
			this.buttonEncode.Text = "2. Encode";
			this.buttonEncode.Click += new System.EventHandler(this.buttonEncode_Click);
			// 
			// folderBrowserDialog
			// 
			this.folderBrowserDialog.Description = "Select a folder which contains BASS Add-Ons (these add-ons will be loaded)...";
			this.folderBrowserDialog.ShowNewFolderButton = false;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(16, 212);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(558, 23);
			this.label1.TabIndex = 11;
			// 
			// comboBoxRate
			// 
			this.comboBoxRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRate.Location = new System.Drawing.Point(16, 60);
			this.comboBoxRate.Name = "comboBoxRate";
			this.comboBoxRate.Size = new System.Drawing.Size(260, 21);
			this.comboBoxRate.TabIndex = 12;
			// 
			// buttonPlaySource
			// 
			this.buttonPlaySource.Enabled = false;
			this.buttonPlaySource.Location = new System.Drawing.Point(16, 130);
			this.buttonPlaySource.Name = "buttonPlaySource";
			this.buttonPlaySource.Size = new System.Drawing.Size(176, 23);
			this.buttonPlaySource.TabIndex = 13;
			this.buttonPlaySource.Text = "3. Play Source and Destination";
			this.buttonPlaySource.Click += new System.EventHandler(this.buttonPlaySource_Click);
			// 
			// trackBarCrossFader
			// 
			this.trackBarCrossFader.Location = new System.Drawing.Point(16, 188);
			this.trackBarCrossFader.Maximum = 100;
			this.trackBarCrossFader.Minimum = -100;
			this.trackBarCrossFader.Name = "trackBarCrossFader";
			this.trackBarCrossFader.Size = new System.Drawing.Size(260, 45);
			this.trackBarCrossFader.SmallChange = 10;
			this.trackBarCrossFader.TabIndex = 15;
			this.trackBarCrossFader.TickFrequency = 20;
			this.trackBarCrossFader.Value = -100;
			this.trackBarCrossFader.Scroll += new System.EventHandler(this.trackBarCrossFader_Scroll);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(96, 158);
			this.label2.Name = "label2";
			this.label2.TabIndex = 16;
			this.label2.Text = "CrossFader";
			this.label2.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 44);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(100, 14);
			this.label3.TabIndex = 17;
			this.label3.Text = "Encoding Rate:";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 165);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 23);
			this.label4.TabIndex = 18;
			this.label4.Text = "Original";
			this.label4.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(222, 165);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(54, 23);
			this.label5.TabIndex = 19;
			this.label5.Text = "Encoded";
			this.label5.TextAlign = System.Drawing.ContentAlignment.BottomRight;
			// 
			// buttonStop
			// 
			this.buttonStop.Location = new System.Drawing.Point(201, 130);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.TabIndex = 20;
			this.buttonStop.Text = "Stop";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// buttonStartRec
			// 
			this.buttonStartRec.Location = new System.Drawing.Point(12, 56);
			this.buttonStartRec.Name = "buttonStartRec";
			this.buttonStartRec.Size = new System.Drawing.Size(78, 23);
			this.buttonStartRec.TabIndex = 21;
			this.buttonStartRec.Text = "Start Rec";
			this.buttonStartRec.Click += new System.EventHandler(this.buttonStartRec_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.checkBoxMonitor);
			this.groupBox1.Controls.Add(this.progressBarRecR);
			this.groupBox1.Controls.Add(this.labelRec);
			this.groupBox1.Controls.Add(this.progressBarRecL);
			this.groupBox1.Controls.Add(this.buttonStopRec);
			this.groupBox1.Controls.Add(this.buttonStartRec);
			this.groupBox1.Controls.Add(this.pictureBoxLiveWave);
			this.groupBox1.Location = new System.Drawing.Point(286, 6);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(300, 220);
			this.groupBox1.TabIndex = 22;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Live Recording and Encoding";
			// 
			// progressBarRecR
			// 
			this.progressBarRecR.Location = new System.Drawing.Point(8, 124);
			this.progressBarRecR.Maximum = 32768;
			this.progressBarRecR.Name = "progressBarRecR";
			this.progressBarRecR.Size = new System.Drawing.Size(284, 14);
			this.progressBarRecR.TabIndex = 25;
			// 
			// labelRec
			// 
			this.labelRec.Location = new System.Drawing.Point(8, 84);
			this.labelRec.Name = "labelRec";
			this.labelRec.Size = new System.Drawing.Size(284, 18);
			this.labelRec.TabIndex = 24;
			// 
			// progressBarRecL
			// 
			this.progressBarRecL.Location = new System.Drawing.Point(8, 108);
			this.progressBarRecL.Maximum = 32768;
			this.progressBarRecL.Name = "progressBarRecL";
			this.progressBarRecL.Size = new System.Drawing.Size(284, 14);
			this.progressBarRecL.TabIndex = 23;
			// 
			// buttonStopRec
			// 
			this.buttonStopRec.Location = new System.Drawing.Point(106, 56);
			this.buttonStopRec.Name = "buttonStopRec";
			this.buttonStopRec.Size = new System.Drawing.Size(78, 23);
			this.buttonStopRec.TabIndex = 22;
			this.buttonStopRec.Text = "Stop Rec";
			this.buttonStopRec.Click += new System.EventHandler(this.buttonStopRec_Click);
			// 
			// pictureBoxLiveWave
			// 
			this.pictureBoxLiveWave.Location = new System.Drawing.Point(8, 154);
			this.pictureBoxLiveWave.Name = "pictureBoxLiveWave";
			this.pictureBoxLiveWave.Size = new System.Drawing.Size(284, 56);
			this.pictureBoxLiveWave.TabIndex = 26;
			this.pictureBoxLiveWave.TabStop = false;
			// 
			// checkBoxMonitor
			// 
			this.checkBoxMonitor.Location = new System.Drawing.Point(208, 55);
			this.checkBoxMonitor.Name = "checkBoxMonitor";
			this.checkBoxMonitor.Size = new System.Drawing.Size(72, 24);
			this.checkBoxMonitor.TabIndex = 27;
			this.checkBoxMonitor.Text = "Monitor";
			this.checkBoxMonitor.CheckedChanged += new System.EventHandler(this.checkBoxMonitor_CheckedChanged);
			// 
			// Encoder
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(590, 232);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.trackBarCrossFader);
			this.Controls.Add(this.buttonPlaySource);
			this.Controls.Add(this.comboBoxRate);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonEncode);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label2);
			this.Name = "Encoder";
			this.Text = "(Wma)Encoder Test";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.Encoder_Closing);
			this.Load += new System.EventHandler(this.Encoder_Load);
			((System.ComponentModel.ISupportInitialize)(this.trackBarCrossFader)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new Encoder());
		}

		// LOCAL VARS
		private int _wmaPlugIn = 0;
		private int _stream = 0;
		private int _encHandle = 0;
		private string _fileName = String.Empty;
		private string _fileNameOutput = String.Empty;
		private byte[] _encBuffer = new byte[65536]; // our encoder buffer (32KB x 16-bit)
		private TAG_INFO _tagInfo;

		private void Encoder_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your email", "your regkey");

			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) )
			{
				BASS_INFO info = new BASS_INFO();
				Bass.BASS_GetInfo( info );
				Console.WriteLine( info.ToString() );

				_wmaPlugIn = Bass.BASS_PluginLoad("basswma.dll");
				// all fine
				int[] rates = BassWma.BASS_WMA_EncodeGetRates( 44100, 2, BASSWMAEncode.BASS_WMA_ENCODE_RATES_CBR);
				foreach( int rate in rates)
					this.comboBoxRate.Items.Add( rate );
				this.comboBoxRate.SelectedIndex = 0;
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );

			// init your recording device (we use the default device)
			if ( !Bass.BASS_RecordInit(-1) )
				MessageBox.Show(this, "Bass_RecordInit error!" );
		}

		private void Encoder_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			buttonStopRec.PerformClick();
			Bass.BASS_StreamFree(monStream);
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
			Bass.BASS_PluginFree(_wmaPlugIn);
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.label1.Text = "";
			_fileNameOutput = String.Empty;
			buttonPlaySource.Enabled = false;
			this.openFileDialog.FileName = _fileName;
			if ( DialogResult.OK == this.openFileDialog.ShowDialog(this) )
			{
				if ( File.Exists(this.openFileDialog.FileName) )
					_fileName = this.openFileDialog.FileName;
				else
					_fileName = String.Empty;
			}
		}

		private void buttonEncode_Click(object sender, System.EventArgs e)
		{
			buttonPlaySource.Enabled = false;
			this.label1.Text = "";
			Bass.BASS_StreamFree(_stream);
			if (_fileName != String.Empty)
			{
				this.label1.Text = "Encoding started...";
				// output will be placed to out bin directory
				_fileNameOutput = Path.Combine( Application.StartupPath, Path.ChangeExtension( Path.GetFileName(_fileName), ".wma" ) );
				// create the encoder...
				_encHandle = BassWma.BASS_WMA_EncodeOpenFile(44100, 2, BASSWMAEncode.BASS_WMA_ENCODE_DEFAULT, (int)this.comboBoxRate.SelectedItem, _fileNameOutput);
				// create the stream
				_stream = Bass.BASS_StreamCreateFile(_fileName, 0, 0, BASSFlag.BASS_STREAM_DECODE);
				// update the tags
				_tagInfo = new TAG_INFO(_fileName);
				// now we want to copy our tags from the source to the new destination file...
				if ( BassTags.BASS_TAG_GetFromFile( _stream, _tagInfo) )
				{
					bool ok = true;
					// and set the tags in our output file as well...
					if (_tagInfo.album != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/AlbumTitle", _tagInfo.album);
					if (_tagInfo.artist != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "Author", _tagInfo.artist);
					if (_tagInfo.comment != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "Description", _tagInfo.comment);
					if (_tagInfo.composer != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/Composer", _tagInfo.composer);
					if (_tagInfo.copyright != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "Copyright", _tagInfo.copyright);
					if (_tagInfo.encodedby != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/EncodedBy", _tagInfo.encodedby);
					if (_tagInfo.genre != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/Genre", _tagInfo.genre);
					if (_tagInfo.publisher != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/Publisher", _tagInfo.publisher);
					if (_tagInfo.title != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "Title", _tagInfo.title);
					if (_tagInfo.track != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/TrackNumber", _tagInfo.track);
					if (_tagInfo.year != String.Empty)
						ok &= BassWma.BASS_WMA_EncodeSetTag( _encHandle, "WM/Year", _tagInfo.year);
				}
				// finish setting tags is no longer needed!

				// encode the data
				while ( Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING )
				{
					// get the decoded sample data
					int len = Bass.BASS_ChannelGetData(_stream, _encBuffer, 65536);
					// write the data to the encoder
                    BassWma.BASS_WMA_EncodeWrite(_encHandle, _encBuffer, len);

					long total = Bass.BASS_ChannelGetLength(_stream);
					long pos = Bass.BASS_ChannelGetPosition(_stream);
					this.label1.Text = String.Format("Encoding : {0:P}", pos/(float)total);
					this.label1.Refresh();
				}
				// finish
				BassWma.BASS_WMA_EncodeClose(_encHandle);
				Bass.BASS_StreamFree(_stream);
				this.label1.Text = "Encoding finished!";
				buttonPlaySource.Enabled = true;
			}
		}

		private int _mixerStream = 0;
		private int _streamA = 0;
		private int _streamB = 0;
		private void buttonPlaySource_Click(object sender, System.EventArgs e)
		{
			Bass.BASS_StreamFree(_streamA);
			Bass.BASS_StreamFree(_streamB);
			Bass.BASS_StreamFree(_mixerStream);
			// mixer setup
			// now we need some channels to plug them in...create two decoding sources
			_streamA = Bass.BASS_StreamCreateFile(_fileName, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
			_streamB = Bass.BASS_StreamCreateFile(_fileNameOutput, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
			BASS_CHANNELINFO i = new BASS_CHANNELINFO();
			Bass.BASS_ChannelGetInfo(_streamA, i);
			// this will be the final mixer output stream being played
			_mixerStream = BassMix.BASS_Mixer_StreamCreate(i.freq, 4, BASSFlag.BASS_DEFAULT );
			// finally we plug them into the mixer (and upmix it to 4 channels - we assume the source to be stereo)
			bool okA = BassMix.BASS_Mixer_StreamAddChannel(_mixerStream, _streamA, BASSFlag.BASS_MIXER_MATRIX | BASSFlag.BASS_STREAM_AUTOFREE);
			bool okB = BassMix.BASS_Mixer_StreamAddChannel(_mixerStream, _streamB, BASSFlag.BASS_STREAM_AUTOFREE);
			// a matrix for A!		
			float[,] matrixA = new float[4,2] { // stereo to quad matrix
												{1, 0}, // left front out = left in
												{0, 1}, // right front out = right in
												{1, 0}, // left rear out = left in
												{0, 1} // right rear out = right in
											   };
			// apply the matrix to stream A only
			BassMix.BASS_Mixer_ChannelSetMatrix(_streamA, matrixA );
			// just so show how to get it back...
			float[,] matrixGet = new float[4,2];
			BassMix.BASS_Mixer_ChannelGetMatrix(_streamA, matrixGet );
			// mute streamB at the beginning
			this.trackBarCrossFader.Value = -100;
			Bass.BASS_ChannelSetAttribute(_streamB, BASSAttribute.BASS_ATTRIB_VOL, 0f);

			// and play it...
			if ( Bass.BASS_ChannelPlay(_mixerStream, false) )
				this.label1.Text = "Playing! Use the crossfader...";
		}

		private void trackBarCrossFader_Scroll(object sender, System.EventArgs e)
		{
			// 0 = both to volume max (100) - since we are playing the same track this might sound lounder!
			// - = more to the source
			// + = more to the desting
			int cf = this.trackBarCrossFader.Value;
			int volA = 100;
			if (cf > 0)
				volA -= cf;
			int volB = 100;
			if (cf < 0)
				volB += cf;

			Bass.BASS_ChannelSetAttribute(_streamA, BASSAttribute.BASS_ATTRIB_VOL, 100f / volA);
            Bass.BASS_ChannelSetAttribute(_streamB, BASSAttribute.BASS_ATTRIB_VOL, 100f / volB);
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			buttonPlaySource.Enabled = false;
			Bass.BASS_ChannelStop(_mixerStream);
			Bass.BASS_StreamFree(_streamA);
			Bass.BASS_StreamFree(_streamB);
			Bass.BASS_StreamFree(_mixerStream);
		}

		#region Live Recording and Encoding

		private WaveForm WF = null;
		private BASSBuffer monBuffer = new BASSBuffer(2f, 44100, 2, 16); // 44.1kHz, 16-bit, stereo (like we record!)
		private int monStream = 0;
		private STREAMPROC monProc = null;
		EncoderLAME lame = null;

		private void buttonStartRec_Click(object sender, System.EventArgs e)
		{
			this.buttonStartRec.Enabled = false;
			this.labelRec.Text = "Recording...";

			// start recording paused
			_myRecProc = new RECORDPROC(MyRecoring);
			_recHandle = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_RECORD_PAUSE, _myRecProc, new IntPtr(_encHandle));

			// needs 'lame.exe' !
			// the recorded data will be written to a file called rectest.mp3
			// create the encoder...192kbps, stereo
			// MP3 encoder setup
			lame = new EncoderLAME(_recHandle);
			lame.InputFile = null;	//STDIN
			lame.OutputFile = "rectest.mp3";
			lame.LAME_Bitrate = (int)EncoderLAME.BITRATE.kbps_192;
			lame.LAME_Mode = EncoderLAME.LAMEMode.Default;
			lame.LAME_TargetSampleRate = (int)EncoderLAME.SAMPLERATE.Hz_44100;
			lame.LAME_Quality = EncoderLAME.LAMEQuality.Quality;

			monBuffer.Clear();
			checkBoxMonitor.Checked = false;

			// create a live recording WaveForm
			WF = new WaveForm();
			WF.FrameResolution = 0.01f; // 10ms are nice
			// start a live recording waveform with 5sec. init size and 2sec. next size
			WF.RenderStartRecording(_recHandle, 5f, 2f);

			// really start recording
			lame.Start(null, IntPtr.Zero, false);
			Bass.BASS_ChannelPlay(_recHandle, false);
		}

		private void buttonStopRec_Click(object sender, System.EventArgs e)
		{
			Bass.BASS_ChannelStop(_recHandle);
			lame.Stop();
			this.buttonStartRec.Enabled = true;
			this.labelRec.Text = "Recording stopped!";
			// stop the live recording waveform
			if (WF != null)
			{
				WF.RenderStopRecording();
				//DrawWave();
				// or now draw the wole wave form...
				// Note: to total length is always up to how it was initialized using WF.RenderStartRecording(_recHandle, 5, 2);
				//       This means it is either 5, 7, 9, 11,... etc. seconds long
				//       This is why sometimes the wave form doesn't fill the full pictureBox!
				this.pictureBoxLiveWave.BackgroundImage = WF.CreateBitmap( this.pictureBoxLiveWave.Width, this.pictureBoxLiveWave.Height, -1, -1, true);
			}
			if (checkBoxMonitor.Checked)
			{
				checkBoxMonitor.Checked = false;
			}
		}

		private void checkBoxMonitor_CheckedChanged(object sender, System.EventArgs e)
		{
			if (checkBoxMonitor.Checked)
			{
				monProc = new STREAMPROC(MonitoringStream);
                monStream = Bass.BASS_StreamCreate(44100, 2, 0, monProc, IntPtr.Zero); // user = reader#
				Bass.BASS_ChannelPlay(monStream, false);
				
			}
			else
			{
				Bass.BASS_StreamFree(monStream);
			}
		}

        private int MonitoringStream(int handle, IntPtr buffer, int length, IntPtr user)
		{
			return monBuffer.Read(buffer, length, user.ToInt32());
		}

		private RECORDPROC _myRecProc; // make it global, so that the Garbage Collector can not remove it
		private int _recHandle = 0;
		// the recording callback
		private unsafe bool MyRecoring(int handle, IntPtr buffer, int length, IntPtr user)
		{
			// user will contain our encoding handle
			if (length > 0 && buffer != IntPtr.Zero)
			{
				if (checkBoxMonitor.Checked)
				{
					monBuffer.Write(buffer, length);
				}
				
				if (!this.buttonStartRec.Enabled)
				{
					// if recording started...write the data to the encoder
					BassWma.BASS_WMA_EncodeWrite(user.ToInt32(), buffer, length);
				}
				// get and draw our live recording waveform
				WF.RenderRecording(buffer, length);
				//WF.RenderRecording();  // old style

				// get the rec level...
				int maxL = 0;
				int maxR = 0;
                short* data = (short*)buffer;
                for (int a = 0; a < length / 2; a++)
                {
                    // decide on L/R channel
                    if (a % 2 == 0)
                    {
                        // L channel
                        if (data[a] > maxL)
                            maxL = data[a];
                    }
                    else
                    {
                        // R channel
                        if (data[a] > maxR)
                            maxR = data[a];
                    }
                }
				// limit the maximum peak levels to 0bB = 32768
				// the peak levels will be int values, where 32768 = 0dB!
				if (maxL > 32768)
					maxL = 32768;
				if (maxR > 32768)
					maxR = 32768;
			
				this.BeginInvoke(new UpdateDelegate(UpdateDisplay), new object[] { maxL, maxR });
				// you might instead also use "this.Invoke(...)", which would call the delegate synchron!
			}
			return true; // always continue recording
		}

		int _zoomDistance;
		int _zoomStart;
		int _zoomEnd;
		private void DrawWave()
		{
			if (WF != null)
			{
				_zoomDistance = 200; // = 5sec., since our resolution is 0.01sec.
				_zoomEnd = WF.FramesRendered > _zoomDistance ? WF.FramesRendered : _zoomDistance;
				_zoomStart = _zoomEnd - _zoomDistance;

				this.pictureBoxLiveWave.BackgroundImage = WF.CreateBitmap( this.pictureBoxLiveWave.Width, this.pictureBoxLiveWave.Height, _zoomStart, _zoomEnd, true);
			}
			else
				this.pictureBoxLiveWave.BackgroundImage = null;
		}

		public delegate void UpdateDelegate(int maxL, int maxR);
		private void UpdateDisplay(int maxL, int maxR)
		{
			this.progressBarRecL.Value = maxL;
			this.progressBarRecR.Value = maxR;
			DrawWave();
		}

		#endregion Live Recording and Encoding

	}
}
