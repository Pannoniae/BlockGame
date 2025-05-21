using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Un4seen.Bass.AddOn.Tags;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!


	public class Simple : System.Windows.Forms.Form
	{
		// LOCAL VARS
		private int _stream = 0;
		private string _fileName = String.Empty;
		private int _tickCounter = 0;
		private float _gainDB = 0f;
		private DSPPROC _myDSPAddr = null;
		private SYNCPROC _sync = null;
		private int _syncer = 0;
		private int _deviceLatencyMS = 0; // device latency in milliseconds
		private int _deviceLatencyBytes = 0; // device latency in bytes
		private Visuals _vis = new Visuals(); // visuals class instance
		private int _updateInterval = 50; // 50ms
		private Un4seen.Bass.BASSTimer _updateTimer = null;

		//

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.ProgressBar progressBarPeakLeft;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ProgressBar progressBarPeakRight;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonPlay;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox textBoxGainDBValue;
		private System.Windows.Forms.Button buttonSetGain;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button buttonZoom;
		private System.Windows.Forms.TextBox textBoxYear;
		private System.Windows.Forms.TextBox textBoxGenre;
		private System.Windows.Forms.TextBox textBoxComment;
		private System.Windows.Forms.TextBox textBoxAlbum;
		private System.Windows.Forms.TextBox textBoxTitle;
		private System.Windows.Forms.TextBox textBoxArtist;
		private System.Windows.Forms.TextBox textBoxTrack;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.PictureBox pictureBoxSpectrum;
		private System.Windows.Forms.Label labelVis;
		private System.Windows.Forms.PictureBox pictureBoxTagImage;
		private System.Windows.Forms.TextBox textBoxPicDescr;
		private System.Windows.Forms.PictureBox pictureBox1;

		public Simple()
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
			this.button1 = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.progressBarPeakLeft = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.progressBarPeakRight = new System.Windows.Forms.ProgressBar();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonPlay = new System.Windows.Forms.Button();
			this.labelTime = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.textBoxGainDBValue = new System.Windows.Forms.TextBox();
			this.buttonSetGain = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.buttonZoom = new System.Windows.Forms.Button();
			this.textBoxYear = new System.Windows.Forms.TextBox();
			this.textBoxGenre = new System.Windows.Forms.TextBox();
			this.textBoxComment = new System.Windows.Forms.TextBox();
			this.textBoxAlbum = new System.Windows.Forms.TextBox();
			this.textBoxTitle = new System.Windows.Forms.TextBox();
			this.textBoxArtist = new System.Windows.Forms.TextBox();
			this.textBoxTrack = new System.Windows.Forms.TextBox();
			this.buttonStop = new System.Windows.Forms.Button();
			this.pictureBoxSpectrum = new System.Windows.Forms.PictureBox();
			this.labelVis = new System.Windows.Forms.Label();
			this.pictureBoxTagImage = new System.Windows.Forms.PictureBox();
			this.textBoxPicDescr = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// button1
			// 
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
			// progressBarPeakLeft
			// 
			this.progressBarPeakLeft.Location = new System.Drawing.Point(20, 90);
			this.progressBarPeakLeft.Maximum = 65535;
			this.progressBarPeakLeft.Name = "progressBarPeakLeft";
			this.progressBarPeakLeft.Size = new System.Drawing.Size(250, 12);
			this.progressBarPeakLeft.Step = 1;
			this.progressBarPeakLeft.TabIndex = 9;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 90);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(16, 12);
			this.label1.TabIndex = 8;
			this.label1.Text = "L";
			// 
			// progressBarPeakRight
			// 
			this.progressBarPeakRight.Location = new System.Drawing.Point(20, 106);
			this.progressBarPeakRight.Maximum = 65535;
			this.progressBarPeakRight.Name = "progressBarPeakRight";
			this.progressBarPeakRight.Size = new System.Drawing.Size(250, 12);
			this.progressBarPeakRight.Step = 1;
			this.progressBarPeakRight.TabIndex = 11;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 106);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(16, 12);
			this.label2.TabIndex = 10;
			this.label2.Text = "R";
			// 
			// buttonPlay
			// 
			this.buttonPlay.Location = new System.Drawing.Point(12, 42);
			this.buttonPlay.Name = "buttonPlay";
			this.buttonPlay.Size = new System.Drawing.Size(64, 23);
			this.buttonPlay.TabIndex = 1;
			this.buttonPlay.Text = "PLAY";
			this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
			// 
			// labelTime
			// 
			this.labelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelTime.Location = new System.Drawing.Point(10, 128);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(260, 18);
			this.labelTime.TabIndex = 12;
			this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(144, 76);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(28, 23);
			this.label3.TabIndex = 6;
			this.label3.Text = "|0dB";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(230, 76);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(42, 23);
			this.label4.TabIndex = 7;
			this.label4.Text = "+6dB|";
			this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(18, 76);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(42, 23);
			this.label5.TabIndex = 5;
			this.label5.Text = "|-90dB";
			// 
			// textBoxGainDBValue
			// 
			this.textBoxGainDBValue.Location = new System.Drawing.Point(146, 44);
			this.textBoxGainDBValue.Name = "textBoxGainDBValue";
			this.textBoxGainDBValue.Size = new System.Drawing.Size(40, 20);
			this.textBoxGainDBValue.TabIndex = 2;
			this.textBoxGainDBValue.Text = "0";
			this.textBoxGainDBValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// buttonSetGain
			// 
			this.buttonSetGain.Location = new System.Drawing.Point(188, 42);
			this.buttonSetGain.Name = "buttonSetGain";
			this.buttonSetGain.Size = new System.Drawing.Size(82, 23);
			this.buttonSetGain.TabIndex = 3;
			this.buttonSetGain.Text = "Set Gain (dB)";
			this.buttonSetGain.Click += new System.EventHandler(this.buttonSetGain_Click);
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(10, 342);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new System.Drawing.Size(474, 58);
			this.textBox1.TabIndex = 21;
			this.textBox1.Text = "";
			this.textBox1.WordWrap = false;
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.WhiteSmoke;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBox1.Location = new System.Drawing.Point(10, 154);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(562, 110);
			this.pictureBox1.TabIndex = 15;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Resize += new System.EventHandler(this.pictureBox1_Resize);
			this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
			// 
			// buttonZoom
			// 
			this.buttonZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonZoom.Location = new System.Drawing.Point(497, 126);
			this.buttonZoom.Name = "buttonZoom";
			this.buttonZoom.TabIndex = 13;
			this.buttonZoom.Text = "Zoom";
			this.buttonZoom.Click += new System.EventHandler(this.buttonZoom_Click);
			// 
			// textBoxYear
			// 
			this.textBoxYear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxYear.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxYear.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxYear.Location = new System.Drawing.Point(336, 286);
			this.textBoxYear.Name = "textBoxYear";
			this.textBoxYear.Size = new System.Drawing.Size(236, 13);
			this.textBoxYear.TabIndex = 17;
			this.textBoxYear.Text = "Year";
			// 
			// textBoxGenre
			// 
			this.textBoxGenre.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxGenre.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxGenre.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxGenre.Location = new System.Drawing.Point(336, 268);
			this.textBoxGenre.Name = "textBoxGenre";
			this.textBoxGenre.Size = new System.Drawing.Size(236, 13);
			this.textBoxGenre.TabIndex = 15;
			this.textBoxGenre.Text = "Genre";
			// 
			// textBoxComment
			// 
			this.textBoxComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxComment.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxComment.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxComment.Location = new System.Drawing.Point(10, 322);
			this.textBoxComment.Name = "textBoxComment";
			this.textBoxComment.Size = new System.Drawing.Size(468, 13);
			this.textBoxComment.TabIndex = 20;
			this.textBoxComment.Text = "Comment";
			// 
			// textBoxAlbum
			// 
			this.textBoxAlbum.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxAlbum.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxAlbum.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxAlbum.Location = new System.Drawing.Point(10, 304);
			this.textBoxAlbum.Name = "textBoxAlbum";
			this.textBoxAlbum.Size = new System.Drawing.Size(294, 13);
			this.textBoxAlbum.TabIndex = 18;
			this.textBoxAlbum.Text = "Album";
			// 
			// textBoxTitle
			// 
			this.textBoxTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTitle.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxTitle.Location = new System.Drawing.Point(10, 286);
			this.textBoxTitle.Name = "textBoxTitle";
			this.textBoxTitle.Size = new System.Drawing.Size(294, 13);
			this.textBoxTitle.TabIndex = 16;
			this.textBoxTitle.Text = "Title";
			// 
			// textBoxArtist
			// 
			this.textBoxArtist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxArtist.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxArtist.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxArtist.Location = new System.Drawing.Point(10, 268);
			this.textBoxArtist.Name = "textBoxArtist";
			this.textBoxArtist.Size = new System.Drawing.Size(294, 13);
			this.textBoxArtist.TabIndex = 14;
			this.textBoxArtist.Text = "Artist";
			// 
			// textBoxTrack
			// 
			this.textBoxTrack.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTrack.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxTrack.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxTrack.Location = new System.Drawing.Point(336, 304);
			this.textBoxTrack.Name = "textBoxTrack";
			this.textBoxTrack.Size = new System.Drawing.Size(142, 13);
			this.textBoxTrack.TabIndex = 19;
			this.textBoxTrack.Text = "Track";
			// 
			// buttonStop
			// 
			this.buttonStop.Enabled = false;
			this.buttonStop.Location = new System.Drawing.Point(80, 42);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size(64, 23);
			this.buttonStop.TabIndex = 4;
			this.buttonStop.Text = "STOP";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// pictureBoxSpectrum
			// 
			this.pictureBoxSpectrum.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBoxSpectrum.BackColor = System.Drawing.Color.Black;
			this.pictureBoxSpectrum.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pictureBoxSpectrum.Location = new System.Drawing.Point(276, 10);
			this.pictureBoxSpectrum.Name = "pictureBoxSpectrum";
			this.pictureBoxSpectrum.Size = new System.Drawing.Size(296, 108);
			this.pictureBoxSpectrum.TabIndex = 22;
			this.pictureBoxSpectrum.TabStop = false;
			this.pictureBoxSpectrum.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxSpectrum_MouseDown);
			// 
			// labelVis
			// 
			this.labelVis.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.labelVis.Location = new System.Drawing.Point(276, 124);
			this.labelVis.Name = "labelVis";
			this.labelVis.Size = new System.Drawing.Size(188, 14);
			this.labelVis.TabIndex = 23;
			this.labelVis.Text = "16 of 16 (click L/R mouse to change)";
			this.labelVis.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// pictureBoxTagImage
			// 
			this.pictureBoxTagImage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBoxTagImage.Location = new System.Drawing.Point(490, 326);
			this.pictureBoxTagImage.Name = "pictureBoxTagImage";
			this.pictureBoxTagImage.Size = new System.Drawing.Size(86, 74);
			this.pictureBoxTagImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBoxTagImage.TabIndex = 24;
			this.pictureBoxTagImage.TabStop = false;
			// 
			// textBoxPicDescr
			// 
			this.textBoxPicDescr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxPicDescr.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxPicDescr.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxPicDescr.Location = new System.Drawing.Point(490, 306);
			this.textBoxPicDescr.Name = "textBoxPicDescr";
			this.textBoxPicDescr.Size = new System.Drawing.Size(86, 13);
			this.textBoxPicDescr.TabIndex = 25;
			this.textBoxPicDescr.Text = "";
			// 
			// Simple
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(584, 406);
			this.Controls.Add(this.textBoxPicDescr);
			this.Controls.Add(this.textBoxTrack);
			this.Controls.Add(this.textBoxYear);
			this.Controls.Add(this.textBoxGenre);
			this.Controls.Add(this.textBoxComment);
			this.Controls.Add(this.textBoxAlbum);
			this.Controls.Add(this.textBoxTitle);
			this.Controls.Add(this.textBoxArtist);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.textBoxGainDBValue);
			this.Controls.Add(this.pictureBoxTagImage);
			this.Controls.Add(this.pictureBoxSpectrum);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.buttonZoom);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.buttonSetGain);
			this.Controls.Add(this.labelTime);
			this.Controls.Add(this.buttonPlay);
			this.Controls.Add(this.progressBarPeakRight);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.progressBarPeakLeft);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.labelVis);
			this.Name = "Simple";
			this.Text = "Simple";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.Simple_Closing);
			this.Load += new System.EventHandler(this.Simple_Load);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new Simple());
		}

		private void Simple_Load(object sender, System.EventArgs e)
		{
			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY, this.Handle) )
			{
				BASS_INFO info = new BASS_INFO();
				Bass.BASS_GetInfo( info );
				Console.WriteLine( info.ToString() );
				_deviceLatencyMS = info.latency;
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );

			// create a secure timer
			_updateTimer = new Un4seen.Bass.BASSTimer(_updateInterval);
			_updateTimer.Tick += new EventHandler(timerUpdate_Tick);

			_sync = new SYNCPROC(EndPosition);
		}

		private void Simple_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_updateTimer.Tick -= new EventHandler(timerUpdate_Tick);
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
		}

		private void buttonPlay_Click(object sender, System.EventArgs e)
		{
			_updateTimer.Stop();
			Bass.BASS_StreamFree(_stream);
			if (_fileName != String.Empty)
			{
				// create the stream
				_stream = Bass.BASS_StreamCreateFile(_fileName, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);
				if (_stream != 0)
				{
					// used in RMS
					_30mslength = (int)Bass.BASS_ChannelSeconds2Bytes(_stream, 0.03); // 30ms window
					// latency from milliseconds to bytes
					_deviceLatencyBytes = (int)Bass.BASS_ChannelSeconds2Bytes(_stream, _deviceLatencyMS/1000.0);

					// set a DSP user callback method
					//_myDSPAddr = new DSPPROC(MyDSPGain);
					//Bass.BASS_ChannelSetDSP(_stream, _myDSPAddr, 0, 2);
					// if you want to use the above two line instead (uncomment the above and comment below)
					_myDSPAddr = new DSPPROC(MyDSPGainUnsafe);
                    Bass.BASS_ChannelSetDSP(_stream, _myDSPAddr, IntPtr.Zero, 2);

					if (WF2 != null && WF2.IsRendered)
					{
						// make sure playback and wave form are in sync, since
						// we rended with 16-bit but play here with 32-bit
						WF2.SyncPlayback(_stream);
						
						long cuein  = WF2.GetMarker( "CUE" );
						long cueout = WF2.GetMarker( "END" );

						int cueinFrame  = WF2.Position2Frames(cuein);
						int cueoutFrame = WF2.Position2Frames(cueout);
						Console.WriteLine( "CueIn at {0}sec.; CueOut at {1}sec.", WF2.Frame2Seconds(cueinFrame), WF2.Frame2Seconds(cueoutFrame) );

						if (cuein >= 0)
						{
							Bass.BASS_ChannelSetPosition(_stream, cuein);
						}
						if (cueout >= 0)
						{
							Bass.BASS_ChannelRemoveSync(_stream, _syncer);
                            _syncer = Bass.BASS_ChannelSetSync(_stream, BASSSync.BASS_SYNC_POS, cueout, _sync, IntPtr.Zero);
						}
					}
				}

				if (_stream != 0 && Bass.BASS_ChannelPlay(_stream, false) )
				{
					this.textBox1.Text = "";
					_updateTimer.Start();

					// get some channel info
					BASS_CHANNELINFO info = new BASS_CHANNELINFO();
					Bass.BASS_ChannelGetInfo(_stream, info);
					this.textBox1.Text += "Info: "+info.ToString()+Environment.NewLine;
					// display the tags...
					TAG_INFO tagInfo = new TAG_INFO(_fileName);
					if ( BassTags.BASS_TAG_GetFromFile( _stream, tagInfo) )
					{
						// and display what we get
						this.textBoxAlbum.Text = tagInfo.album;
						this.textBoxArtist.Text = tagInfo.artist;
						this.textBoxTitle.Text = tagInfo.title;
						this.textBoxComment.Text = tagInfo.comment;
						this.textBoxGenre.Text = tagInfo.genre;
						this.textBoxYear.Text = tagInfo.year;
						this.textBoxTrack.Text = tagInfo.track;
						this.pictureBoxTagImage.Image = tagInfo.PictureGetImage(0);
						this.textBoxPicDescr.Text = tagInfo.PictureGetDescription(0);
						if (this.textBoxPicDescr.Text == String.Empty)
							this.textBoxPicDescr.Text = tagInfo.PictureGetType(0);
					}
					this.buttonStop.Enabled = true;
					this.buttonPlay.Enabled = false;
				}
				else
				{
                    Console.WriteLine("Error={0}", Bass.BASS_ErrorGetCode());
				}
			}
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			_updateTimer.Stop();

			// kills rendering, if still in progress, e.g. if a large file was selected
			if (WF2 != null && WF2.IsRenderingInProgress)
				WF2.RenderStop();
			
			Bass.BASS_StreamFree(_stream);
			_stream = 0;
			this.button1.Text = "Select a file to play (e.g. MP3, OGG or WAV)...";
			this.buttonStop.Enabled = false;
			this.buttonPlay.Enabled = true;
		}

        private void EndPosition(int handle, int channel, int data, IntPtr user)
		{
			Bass.BASS_ChannelStop(channel);
		}

		private void timerUpdate_Tick(object sender, System.EventArgs e)
		{
			// here we gather info about the stream, when it is playing...
			if ( Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING )
			{
				// the stream is still playing...
			}
			else
			{
				// the stream is NOT playing anymore...
				_updateTimer.Stop();
				this.progressBarPeakLeft.Value = 0;
				this.progressBarPeakRight.Value = 0;
				this.labelTime.Text = "Stopped";
				DrawWavePosition(-1, -1);
				this.pictureBoxSpectrum.Image = null;
				this.buttonStop.Enabled = false;
				this.buttonPlay.Enabled = true;
				return;
			}

			// from here on, the stream is for sure playing...
			_tickCounter++;
			long pos = Bass.BASS_ChannelGetPosition(_stream); // position in bytes
			long len = Bass.BASS_ChannelGetLength(_stream); // length in bytes

			if (_tickCounter == 5)
			{
				// display the position every 250ms (since timer is 50ms)
				_tickCounter = 0;
                double totaltime = Bass.BASS_ChannelBytes2Seconds(_stream, len); // the total time length
                double elapsedtime = Bass.BASS_ChannelBytes2Seconds(_stream, pos); // the elapsed time length
                double remainingtime = totaltime - elapsedtime;
				this.labelTime.Text = String.Format( "Elapsed: {0:#0.00} - Total: {1:#0.00} - Remain: {2:#0.00}", Utils.FixTimespan(elapsedtime,"MMSS"), Utils.FixTimespan(totaltime,"MMSS"), Utils.FixTimespan(remainingtime,"MMSS"));
				this.Text = String.Format( "Bass-CPU: {0:0.00}% (not including Waves & Spectrum!)", Bass.BASS_GetCPU() );
			}
			
			// display the level bars
			int peakL = 0;
			int peakR = 0;
			// for testing you might also call RMS_2, RMS_3 or RMS_4
			RMS(_stream, out peakL, out peakR);
            // level to dB
            double dBlevelL = Utils.LevelToDB(peakL, 65535);
            double dBlevelR = Utils.LevelToDB(peakR, 65535);
			//RMS_2(_stream, out peakL, out peakR);
			//RMS_3(_stream, out peakL, out peakR);
			//RMS_4(_stream, out peakL, out peakR);
			this.progressBarPeakLeft.Value = peakL;
			this.progressBarPeakRight.Value = peakR;

			// update the wave position
			DrawWavePosition(pos, len);
			// update spectrum
			DrawSpectrum();
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.openFileDialog.FileName = _fileName;
			if ( DialogResult.OK == this.openFileDialog.ShowDialog(this) )
			{
				if ( File.Exists(this.openFileDialog.FileName) )
				{
					_fileName = this.openFileDialog.FileName;
					this.button1.Text = Path.GetFileName(_fileName);

					// eg. use this to load a previously saved wave form...
					//if ( File.Exists(@"C:\test.wf") )
					//{
					//	WF2 = new Un4seen.Bass.Misc.WaveForm2();
					//	WF2.WaveFormLoadFromFile( @"C:\test.wf" );
					//	WF2.DrawMarker = WaveForm2.MARKERDRAWTYPE.Line | WaveForm2.MARKERDRAWTYPE.Name | WaveForm2.MARKERDRAWTYPE.NamePositionAlternate;
					//	DrawWave();
					//}

					// render wave form (this is done in a background thread, so that we already play the channel in parallel)
					GetWaveForm();
				}
				else
					_fileName = String.Empty;
			}
		}
		
		#region VU (peak) level meter

		// this method is a simple demo!
		// See the other implementations (RMS_2, RMS_3, RMS_4) for other examples
		// As you can see, there are many ways to interact with unmanaged code!

		private int _30mslength = 0;
		private float[] _rmsData;     // our global data buffer used at RMS

		private void RMS(int channel, out int peakL, out int peakR)
		{
			float maxL = 0f;
			float maxR = 0f;
			int length = _30mslength; // 30ms window already set at buttonPlay_Click
			int l4 = length/4; // the number of 32-bit floats required (since length is in bytes!)
			
			// increase our data buffer as needed
			if (_rmsData == null || _rmsData.Length < l4)
				_rmsData = new float[l4];

			// Note: this is a special mechanism to deal with variable length c-arrays.
			// In fact we just pass the address (reference) to the first array element to the call.
			// However the .Net marshal operation will copy N array elements (so actually fill our float[]).
			// N is determined by the size of our managed array, in this case N=l4
			length = Bass.BASS_ChannelGetData(channel, _rmsData, length);

			l4 = length/4; // the number of 32-bit floats received

			for (int a=0; a < l4; a++)
			{
                float absLevel = Math.Abs(_rmsData[a]);
				// decide on L/R channel
				if (a % 2 == 0)
				{
					// L channel
                    if (absLevel > maxL)
                        maxL = absLevel;
				}
				else
				{
					// R channel
                    if (absLevel > maxR)
                        maxR = absLevel;
				}
			}

			// limit the maximum peak levels to +6bB = 0xFFFF = 65535
			// the peak levels will be int values, where 32767 = 0dB!
			// and a float value of 1.0 also represents 0db.
			peakL = (int)Math.Round(32767f * maxL) & 0xFFFF;
			peakR = (int)Math.Round(32767f * maxR) & 0xFFFF;
		}

		// works as well, and should just demo the use of GCHandles
		private void RMS_2(int channel, out int peakL, out int peakR)
		{
			float maxL = 0f;
			float maxR = 0f;
			int length = _30mslength; // 30ms window already set at buttonPlay_Click
			int l4 = length/4; // the number of 32-bit floats required (since length is in bytes!)

			// increase our data buffer as needed
			if (_rmsData == null || _rmsData.Length < l4)
				_rmsData = new float[l4];

			// create a handle to a managed object and pin it,
			// so that the Garbage Collector will not remove it
			GCHandle hGC = GCHandle.Alloc(_rmsData, GCHandleType.Pinned);
			try
			{
				// this will hand over an IntPtr to our managed data object
				length = Bass.BASS_ChannelGetData(channel, hGC.AddrOfPinnedObject(), length);
			}
			finally
			{
				// free the pinned handle, so that the Garbage Collector can use it
				hGC.Free();
			}

			l4 = length/4; // the number of 32-bit floats received

			for (int a=0; a < l4; a++)
			{
				// decide on L/R channel
				if (a % 2 == 0)
				{
					// L channel
					if (_rmsData[a] > maxL)
						maxL = _rmsData[a];
				}
				else
				{
					// R channel
					if (_rmsData[a] > maxR)
						maxR = _rmsData[a];
				}
			}

			// limit the maximum peak levels to +6bB = 0xFFFF = 65535
			// the peak levels will be int values, where 32767 = 0dB!
			// and a float value of 1.0 also represents 0db.
			peakL = (int)Math.Round(32767f * maxL) & 0xFFFF;
			peakR = (int)Math.Round(32767f * maxR) & 0xFFFF;
		}

		// works as well (even if the slowest), and should just demo the use of Marshal.Copy
		private void RMS_3(int channel, out int peakL, out int peakR)
		{
			float maxL = 0f;
			float maxR = 0f;
			int length = _30mslength; // 30ms window already set at buttonPlay_Click
			int l4 = length/4; // the number of 32-bit floats required (since length is in bytes!)

			// increase our data buffer as needed
			if (_rmsData == null || _rmsData.Length < l4)
				_rmsData = new float[l4];

			IntPtr buffer = IntPtr.Zero;
			// allocate a buffer of that size for unmanaged code
			buffer = Marshal.AllocCoTaskMem( length );
			try
			{
				// get the data
				length = Bass.BASS_ChannelGetData(channel, buffer, length);
				l4 = length/4; // the number of 32-bit floats received
				// copy the data from unmanaged BASS to our local managed application
				Marshal.Copy( buffer, _rmsData, 0, l4);

				for (int a=0; a < l4; a++)
				{
					// decide on L/R channel
					if (a % 2 == 0)
					{
						// L channel
						if (_rmsData[a] > maxL)
							maxL = _rmsData[a];
					}
					else
					{
						// R channel
						if (_rmsData[a] > maxR)
							maxR = _rmsData[a];
					}
				}
			}
			finally
			{
				// free the allocated unmanaged memory
				Marshal.FreeCoTaskMem( buffer );
			}

			// limit the maximum peak levels to +6bB = 0xFFFF = 65535
			// the peak levels will be int values, where 32767 = 0dB!
			// and a float value of 1.0 also represents 0db.
			peakL = (int)Math.Round(32767f * maxL) & 0xFFFF;
			peakR = (int)Math.Round(32767f * maxR) & 0xFFFF;
		}

		// works as well, and should just demo the use of unsafe code blocks
		private void RMS_4(int channel, out int peakL, out int peakR)
		{
			float maxL = 0f;
			float maxR = 0f;
			int length = _30mslength; // 30ms window already set at buttonPlay_Click
			int l4 = length/4; // the number of 32-bit floats required (since length is in bytes!)

			// increase our data buffer as needed
			if (_rmsData == null || _rmsData.Length < l4)
				_rmsData = new float[l4];

			unsafe
			{
				fixed (float* p = _rmsData) // equivalent to p = &_rmsData[0]
				{
					length = Bass.BASS_ChannelGetData(channel, (IntPtr)p, length);
				}
			}
			l4 = length/4; // the number of 32-bit floats received

			for (int a=0; a < l4; a++)
			{
				// decide on L/R channel
				if (a % 2 == 0)
				{
					// L channel
					if (_rmsData[a] > maxL)
						maxL = _rmsData[a];
				}
				else
				{
					// R channel
					if (_rmsData[a] > maxR)
						maxR = _rmsData[a];
				}
			}

			// limit the maximum peak levels to +6bB = 0xFFFF = 65535
			// the peak levels will be int values, where 32767 = 0dB!
			// and a float value of 1.0 also represents 0db.
			peakL = (int)Math.Round(32767f * maxL) & 0xFFFF;
			peakR = (int)Math.Round(32767f * maxR) & 0xFFFF;
		}

		#endregion

		#region DSP (gain) routines 

		// this will be our local buffer
		// we use it outside of MyDSPGain for better performance and to reduce
		// the need to alloc it everytime MyDSPGain is called!
		private float[] _data;

		// this local member keeps the amplification level as a float value
		private float _gainAmplification = 1;

		// the DSP callback - safe!
		private void MyDSPGain(int handle, int channel, System.IntPtr buffer, int length, int user)
		{
			if (_gainAmplification == 1f || length == 0 || buffer == IntPtr.Zero)
				return;

			// length is in bytes, so the number of floats to process is:
			// length / 4 : byte = 8-bit, float = 32-bit
			int l4 = length/4;

			// increase the data buffer as needed
			if (_data == null || _data.Length < l4)
				_data = new float[l4];

			// copy from unmanaged to managed memory
			Marshal.Copy(buffer, _data, 0, l4);

			// apply the gain, assumeing using 32-bit floats (no clipping here ;-)
			for (int a=0; a<l4; a++)
				_data[a] = _data[a] * _gainAmplification;

			// copy back from managed to unmanaged memory
			Marshal.Copy(_data, 0, buffer, l4);
		}

		// another alternative in using a DSP callback is using UNSAFE code
		// this allows you to use pointers pretty much like in C/C++!!!
		// this is fast, efficient, but is NOT safe (e.g. no overflow handling, no type checking etc.)
		// But also there is no need to Marshal and Copy any data between managed and unmanaged code
		// So be careful!
		// Also note: you need to compile your app with the /unsafe option!
        unsafe private void MyDSPGainUnsafe(int handle, int channel, IntPtr buffer, int length, IntPtr user)
		{
			if (_gainAmplification == 1f || length == 0 || buffer == IntPtr.Zero)
				return;

			// length is in bytes, so the number of floats to process is:
			// length / 4 : byte = 8-bit, float = 32-bit
			int l4 = length/4;

			float *data = (float*)buffer;
			for (int a=0; a<l4; a++)
			{
				data[a] = data[a] * _gainAmplification;
				// alternatively you can also use:
				// *data = *data * _gainAmplification;
				// data++;  // moves the pointer to the next float
			}
		}

		private void buttonSetGain_Click(object sender, System.EventArgs e)
		{
			try
			{
				this._gainDB = float.Parse( this.textBoxGainDBValue.Text );
				// convert the _gainDB value to a float
				_gainAmplification = (float)Math.Pow(10d, this._gainDB / 20d);
			}
			catch 
			{
				this._gainDB = 0f;
				_gainAmplification = 1f;
			}
		}

		#endregion

		#region Wave Form 

		// zoom helper varibales
		private bool _zoomed = false;
		private int _zoomStart = -1;
		private long _zoomStartBytes = -1;
		private int _zoomEnd = -1;
		private float _zoomDistance = 5.0f; // zoom = 5sec.

		private Un4seen.Bass.Misc.WaveForm WF2 = null;
		private void GetWaveForm()
		{
			// unzoom...(display the whole wave form)
			_zoomStart = -1;
			_zoomStartBytes = -1;
			_zoomEnd = -1;
			_zoomed = false;
			// render a wave form
			WF2 = new WaveForm(this._fileName, new WAVEFORMPROC(MyWaveFormCallback), this);
			WF2.FrameResolution = 0.01f; // 10ms are nice
			WF2.CallbackFrequency = 2000; // every 30 seconds rendered (3000*10ms=30sec)
			WF2.ColorBackground = Color.WhiteSmoke;
			WF2.ColorLeft = Color.Gainsboro;
			WF2.ColorLeftEnvelope = Color.Gray;
			WF2.ColorRight = Color.LightGray;
			WF2.ColorRightEnvelope = Color.DimGray;
			WF2.ColorMarker = Color.DarkBlue;
			WF2.DrawWaveForm = WaveForm.WAVEFORMDRAWTYPE.Stereo;
			WF2.DrawMarker = WaveForm.MARKERDRAWTYPE.Line | WaveForm.MARKERDRAWTYPE.Name | WaveForm.MARKERDRAWTYPE.NamePositionAlternate;
			WF2.MarkerLength = 0.75f;
			// our playing stream will be in 32-bit float!
			// but here we render with 16-bit (default) - just to demo the WF2.SyncPlayback method
			WF2.RenderStart( true, BASSFlag.BASS_DEFAULT );
		}

		private void MyWaveFormCallback(int framesDone, int framesTotal, TimeSpan elapsedTime, bool finished)
		{
			if (finished)
			{
				Console.WriteLine( "Finished rendering in {0}sec.", elapsedTime);
				Console.WriteLine( "FramesRendered={0} of {1}", WF2.FramesRendered, WF2.FramesToRender);
				// eg.g use this to save the rendered wave form...
				//WF.WaveFormSaveToFile( @"C:\test.wf" );

				// auto detect silence at beginning and end
				long cuein  = 0;
				long cueout = 0;
				WF2.GetCuePoints(ref cuein, ref cueout, -25.0, -42.0, -1, -1);
				WF2.AddMarker( "CUE", cuein );
				WF2.AddMarker( "END", cueout );
			}
			// will be called during rendering...
			DrawWave();
		}

		private void pictureBox1_Resize(object sender, System.EventArgs e)
		{
			DrawWave();
		}

		private void DrawWave()
		{
			if (WF2 != null)
				this.pictureBox1.BackgroundImage = WF2.CreateBitmap( this.pictureBox1.Width, this.pictureBox1.Height, _zoomStart, _zoomEnd, true);
			else
				this.pictureBox1.BackgroundImage = null;
		}

		private void DrawWavePosition(long pos, long len)
		{
			// Note: we might take the latency of the device into account here!
			// so we show the position as heard, not played.
			// That's why we called Bass.Bass_Init with the BASS_DEVICE_LATENCY flag
			// and then used the BASS_INFO structure to get the latency of the device

			if (len == 0 || pos < 0)
			{
				this.pictureBox1.Image = null;
				return;
			}

			Bitmap bitmap = null;
			Graphics g = null;
			Pen p = null;
			double bpp = 0;

			try
			{
				if (_zoomed)
				{
					// total length doesn't have to be _zoomDistance sec. here
					len = WF2.Frame2Bytes(_zoomEnd) - _zoomStartBytes;

					int scrollOffset = 10; // 10*20ms = 200ms.
					// if we scroll out the window...(scrollOffset*20ms before the zoom window ends)
					if ( pos > (_zoomStartBytes + len - scrollOffset*WF2.Wave.bpf) )
					{
						// we 'scroll' our zoom with a little offset
						_zoomStart = WF2.Position2Frames(pos - scrollOffset*WF2.Wave.bpf);
						_zoomStartBytes = WF2.Frame2Bytes(_zoomStart);
						_zoomEnd = _zoomStart + WF2.Position2Frames( _zoomDistance ) - 1;
						if (_zoomEnd >= WF2.Wave.data.Length)
						{
							// beyond the end, so we zoom from end - _zoomDistance.
							_zoomEnd = WF2.Wave.data.Length-1;
							_zoomStart = _zoomEnd - WF2.Position2Frames( _zoomDistance ) + 1;
							if (_zoomStart < 0)
								_zoomStart = 0;
							_zoomStartBytes = WF2.Frame2Bytes(_zoomStart);
							// total length doesn't have to be _zoomDistance sec. here
							len = WF2.Frame2Bytes(_zoomEnd) - _zoomStartBytes;
						}
						// get the new wave image for the new zoom window
						DrawWave();
					}
					// zoomed: starts with _zoomStartBytes and is _zoomDistance long
					pos -= _zoomStartBytes; // offset of the zoomed window
					
					bpp = len/(double)this.pictureBox1.Width;  // bytes per pixel
				}
				else
				{
					// not zoomed: width = length of stream
					bpp = len/(double)this.pictureBox1.Width;  // bytes per pixel
				}

				// we take the device latency into account
				// Not really needed, but if you have a real slow device, you might need the next line
				// so the BASS_ChannelGetPosition might return a position ahead of what we hear
				pos -= _deviceLatencyBytes;

				p = new Pen(Color.Red);
				bitmap = new Bitmap(this.pictureBox1.Width, this.pictureBox1.Height);
				g = Graphics.FromImage(bitmap);
				g.Clear( Color.Black );
				int x = (int)Math.Round(pos/bpp);  // position (x) where to draw the line
				g.DrawLine( p, x, 0, x,  this.pictureBox1.Height-1);
				bitmap.MakeTransparent( Color.Black );
			}
			catch 
			{ 
				bitmap = null; 
			}
			finally
			{
				// clean up graphics resources
				if (p != null)
					p.Dispose();
				if (g != null)
					g.Dispose();
			}

			this.pictureBox1.Image = bitmap;
		}

		private void buttonZoom_Click(object sender, System.EventArgs e)
		{
			if (WF2 == null)
				return;

			// WF is not null, so the stream must be playing...
			if (_zoomed)
			{
				// unzoom...(display the whole wave form)
				_zoomStart = -1;
				_zoomStartBytes = -1;
				_zoomEnd = -1;
			}
			else
			{
				// zoom...(display only a partial wave form)
				long pos = Bass.BASS_ChannelGetPosition(this._stream);
				// calculate the window to display
				_zoomStart = WF2.Position2Frames(pos);
				_zoomStartBytes = WF2.Frame2Bytes(_zoomStart);
				_zoomEnd = _zoomStart + WF2.Position2Frames( _zoomDistance ) - 1;
				if (_zoomEnd >= WF2.Wave.data.Length)
				{
					// beyond the end, so we zoom from end - _zoomDistance.
					_zoomEnd = WF2.Wave.data.Length-1;
					_zoomStart = _zoomEnd - WF2.Position2Frames( _zoomDistance ) + 1;
					_zoomStartBytes = WF2.Frame2Bytes(_zoomStart);
				}
			}
			_zoomed = !_zoomed;
			// and display this new wave form
			DrawWave();
		}

		private void pictureBox1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (WF2 == null)
				return;

			long pos = WF2.GetBytePositionFromX( e.X, this.pictureBox1.Width, _zoomStart, _zoomEnd);
			Bass.BASS_ChannelSetPosition(_stream, pos);
		}

		#endregion

		#region Spectrum

		private int specIdx = 15;
		private int voicePrintIdx = 0;
		private void DrawSpectrum()
		{
			switch (specIdx)
			{
					// normal spectrum (width = resolution)
				case 0:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrum(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Black, false, false, false);
					break;
					// normal spectrum (full resolution)
				case 1:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrum(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Black, false, true, true);
					break;
					// line spectrum (width = resolution)
				case 2:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumLine(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Black, 2, 2, false, false, false);
					break;
					// line spectrum (full resolution)
				case 3:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumLine(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Black, 16, 4, false, true, true);
					break;
					// ellipse spectrum (width = resolution)
				case 4:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumEllipse(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Black, 1, 2, false, false, false);
					break;
					// ellipse spectrum (full resolution)
				case 5:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumEllipse(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Black, 2, 4, false, true, true);
					break;
					// dot spectrum (width = resolution)
				case 6:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumDot(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Black, 1, 0, false, false, false);
					break;
					// dot spectrum (full resolution)
				case 7:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumDot(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Black, 2, 1, false, false, true);
					break;
					// peak spectrum (width = resolution)
				case 8:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumLinePeak(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.SeaGreen, Color.LightGreen, Color.Orange, Color.Black, 2, 1, 2, 10, false, false, false);
					break;
					// peak spectrum (full resolution)
				case 9:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumLinePeak(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.GreenYellow, Color.RoyalBlue, Color.DarkOrange, Color.Black, 23, 5, 3, 5, false, true, true);
					break;
					// wave spectrum (width = resolution)
				case 10:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumWave(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Yellow, Color.Orange, Color.Black, 1, false, false, false);
					break;
					// dancing beans spectrum (width = resolution)
				case 11:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumBean(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Chocolate, Color.DarkGoldenrod, Color.Black, 4, false, false, true);
					break;
					// dancing text spectrum (width = resolution)
				case 12:
					this.pictureBoxSpectrum.Image = _vis.CreateSpectrumText(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.White, Color.Tomato, Color.Black, "BASS .NET IS GREAT PIECE! UN4SEEN ROCKS...", false, false, true);
					break;
					// frequency detection
				case 13:
					float amp = _vis.DetectFrequency(_stream, 10, 500, true);
					if (amp > 0.3)
						this.pictureBoxSpectrum.BackColor = Color.Red;
					else
						this.pictureBoxSpectrum.BackColor = Color.Black;
					break;
					// 3D voice print
				case 14:
					// we need to draw directly directly on the picture box...
					// normally you would encapsulate this in your own custom control
					Graphics g = Graphics.FromHwnd(this.pictureBoxSpectrum.Handle);
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					_vis.CreateSpectrum3DVoicePrint(_stream, g, new Rectangle(0,0,this.pictureBoxSpectrum.Width,this.pictureBoxSpectrum.Height), Color.Black, Color.White, voicePrintIdx, false, false);
					g.Dispose();
					// next call will be at the next pos
					voicePrintIdx++;
					if (voicePrintIdx > this.pictureBoxSpectrum.Width-1)
						voicePrintIdx = 0;
					break;
					// WaveForm
				case 15:
					this.pictureBoxSpectrum.Image = _vis.CreateWaveForm(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Green, Color.Red, Color.Gray, Color.Black, 1, true, false, true);
					break;
			}
		}

		private void pictureBoxSpectrum_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				specIdx++;
			else
				specIdx--;

			if (specIdx > 15)
				specIdx = 0;
			if (specIdx < 0)
				specIdx = 15;
			this.labelVis.Text = String.Format( "{0} of 16 (click L/R mouse to change)", specIdx+1);
			this.pictureBoxSpectrum.Image = null;
			_vis.ClearPeaks();
		}


		#endregion Spectrum

	}
}
