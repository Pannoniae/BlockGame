using System;
using System.Text;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Wma;
using Un4seen.Bass.AddOn.Tags;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
	//       needs 'basswma.dll' - copy it to your output directory first!


	public class NetRadio : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.StatusBar statusBar1;
		private System.Windows.Forms.ComboBox comboBoxURL;
		private System.Windows.Forms.TextBox textBoxArtist;
		private System.Windows.Forms.TextBox textBoxTitle;
		private System.Windows.Forms.TextBox textBoxAlbum;
		private System.Windows.Forms.TextBox textBoxComment;
		private System.Windows.Forms.TextBox textBoxGenre;
		private System.Windows.Forms.TextBox textBoxYear;

		private System.ComponentModel.Container components = null;

		public NetRadio()
		{
			InitializeComponent();

            _myUserAgentPtr = Marshal.StringToHGlobalAnsi(_myUserAgent);
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
                Marshal.FreeHGlobal(_myUserAgentPtr);
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
			this.button1 = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.statusBar1 = new System.Windows.Forms.StatusBar();
			this.comboBoxURL = new System.Windows.Forms.ComboBox();
			this.textBoxArtist = new System.Windows.Forms.TextBox();
			this.textBoxTitle = new System.Windows.Forms.TextBox();
			this.textBoxAlbum = new System.Windows.Forms.TextBox();
			this.textBoxComment = new System.Windows.Forms.TextBox();
			this.textBoxGenre = new System.Windows.Forms.TextBox();
			this.textBoxYear = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(16, 40);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(260, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "Connect";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.textBox1.Location = new System.Drawing.Point(16, 72);
			this.textBox1.MaxLength = 2032767;
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new System.Drawing.Size(260, 166);
			this.textBox1.TabIndex = 7;
			this.textBox1.Text = "";
			this.textBox1.WordWrap = false;
			// 
			// statusBar1
			// 
			this.statusBar1.Location = new System.Drawing.Point(0, 244);
			this.statusBar1.Name = "statusBar1";
			this.statusBar1.Size = new System.Drawing.Size(612, 22);
			this.statusBar1.TabIndex = 9;
			// 
			// comboBoxURL
			// 
            this.comboBoxURL.Items.AddRange(new object[] {
            "http://lounge-office.rautemusik.fm",
            "http://www.radioparadise.com/musiclinks/rp_128-9.m3u",
            "http://www.sky.fm/mp3/classical.pls",
            "http://www.sky.fm/mp3/the80s.pls",
            "http://somafm.com/secretagent.pls",
            "http://rautemusik.fm:14100",
            "http://64.236.34.97/stream/1006",
            "http://ogg.smgradio.com/vr160.ogg",
            "mms://a1149.l1305038288.c13050.g.lm.akamaistream.net/D/1149/13050/v0001/reflector" +
                ":38288",
            "http://repc-1.adinjector.net/amtmsvc/gateway.asp?stationid=109&adformat=2"});
			this.comboBoxURL.Location = new System.Drawing.Point(16, 12);
			this.comboBoxURL.Name = "comboBoxURL";
			this.comboBoxURL.Size = new System.Drawing.Size(262, 21);
			this.comboBoxURL.TabIndex = 10;
			// 
			// textBoxArtist
			// 
			this.textBoxArtist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxArtist.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxArtist.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxArtist.Location = new System.Drawing.Point(286, 74);
			this.textBoxArtist.Name = "textBoxArtist";
			this.textBoxArtist.Size = new System.Drawing.Size(316, 13);
			this.textBoxArtist.TabIndex = 11;
			this.textBoxArtist.Text = "Artist";
			// 
			// textBoxTitle
			// 
			this.textBoxTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxTitle.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxTitle.Location = new System.Drawing.Point(286, 98);
			this.textBoxTitle.Name = "textBoxTitle";
			this.textBoxTitle.Size = new System.Drawing.Size(316, 13);
			this.textBoxTitle.TabIndex = 12;
			this.textBoxTitle.Text = "Title";
			// 
			// textBoxAlbum
			// 
			this.textBoxAlbum.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxAlbum.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxAlbum.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxAlbum.Location = new System.Drawing.Point(286, 122);
			this.textBoxAlbum.Name = "textBoxAlbum";
			this.textBoxAlbum.Size = new System.Drawing.Size(316, 13);
			this.textBoxAlbum.TabIndex = 13;
			this.textBoxAlbum.Text = "Album";
			// 
			// textBoxComment
			// 
			this.textBoxComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxComment.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxComment.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxComment.Location = new System.Drawing.Point(286, 146);
			this.textBoxComment.Name = "textBoxComment";
			this.textBoxComment.Size = new System.Drawing.Size(316, 13);
			this.textBoxComment.TabIndex = 14;
			this.textBoxComment.Text = "Comment";
			// 
			// textBoxGenre
			// 
			this.textBoxGenre.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxGenre.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxGenre.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxGenre.Location = new System.Drawing.Point(286, 170);
			this.textBoxGenre.Name = "textBoxGenre";
			this.textBoxGenre.Size = new System.Drawing.Size(316, 13);
			this.textBoxGenre.TabIndex = 15;
			this.textBoxGenre.Text = "Genre";
			// 
			// textBoxYear
			// 
			this.textBoxYear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxYear.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxYear.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxYear.Location = new System.Drawing.Point(286, 194);
			this.textBoxYear.Name = "textBoxYear";
			this.textBoxYear.Size = new System.Drawing.Size(316, 13);
			this.textBoxYear.TabIndex = 16;
			this.textBoxYear.Text = "Year";
			// 
			// NetRadio
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(612, 266);
			this.Controls.Add(this.textBoxYear);
			this.Controls.Add(this.textBoxGenre);
			this.Controls.Add(this.textBoxComment);
			this.Controls.Add(this.textBoxAlbum);
			this.Controls.Add(this.textBoxTitle);
			this.Controls.Add(this.textBoxArtist);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.comboBoxURL);
			this.Controls.Add(this.statusBar1);
			this.Controls.Add(this.button1);
			this.Name = "NetRadio";
			this.Text = "NetRadio";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.NetRadio_Closing);
			this.Load += new System.EventHandler(this.NetRadio_Load);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new NetRadio());
		}

        // PINNED
        private string _myUserAgent = "RADIO42";
        [FixedAddressValueType()]
        public IntPtr _myUserAgentPtr;

		// LOCAL VARS
		private int _Stream = 0;
		private string _url = String.Empty;
		private DOWNLOADPROC myStreamCreateURL;
		private TAG_INFO _tagInfo;
		private SYNCPROC mySync;
		private RECORDPROC myRecProc;
		private int _wmaPlugIn = 0;

        private void NetRadio_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your email", "your regkey");

			// check the version..
			if (Utils.HighWord(Bass.BASS_GetVersion()) != Bass.BASSVERSION)
			{
				MessageBox.Show( this, "Wrong Bass Version!" );
			}

			// stupid thing here as well, just to demo...
			//string userAgent = Bass.BASS_GetConfigString(BASSConfig.BASS_CONFIG_NET_AGENT);

            Bass.BASS_SetConfigPtr(BASSConfig.BASS_CONFIG_NET_AGENT, _myUserAgentPtr);
			
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PREBUF, 0); // so that we can display the buffering%
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PLAYLIST, 1);

			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) )
			{
				// Some words about loading add-ons:
				// In order to set an add-on option with BASS_SetConfig, we need to make sure, that the
				// library (in this case basswma.dll) is actually loaded!
				// However, an external library is dynamically loaded in .NET with the first call 
				// to one of it's methods...
				// As BASS will only know about additional config options once the lib has been loaded,
				// we need to make sure, that the lib is loaded before we make the following call.
				// 1) Loading a lib manually :
				// BassWma.LoadMe();  // basswma.dll must be in same directory
				// 2) Using the BASS PlugIn system (recommended):
				_wmaPlugIn = Bass.BASS_PluginLoad( "basswma.dll" );
				// 3) ALTERNATIVLY you might call any 'dummy' method to load the lib!
				//int[] cbrs = BassWma.BASS_WMA_EncodeGetRates(44100, 2, BASSWMAEncode.BASS_WMA_ENCODE_RATES_CBR);
				// now basswma.dll is loaded and the additional config options are available...

				if ( Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_WMA_BASSFILE, true) == false)
				{
					Console.WriteLine( "ERROR: " + Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()) );
				}
				// we alraedy create the user callback methods...
				myStreamCreateURL = new DOWNLOADPROC(MyDownloadProc);
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void NetRadio_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Bass.BASS_PluginFree( _wmaPlugIn );
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();

            Bass.BASS_PluginFree(_wmaPlugIn);
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			Bass.BASS_StreamFree(_Stream);
			this.textBox1.Text = "";
			_url = this.comboBoxURL.Text;
			// test BASS_StreamCreateURL

			bool isWMA = false;
			if (_url != String.Empty)
			{
				this.textBox1.Text += "URL: "+_url+Environment.NewLine;
				// create the stream
                _Stream = Bass.BASS_StreamCreateURL(_url, 0, BASSFlag.BASS_STREAM_STATUS, myStreamCreateURL, IntPtr.Zero);
				if (_Stream == 0)
				{
					// try WMA streams...
					_Stream = BassWma.BASS_WMA_StreamCreateFile( _url, 0, 0, BASSFlag.BASS_DEFAULT );
					if (_Stream != 0)
						isWMA = true;
					else
					{
						// error
						this.statusBar1.Text = "ERROR...";
						return;
					}
				}
				_tagInfo = new TAG_INFO(_url);
				BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(_Stream);
				if (info.ctype == BASSChannelType.BASS_CTYPE_STREAM_WMA)
					isWMA = true;
				// ok, do some pre-buffering...
				this.statusBar1.Text = "Buffering...";
				if (!isWMA)
				{
					// display buffering for MP3, OGG...
					while (true) 
					{ 
						long len = Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_END);
						if (len == -1)
							break; // typical for WMA streams
						// percentage of buffer filled
						float progress = ( 
							Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD) - 
							Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_CURRENT) 
							) * 100f / len;
                        
						if (progress > 75f) 
						{
							break; // over 75% full, enough
						}

						this.statusBar1.Text = String.Format( "Buffering... {0}%", progress );
					}
				}
				else
				{
					// display buffering for WMA...
					while (true) 
					{ 
						long len = Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_WMA_BUFFER);
						if (len == -1L)
							break;
						// percentage of buffer filled
						if (len > 75L) 
						{
							break; // over 75% full, enough
						}

						this.statusBar1.Text = String.Format( "Buffering... {0}%", len );
					}
				}

				// get the meta tags (manually - will not work for WMA streams here)
				string[] icy = Bass.BASS_ChannelGetTagsICY(_Stream);
				if (icy == null)
				{
					// try http...
					icy = Bass.BASS_ChannelGetTagsHTTP(_Stream);
				}
				if (icy != null)
				{
					foreach (string tag in icy)
					{
						this.textBox1.Text += "ICY: "+tag+Environment.NewLine;
					}
				}
				// get the initial meta data (streamed title...)
				icy = Bass.BASS_ChannelGetTagsMETA(_Stream);
				if (icy != null)
				{
					foreach (string tag in icy)
					{
						this.textBox1.Text += "Meta: "+tag+Environment.NewLine;
					}
				}
				else
				{
					// an ogg stream meta can be obtained here
					icy = Bass.BASS_ChannelGetTagsOGG(_Stream);
					if (icy != null)
					{
						foreach (string tag in icy)
						{
							this.textBox1.Text += "Meta: "+tag+Environment.NewLine;
						}
					}
				}

				// alternatively to the above, you might use the TAG_INFO (see BassTags add-on)
				// This will also work for WMA streams here ;-)
				if ( BassTags.BASS_TAG_GetFromURL( _Stream, _tagInfo) )
				{
					// and display what we get
					this.textBoxAlbum.Text = _tagInfo.album;
					this.textBoxArtist.Text = _tagInfo.artist;
					this.textBoxTitle.Text = _tagInfo.title;
					this.textBoxComment.Text = _tagInfo.comment;
					this.textBoxGenre.Text = _tagInfo.genre;
					this.textBoxYear.Text = _tagInfo.year;
				}

				// set a sync to get the title updates out of the meta data...
				mySync = new SYNCPROC(MetaSync);
                Bass.BASS_ChannelSetSync(_Stream, BASSSync.BASS_SYNC_META, 0, mySync, IntPtr.Zero);
                Bass.BASS_ChannelSetSync(_Stream, BASSSync.BASS_SYNC_WMA_CHANGE, 0, mySync, IntPtr.Zero);
				
				// start recording...
				int rechandle = 0;
				if ( Bass.BASS_RecordInit(-1) )
				{
					_byteswritten = 0;
					myRecProc = new RECORDPROC(MyRecoring);
                    rechandle = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_RECORD_PAUSE, myRecProc, IntPtr.Zero);
				}
				this.statusBar1.Text = "Playling...";
				// play the stream
				Bass.BASS_ChannelPlay(_Stream, false);
				// record the stream
				Bass.BASS_ChannelPlay(rechandle, false);
			}
		}

		private int _byteswritten = 0;
		private byte[] _recbuffer = new byte[1048510]; // 1MB buffer
        private bool MyRecoring(int handle, IntPtr buffer, int length, IntPtr user)
		{
			// just a dummy here...nothing is really written to disk...
			if (length > 0 && buffer != IntPtr.Zero)
			{
				// copy from managed to unmanaged memory
				// it is clever to NOT alloc the byte[] everytime here, since ALL callbacks should be really fast!
				// and if you would do a 'new byte[]' every time here...the GarbageCollector would never really clean up that memory here
				// even other sideeffects might occure, due to the fact, that BASS micht call this callback too fast and too often...
				Marshal.Copy(buffer, _recbuffer, 0, length);
				// write to file
				// NOT implemented her...;-)
				_byteswritten += length;
				Console.WriteLine( "Bytes written = {0}", _byteswritten);
				if (_byteswritten < 800000)
					return true; // continue recording
				else
					return false;
			}
			return true;
		}

        private void MyDownloadProc(IntPtr buffer, int length, IntPtr user)
		{
			if (buffer != IntPtr.Zero && length == 0)
			{
				// the buffer contains HTTP or ICY tags.
				string txt = Marshal.PtrToStringAnsi(buffer);
				this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { txt });
				// you might instead also use "this.BeginInvoke(...)", which would call the delegate asynchron!
			}
		}

        private void MetaSync(int handle, int channel, int data, IntPtr user)
        {
            // BASS_SYNC_META is triggered on meta changes of SHOUTcast streams
            if (_tagInfo.UpdateFromMETA(Bass.BASS_ChannelGetTags(channel, BASSTag.BASS_TAG_META), false, true))
            {
                this.Invoke(new UpdateTagDelegate(UpdateTagDisplay));
            }
        }

        public delegate void UpdateTagDelegate();
        private void UpdateTagDisplay()
        {
            this.textBoxAlbum.Text = _tagInfo.album;
            this.textBoxArtist.Text = _tagInfo.artist;
            this.textBoxTitle.Text = _tagInfo.title;
            this.textBoxComment.Text = _tagInfo.comment;
            this.textBoxGenre.Text = _tagInfo.genre;
            this.textBoxYear.Text = _tagInfo.year;
        }

        public delegate void UpdateStatusDelegate(string txt);
        private void UpdateStatusDisplay(string txt)
        {
            this.statusBar1.Text = txt;
        }

		public delegate void UpdateMessageDelegate(string txt);
		private void UpdateMessageDisplay(string txt)
		{
			this.textBox1.Text += "Tags: " + txt + Environment.NewLine;
		}

	}
}
