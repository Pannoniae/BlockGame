using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Un4seen.Bass.AddOn.Tags;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
	// Need a directory in which additional stream support Add-Ons are located


	public class PlugIn : System.Windows.Forms.Form
	{
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button buttonPlay;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxYear;
		private System.Windows.Forms.TextBox textBoxGenre;
		private System.Windows.Forms.TextBox textBoxComment;
		private System.Windows.Forms.TextBox textBoxAlbum;
		private System.Windows.Forms.TextBox textBoxTitle;
		private System.Windows.Forms.TextBox textBoxArtist;
		private System.Windows.Forms.Timer timerBPM;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label labelBPM;
		private System.Windows.Forms.Button buttonTapBPM;
		private System.ComponentModel.IContainer components;

		private BPMCounter _bpm = new BPMCounter(20, 44100);
		private System.Windows.Forms.PictureBox pictureBoxSpectrum;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private Visuals _vis = new Visuals();

		public PlugIn()
		{
			InitializeComponent();

			// set the BPM history buffer to 8
			_bpm.BPMHistorySize = 8;
			// setting the BPM range (60-180 is a range most analog DJ Mixers do have)
			_bpm.MinBPM = 60;
			_bpm.MaxBPM = 180;

			// limit the spectrum to around 15kHz (exaclty 15.073Hz) - which contain most hearable freq.
			_vis.MaxFrequencySpectrum = 1400; // assuming 44.1kHz samplerate here ;-)
			_vis.ScaleFactorSqr = 5;
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
			this.buttonPlay = new System.Windows.Forms.Button();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.button2 = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxYear = new System.Windows.Forms.TextBox();
			this.textBoxGenre = new System.Windows.Forms.TextBox();
			this.textBoxComment = new System.Windows.Forms.TextBox();
			this.textBoxAlbum = new System.Windows.Forms.TextBox();
			this.textBoxTitle = new System.Windows.Forms.TextBox();
			this.textBoxArtist = new System.Windows.Forms.TextBox();
			this.timerBPM = new System.Windows.Forms.Timer(this.components);
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.buttonTapBPM = new System.Windows.Forms.Button();
			this.labelBPM = new System.Windows.Forms.Label();
			this.pictureBoxSpectrum = new System.Windows.Forms.PictureBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// openFileDialog
			// 
			this.openFileDialog.Filter = "Audio Files (*.*)|*.*";
			this.openFileDialog.Title = "Select an audio file to play";
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button1.Location = new System.Drawing.Point(16, 200);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(260, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "2. Select a file to play";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// buttonPlay
			// 
			this.buttonPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonPlay.Location = new System.Drawing.Point(16, 230);
			this.buttonPlay.Name = "buttonPlay";
			this.buttonPlay.Size = new System.Drawing.Size(260, 23);
			this.buttonPlay.TabIndex = 8;
			this.buttonPlay.Text = "3. Play";
			this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
			// 
			// listBox1
			// 
			this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.listBox1.Location = new System.Drawing.Point(16, 46);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(260, 147);
			this.listBox1.TabIndex = 9;
			this.toolTip1.SetToolTip(this.listBox1, "Double-Click on an item to display the supported format information");
			this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(16, 14);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(260, 23);
			this.button2.TabIndex = 10;
			this.button2.Text = "1. Select a plugin folder (containing Add-Ons)";
			this.button2.Click += new System.EventHandler(this.button2_Click);
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
			this.label1.Location = new System.Drawing.Point(16, 258);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(572, 23);
			this.label1.TabIndex = 11;
			// 
			// textBoxYear
			// 
			this.textBoxYear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxYear.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxYear.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxYear.Location = new System.Drawing.Point(286, 170);
			this.textBoxYear.Name = "textBoxYear";
			this.textBoxYear.Size = new System.Drawing.Size(316, 13);
			this.textBoxYear.TabIndex = 22;
			this.textBoxYear.Text = "Year";
			// 
			// textBoxGenre
			// 
			this.textBoxGenre.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxGenre.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxGenre.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxGenre.Location = new System.Drawing.Point(286, 146);
			this.textBoxGenre.Name = "textBoxGenre";
			this.textBoxGenre.Size = new System.Drawing.Size(316, 13);
			this.textBoxGenre.TabIndex = 21;
			this.textBoxGenre.Text = "Genre";
			// 
			// textBoxComment
			// 
			this.textBoxComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxComment.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxComment.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxComment.Location = new System.Drawing.Point(286, 122);
			this.textBoxComment.Name = "textBoxComment";
			this.textBoxComment.Size = new System.Drawing.Size(316, 13);
			this.textBoxComment.TabIndex = 20;
			this.textBoxComment.Text = "Comment";
			// 
			// textBoxAlbum
			// 
			this.textBoxAlbum.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxAlbum.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxAlbum.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxAlbum.Location = new System.Drawing.Point(286, 98);
			this.textBoxAlbum.Name = "textBoxAlbum";
			this.textBoxAlbum.Size = new System.Drawing.Size(316, 13);
			this.textBoxAlbum.TabIndex = 19;
			this.textBoxAlbum.Text = "Album";
			// 
			// textBoxTitle
			// 
			this.textBoxTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTitle.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxTitle.Location = new System.Drawing.Point(286, 74);
			this.textBoxTitle.Name = "textBoxTitle";
			this.textBoxTitle.Size = new System.Drawing.Size(316, 13);
			this.textBoxTitle.TabIndex = 18;
			this.textBoxTitle.Text = "Title";
			// 
			// textBoxArtist
			// 
			this.textBoxArtist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxArtist.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxArtist.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxArtist.Location = new System.Drawing.Point(286, 50);
			this.textBoxArtist.Name = "textBoxArtist";
			this.textBoxArtist.Size = new System.Drawing.Size(316, 13);
			this.textBoxArtist.TabIndex = 17;
			this.textBoxArtist.Text = "Artist";
			// 
			// timerBPM
			// 
			this.timerBPM.Interval = 20;
			this.timerBPM.Tick += new System.EventHandler(this.timerBPM_Tick);
			// 
			// pictureBox1
			// 
			this.pictureBox1.BackColor = System.Drawing.Color.Black;
			this.pictureBox1.Location = new System.Drawing.Point(498, 205);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(73, 25);
			this.pictureBox1.TabIndex = 23;
			this.pictureBox1.TabStop = false;
			this.toolTip1.SetToolTip(this.pictureBox1, "Click here to reset the live BPM detection");
			this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
			// 
			// buttonTapBPM
			// 
			this.buttonTapBPM.Location = new System.Drawing.Point(390, 230);
			this.buttonTapBPM.Name = "buttonTapBPM";
			this.buttonTapBPM.Size = new System.Drawing.Size(94, 23);
			this.buttonTapBPM.TabIndex = 24;
			this.buttonTapBPM.Text = "Tapped BPM";
			this.toolTip1.SetToolTip(this.buttonTapBPM, "Click here to tap your BPM manually");
			this.buttonTapBPM.MouseDown += new System.Windows.Forms.MouseEventHandler(this.buttonTapBPM_MouseDown);
			// 
			// labelBPM
			// 
			this.labelBPM.Location = new System.Drawing.Point(390, 206);
			this.labelBPM.Name = "labelBPM";
			this.labelBPM.Size = new System.Drawing.Size(94, 18);
			this.labelBPM.TabIndex = 25;
			this.labelBPM.Text = "Live BPM";
			this.labelBPM.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// pictureBoxSpectrum
			// 
			this.pictureBoxSpectrum.BackColor = System.Drawing.Color.Black;
			this.pictureBoxSpectrum.Location = new System.Drawing.Point(498, 231);
			this.pictureBoxSpectrum.Name = "pictureBoxSpectrum";
			this.pictureBoxSpectrum.Size = new System.Drawing.Size(73, 23);
			this.pictureBoxSpectrum.TabIndex = 26;
			this.pictureBoxSpectrum.TabStop = false;
			this.toolTip1.SetToolTip(this.pictureBoxSpectrum, "Click here to reset the live BPM detection");
			this.pictureBoxSpectrum.Click += new System.EventHandler(this.pictureBoxSpectrum_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(310, 204);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(68, 23);
			this.label2.TabIndex = 27;
			this.label2.Text = "Live BPM:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(310, 230);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(74, 23);
			this.label3.TabIndex = 28;
			this.label3.Text = "Tapped BPM:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// PlugIn
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(612, 278);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.pictureBoxSpectrum);
			this.Controls.Add(this.labelBPM);
			this.Controls.Add(this.buttonTapBPM);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.textBoxYear);
			this.Controls.Add(this.textBoxGenre);
			this.Controls.Add(this.textBoxComment);
			this.Controls.Add(this.textBoxAlbum);
			this.Controls.Add(this.textBoxTitle);
			this.Controls.Add(this.textBoxArtist);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.buttonPlay);
			this.Controls.Add(this.button1);
			this.Name = "PlugIn";
			this.Text = "PlugIn Test";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.PlugIn_Closing);
			this.Load += new System.EventHandler(this.PlugIn_Load);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new PlugIn());
		}

		// LOCAL VARS
		private int _Stream = 0;
		private string _FileName = String.Empty;
		private TAG_INFO _tagInfo;

		private void PlugIn_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your email", "your regkey");

			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) )
			{
				// all fine
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void PlugIn_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// unload all loaded add-ons...
			Bass.BASS_PluginFree(0);
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.label1.Text = "";
			this.openFileDialog.FileName = _FileName;
			if ( DialogResult.OK == this.openFileDialog.ShowDialog(this) )
			{
				if ( File.Exists(this.openFileDialog.FileName) )
					_FileName = this.openFileDialog.FileName;
				else
					_FileName = String.Empty;
			}
		}

		private void buttonPlay_Click(object sender, System.EventArgs e)
		{
			this.label1.Text = "";
			Bass.BASS_StreamFree(_Stream);
			this.timerBPM.Stop();

			// test PlugIns...
			// after additional Add-Ons have been loaded,
			// they will be supported in the standard BASS_StreamCreateFile methods
			if (_FileName != String.Empty)
			{
				// create the stream
				_Stream = Bass.BASS_StreamCreateFile(_FileName, 0, 0, BASSFlag.BASS_DEFAULT);

				// update the tags
				_tagInfo = new TAG_INFO(_FileName);
				if ( BassTags.BASS_TAG_GetFromFile( _Stream, _tagInfo) )
				{
					// and display what we get
					this.textBoxAlbum.Text = _tagInfo.album;
					this.textBoxArtist.Text = _tagInfo.artist;
					this.textBoxTitle.Text = _tagInfo.title;
					this.textBoxComment.Text = _tagInfo.comment;
					this.textBoxGenre.Text = _tagInfo.genre;
					this.textBoxYear.Text = _tagInfo.year;
				}

				// play the stream
				if (_Stream != 0 && Bass.BASS_ChannelPlay(_Stream, false) )
				{
					//playing...
					BASS_CHANNELINFO info = new BASS_CHANNELINFO();
					if ( Bass.BASS_ChannelGetInfo(_Stream, info) )
					{
						// start the BPMCounter
						_bpm.Reset(info.freq);
						this.timerBPM.Start();

						// display the channel info..
						this.label1.Text = String.Format( "Type={0}, Channels={1}, OrigRes={2}", Utils.BASSChannelTypeToString(info.ctype), info.chans, info.origres);
					}
				}
				else
				{
					MessageBox.Show( this, "Error: "+Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()) );
				}
			}
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			folderBrowserDialog.SelectedPath = Application.StartupPath;
			// this will try to load all bass*.dll add-ons from that directory...
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
			{
				this.listBox1.Items.Clear();
				string dir = folderBrowserDialog.SelectedPath;
				// load all (bass*.dll) add-ons from the given directory
				this.Cursor = Cursors.WaitCursor;
                Dictionary<int, string> loadedPlugIns = Bass.BASS_PluginLoadDirectory(dir);
				openFileDialog.Filter = Utils.BASSAddOnGetSupportedFileFilter(loadedPlugIns, "All supported Audio Files", true);
				this.Cursor = Cursors.Default;
				// lets see what we have received...
				if (loadedPlugIns != null)
				{
					foreach (string file in loadedPlugIns.Values)
					{
						this.listBox1.Items.Add( file );
					}
					foreach (int plugin in loadedPlugIns.Keys)
					{
						BASS_PLUGININFO pluginInfo = Bass.BASS_PluginGetInfo(plugin);
						// or this might also be used as an alternative:
						//BASS_PLUGININFO pluginInfo = new BASS_PLUGININFO( Bass.BASS_PluginGetInfoPtr(plugin) );
						foreach (BASS_PLUGINFORM formats in pluginInfo.formats)
						{
							Console.WriteLine( formats.ToString() );
						}
					}
					Console.WriteLine( Utils.BASSAddOnGetPluginFileFilter(loadedPlugIns, "All supported Audio Files", true) );
					Console.WriteLine( Utils.BASSAddOnGetPluginFileFilter(loadedPlugIns, null, false) );
				}
				else
					MessageBox.Show( this, "No BASS add-on loaded!" );
			}

		}

		private void listBox1_DoubleClick(object sender, System.EventArgs e)
		{
			if (this.listBox1.SelectedItem != null)
			{
				string file = this.listBox1.SelectedItem as string;
				if (file != null)
				{
					MessageBox.Show( this, String.Format( "{0}\nName={1}\nExtensions={2}", file, Utils.BASSAddOnGetSupportedFileName(file), Utils.BASSAddOnGetSupportedFileExtensions(file) ) );
				}
			}
		}

		private int counter = 0;
		private void timerBPM_Tick(object sender, System.EventArgs e)
		{
			counter++;
			if ( _Stream == 0 || Bass.BASS_ChannelIsActive(_Stream) != BASSActive.BASS_ACTIVE_PLAYING)
			{
				this.timerBPM.Stop();
				return;
			}
			bool beat = _bpm.ProcessAudio(_Stream, false);
			if (beat)
			{
				// display the live calculated BPM value
				this.labelBPM.Text = _bpm.BPM.ToString( "#00.0" );
				this.pictureBox1.BackColor = Color.Red;
			}
			else
			{
				this.pictureBox1.BackColor = Color.Black;
			}

			// and the little spectrum (calles every 2 times = 40ms)
			if (counter == 2)
			{
				this.pictureBoxSpectrum.Image = _vis.CreateSpectrumLinePeak(_Stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.FromArgb(255,226,173), Color.FromArgb(250,181,63), Color.FromArgb(150,150,150), Color.Black, 3, 1, 1, 10, false, true, false);
				counter = 0;
			}
		}

		private void pictureBox1_Click(object sender, System.EventArgs e)
		{
			if ( _Stream == 0 || Bass.BASS_ChannelIsActive(_Stream) != BASSActive.BASS_ACTIVE_PLAYING)
			{
				// not playing anymore...
				return;
			}
			BASS_CHANNELINFO info = new BASS_CHANNELINFO();
			if ( Bass.BASS_ChannelGetInfo(_Stream, info) )
				_bpm.Reset(info.freq);
		}

		private void pictureBoxSpectrum_Click(object sender, System.EventArgs e)
		{
			_vis.ClearPeaks();
		}

		private void buttonTapBPM_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			_bpm.TapBeat();
			this.buttonTapBPM.Text = _bpm.TappedBPM.ToString( "#00.0" );
		}


	}
}
