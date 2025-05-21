using System;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass.Misc;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
    //       Needs 'bassmix.dll' - copy it to your output directory first!


	public class SimpleMix : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonAddFile;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.ProgressBar progressBarLeft;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ProgressBar progressBarRight;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelTime;
        private ListBox listBoxPlaylist;
        private System.Windows.Forms.Timer timerUpdate;
        private IContainer components;
        private Label labelArtist;
        private Label labelTitle;
        private Label labelRemain;
        private Label label3;
        private Label label4;
        private Button buttonSetEnvelope;
        private Button buttonRemoveEnvelope;
		private System.Windows.Forms.PictureBox pictureBoxWaveForm;

		public SimpleMix()
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
            this.components = new System.ComponentModel.Container();
            this.buttonAddFile = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.progressBarLeft = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBarRight = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.labelTime = new System.Windows.Forms.Label();
            this.pictureBoxWaveForm = new System.Windows.Forms.PictureBox();
            this.listBoxPlaylist = new System.Windows.Forms.ListBox();
            this.timerUpdate = new System.Windows.Forms.Timer(this.components);
            this.labelArtist = new System.Windows.Forms.Label();
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelRemain = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonSetEnvelope = new System.Windows.Forms.Button();
            this.buttonRemoveEnvelope = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWaveForm)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonAddFile
            // 
            this.buttonAddFile.Location = new System.Drawing.Point(12, 242);
            this.buttonAddFile.Name = "buttonAddFile";
            this.buttonAddFile.Size = new System.Drawing.Size(206, 23);
            this.buttonAddFile.TabIndex = 0;
            this.buttonAddFile.Text = "Add Track to Playlist...";
            this.buttonAddFile.Click += new System.EventHandler(this.buttonAddFile_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav";
            this.openFileDialog.Title = "Select an audio file to play";
            // 
            // progressBarLeft
            // 
            this.progressBarLeft.Location = new System.Drawing.Point(391, 12);
            this.progressBarLeft.Maximum = 32768;
            this.progressBarLeft.Name = "progressBarLeft";
            this.progressBarLeft.Size = new System.Drawing.Size(181, 12);
            this.progressBarLeft.Step = 1;
            this.progressBarLeft.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(378, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(16, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "L";
            // 
            // progressBarRight
            // 
            this.progressBarRight.Location = new System.Drawing.Point(391, 28);
            this.progressBarRight.Maximum = 32768;
            this.progressBarRight.Name = "progressBarRight";
            this.progressBarRight.Size = new System.Drawing.Size(181, 12);
            this.progressBarRight.Step = 1;
            this.progressBarRight.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(378, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(16, 12);
            this.label2.TabIndex = 10;
            this.label2.Text = "R";
            // 
            // labelTime
            // 
            this.labelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTime.Location = new System.Drawing.Point(236, 27);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(65, 15);
            this.labelTime.TabIndex = 12;
            this.labelTime.Text = "00:00:00";
            this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pictureBoxWaveForm
            // 
            this.pictureBoxWaveForm.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pictureBoxWaveForm.ErrorImage = null;
            this.pictureBoxWaveForm.InitialImage = null;
            this.pictureBoxWaveForm.Location = new System.Drawing.Point(12, 46);
            this.pictureBoxWaveForm.Name = "pictureBoxWaveForm";
            this.pictureBoxWaveForm.Size = new System.Drawing.Size(560, 63);
            this.pictureBoxWaveForm.TabIndex = 15;
            this.pictureBoxWaveForm.TabStop = false;
            this.pictureBoxWaveForm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxWaveForm_MouseDown);
            // 
            // listBoxPlaylist
            // 
            this.listBoxPlaylist.FormattingEnabled = true;
            this.listBoxPlaylist.Location = new System.Drawing.Point(12, 115);
            this.listBoxPlaylist.Name = "listBoxPlaylist";
            this.listBoxPlaylist.Size = new System.Drawing.Size(560, 121);
            this.listBoxPlaylist.TabIndex = 16;
            // 
            // timerUpdate
            // 
            this.timerUpdate.Interval = 50;
            this.timerUpdate.Tick += new System.EventHandler(this.timerUpdate_Tick);
            // 
            // labelArtist
            // 
            this.labelArtist.Location = new System.Drawing.Point(12, 9);
            this.labelArtist.Name = "labelArtist";
            this.labelArtist.Size = new System.Drawing.Size(218, 13);
            this.labelArtist.TabIndex = 8;
            this.labelArtist.Text = "Artist";
            // 
            // labelTitle
            // 
            this.labelTitle.Location = new System.Drawing.Point(12, 27);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(218, 13);
            this.labelTitle.TabIndex = 10;
            this.labelTitle.Text = "Title";
            // 
            // labelRemain
            // 
            this.labelRemain.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRemain.Location = new System.Drawing.Point(307, 27);
            this.labelRemain.Name = "labelRemain";
            this.labelRemain.Size = new System.Drawing.Size(65, 15);
            this.labelRemain.TabIndex = 12;
            this.labelRemain.Text = "00:00:00";
            this.labelRemain.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(236, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Elapsed";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(307, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Remain";
            // 
            // buttonSetEnvelope
            // 
            this.buttonSetEnvelope.Location = new System.Drawing.Point(346, 242);
            this.buttonSetEnvelope.Name = "buttonSetEnvelope";
            this.buttonSetEnvelope.Size = new System.Drawing.Size(110, 23);
            this.buttonSetEnvelope.TabIndex = 0;
            this.buttonSetEnvelope.Text = "Set Envelope";
            this.buttonSetEnvelope.Click += new System.EventHandler(this.buttonSetEnvelope_Click);
            // 
            // buttonRemoveEnvelope
            // 
            this.buttonRemoveEnvelope.Location = new System.Drawing.Point(462, 242);
            this.buttonRemoveEnvelope.Name = "buttonRemoveEnvelope";
            this.buttonRemoveEnvelope.Size = new System.Drawing.Size(110, 23);
            this.buttonRemoveEnvelope.TabIndex = 0;
            this.buttonRemoveEnvelope.Text = "Remove Envelope";
            this.buttonRemoveEnvelope.Click += new System.EventHandler(this.buttonRemoveEnvelope_Click);
            // 
            // SimpleMix
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(584, 276);
            this.Controls.Add(this.listBoxPlaylist);
            this.Controls.Add(this.pictureBoxWaveForm);
            this.Controls.Add(this.labelRemain);
            this.Controls.Add(this.labelTime);
            this.Controls.Add(this.progressBarRight);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.progressBarLeft);
            this.Controls.Add(this.labelArtist);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonRemoveEnvelope);
            this.Controls.Add(this.buttonSetEnvelope);
            this.Controls.Add(this.buttonAddFile);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "SimpleMix";
            this.Text = "Simple Mixer";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Simple_Closing);
            this.Load += new System.EventHandler(this.Simple_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWaveForm)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

        // private VARs
        private int _mixer = 0;
        private SYNCPROC _mixerStallSync;
        private Track _currentTrack = null;
        private Track _previousTrack = null;

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new SimpleMix());
		}

		private void Simple_Load(object sender, System.EventArgs e)
		{
			// BassNet.Registration("your email", "your regkey");

            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle))
            {
                MessageBox.Show(this, "Bass_Init error!");
                this.Close();
                return;
            }
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 200);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 20);

            // already create a mixer
            _mixer = BassMix.BASS_Mixer_StreamCreate(44100, 2, BASSFlag.BASS_SAMPLE_FLOAT);
            if (_mixer == 0)
            {
                MessageBox.Show(this, "Could not create mixer!");
                Bass.BASS_Free();
                this.Close();
                return;
            }

            _mixerStallSync = new SYNCPROC(OnMixerStall);
            Bass.BASS_ChannelSetSync(_mixer, BASSSync.BASS_SYNC_STALL, 0L, _mixerStallSync, IntPtr.Zero);

            timerUpdate.Start();
            Bass.BASS_ChannelPlay(_mixer, false);
		}

		private void Simple_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            timerUpdate.Stop();
			// close bass
            Bass.BASS_StreamFree(_mixer);
			Bass.BASS_Free();
		}

        private void OnMixerStall(int handle, int channel, int data, IntPtr user)
        {
            BeginInvoke((MethodInvoker)delegate()
            {
                // this code runs on the UI thread!
                if (data == 0)
                {
                    // mixer stalled
                    timerUpdate.Stop();
                    progressBarLeft.Value = 0;
                    progressBarRight.Value = 0;
                }
                else
                {
                    // mixer resumed
                    timerUpdate.Start();
                }
            });
        }

        private void PlayNextTrack()
        {
            lock (listBoxPlaylist)
            {
                // get the next track to play
                if (listBoxPlaylist.Items.Count > 0)
                {
                    _previousTrack = _currentTrack;
                    _currentTrack = listBoxPlaylist.Items[0] as Track;

                    listBoxPlaylist.Items.RemoveAt(0);

                    // the channel was already added
                    // so for instant playback, we just unpause the channel
                    BassMix.BASS_Mixer_ChannelPlay(_currentTrack.Channel);

                    labelTitle.Text = _currentTrack.Tags.title;
                    labelArtist.Text = _currentTrack.Tags.artist;

                    // get the waveform for that track
                    GetWaveForm();
                }
            }
        }

        private void OnTrackSync(int handle, int channel, int data, IntPtr user)
        {
            if (user.ToInt32() == 0)
            {
                // END SYNC
                BeginInvoke(new MethodInvoker(PlayNextTrack));
            }
            else
            {
                // POS SYNC
                BeginInvoke((MethodInvoker)delegate()
                {
                    // this code runs on the UI thread!
                    PlayNextTrack();
                    // and fade out and stop the 'previous' track (for 4 seconds)
                    if (_previousTrack != null)
                        Bass.BASS_ChannelSlideAttribute(_previousTrack.Channel, BASSAttribute.BASS_ATTRIB_VOL, -1f, 4000);
                });
            }
        }
	
		#region Wave Form 

		// zoom helper varibales
		private bool _zoomed = false;
		private int _zoomStart = -1;
		private long _zoomStartBytes = -1;
		private int _zoomEnd = -1;
		private float _zoomDistance = 5.0f; // zoom = 5sec.

		private Un4seen.Bass.Misc.WaveForm _WF = null;
		private void GetWaveForm()
		{
			// unzoom...(display the whole wave form)
			_zoomStart = -1;
			_zoomStartBytes = -1;
			_zoomEnd = -1;
			_zoomed = false;
			// render a wave form
			_WF = new WaveForm(_currentTrack.Filename, new WAVEFORMPROC(MyWaveFormCallback), this);
			_WF.FrameResolution = 0.01f; // 10ms are nice
            _WF.CallbackFrequency = 30000; // every 5min.
            _WF.ColorBackground = Color.FromArgb(20, 20, 20);
            _WF.ColorLeft = Color.Gray;
            _WF.ColorLeftEnvelope = Color.LightGray;
            _WF.ColorRight = Color.Gray;
            _WF.ColorRightEnvelope = Color.LightGray;
            _WF.ColorMarker = Color.Gold;
            _WF.ColorBeat = Color.LightSkyBlue;
            _WF.ColorVolume = Color.White;
            _WF.DrawEnvelope = false;
			_WF.DrawWaveForm = WaveForm.WAVEFORMDRAWTYPE.HalfMono;
			_WF.DrawMarker = WaveForm.MARKERDRAWTYPE.Line | WaveForm.MARKERDRAWTYPE.Name | WaveForm.MARKERDRAWTYPE.NamePositionAlternate;
			_WF.MarkerLength = 0.75f;
			_WF.RenderStart( true, BASSFlag.BASS_DEFAULT );
		}

		private void MyWaveFormCallback(int framesDone, int framesTotal, TimeSpan elapsedTime, bool finished)
		{
			if (finished)
			{
                _WF.SyncPlayback(_currentTrack.Channel);

                // and do pre-calculate the next track position
                // in this example we will only use the end-position
                long startPos = 0L;
                long endPos = 0L;
                if (_WF.GetCuePoints(ref startPos, ref endPos, -24.0, -12.0, true))
                {
                    _currentTrack.NextTrackPos = endPos;
                    // if there is already a sync set, remove it first
                    if (_currentTrack.NextTrackSync != 0)
                        BassMix.BASS_Mixer_ChannelRemoveSync(_currentTrack.Channel, _currentTrack.NextTrackSync);

                    // set the next track sync automatically
                    _currentTrack.NextTrackSync = BassMix.BASS_Mixer_ChannelSetSync(_currentTrack.Channel, BASSSync.BASS_SYNC_POS | BASSSync.BASS_SYNC_MIXTIME, _currentTrack.NextTrackPos, _currentTrack.TrackSync, new IntPtr(1));
                    
                    _WF.AddMarker("Next", _currentTrack.NextTrackPos);
                }
			}
			// will be called during rendering...
			DrawWave();
		}

		private void DrawWave()
		{
			if (_WF != null)
				this.pictureBoxWaveForm.BackgroundImage = _WF.CreateBitmap( this.pictureBoxWaveForm.Width, this.pictureBoxWaveForm.Height, _zoomStart, _zoomEnd, true);
			else
				this.pictureBoxWaveForm.BackgroundImage = null;
		}

		private void DrawWavePosition(long pos, long len)
		{
			if (_WF == null || len == 0 || pos < 0)
			{
				this.pictureBoxWaveForm.Image = null;
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
					len = _WF.Frame2Bytes(_zoomEnd) - _zoomStartBytes;

					int scrollOffset = 10; // 10*20ms = 200ms.
					// if we scroll out the window...(scrollOffset*20ms before the zoom window ends)
					if ( pos > (_zoomStartBytes + len - scrollOffset*_WF.Wave.bpf) )
					{
						// we 'scroll' our zoom with a little offset
						_zoomStart = _WF.Position2Frames(pos - scrollOffset*_WF.Wave.bpf);
						_zoomStartBytes = _WF.Frame2Bytes(_zoomStart);
						_zoomEnd = _zoomStart + _WF.Position2Frames( _zoomDistance ) - 1;
						if (_zoomEnd >= _WF.Wave.data.Length)
						{
							// beyond the end, so we zoom from end - _zoomDistance.
							_zoomEnd = _WF.Wave.data.Length-1;
							_zoomStart = _zoomEnd - _WF.Position2Frames( _zoomDistance ) + 1;
							if (_zoomStart < 0)
								_zoomStart = 0;
							_zoomStartBytes = _WF.Frame2Bytes(_zoomStart);
							// total length doesn't have to be _zoomDistance sec. here
							len = _WF.Frame2Bytes(_zoomEnd) - _zoomStartBytes;
						}
						// get the new wave image for the new zoom window
						DrawWave();
					}
					// zoomed: starts with _zoomStartBytes and is _zoomDistance long
					pos -= _zoomStartBytes; // offset of the zoomed window
					
					bpp = len/(double)this.pictureBoxWaveForm.Width;  // bytes per pixel
				}
				else
				{
					// not zoomed: width = length of stream
					bpp = len/(double)this.pictureBoxWaveForm.Width;  // bytes per pixel
				}

				p = new Pen(Color.Red);
				bitmap = new Bitmap(this.pictureBoxWaveForm.Width, this.pictureBoxWaveForm.Height);
				g = Graphics.FromImage(bitmap);
				g.Clear( Color.Black );
				int x = (int)Math.Round(pos/bpp);  // position (x) where to draw the line
				g.DrawLine( p, x, 0, x,  this.pictureBoxWaveForm.Height-1);
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

			this.pictureBoxWaveForm.Image = bitmap;
		}

        private void ToggleZoom()
        {
            if (_WF == null)
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
                long pos = BassMix.BASS_Mixer_ChannelGetPosition(_currentTrack.Channel);
                // calculate the window to display
                _zoomStart = _WF.Position2Frames(pos);
                _zoomStartBytes = _WF.Frame2Bytes(_zoomStart);
                _zoomEnd = _zoomStart + _WF.Position2Frames(_zoomDistance) - 1;
                if (_zoomEnd >= _WF.Wave.data.Length)
                {
                    // beyond the end, so we zoom from end - _zoomDistance.
                    _zoomEnd = _WF.Wave.data.Length - 1;
                    _zoomStart = _zoomEnd - _WF.Position2Frames(_zoomDistance) + 1;
                    _zoomStartBytes = _WF.Frame2Bytes(_zoomStart);
                }
            }
            _zoomed = !_zoomed;
            // and display this new wave form
            DrawWave();
        }

        private void pictureBoxWaveForm_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_WF == null)
                return;

            bool doubleClick = e.Clicks > 1;
            bool lowerHalf = (e.Y > pictureBoxWaveForm.Height / 2);

            if (lowerHalf && doubleClick)
            {
                ToggleZoom();
            }
            else if (!lowerHalf && e.Button == MouseButtons.Left)
            {
                // left button will set the position
                long pos = _WF.GetBytePositionFromX(e.X, pictureBoxWaveForm.Width, _zoomStart, _zoomEnd);
                SetEnvelopePos(_currentTrack.Channel, pos);
            }
            else if (!lowerHalf)
            {
                _currentTrack.NextTrackPos = _WF.GetBytePositionFromX(e.X, pictureBoxWaveForm.Width, _zoomStart, _zoomEnd);
                // if there is already a sync set, remove it first
                if (_currentTrack.NextTrackSync != 0)
                    BassMix.BASS_Mixer_ChannelRemoveSync(_currentTrack.Channel, _currentTrack.NextTrackSync);

                // right button will set a next track position sync
                _currentTrack.NextTrackSync = BassMix.BASS_Mixer_ChannelSetSync(_currentTrack.Channel, BASSSync.BASS_SYNC_POS | BASSSync.BASS_SYNC_MIXTIME, _currentTrack.NextTrackPos, _currentTrack.TrackSync, new IntPtr(1));

                _WF.AddMarker("Next", _currentTrack.NextTrackPos);
                DrawWave();
            }
        }

		#endregion

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            int level = Bass.BASS_ChannelGetLevel(_mixer);
            progressBarLeft.Value = Utils.LowWord32(level);
            progressBarRight.Value = Utils.HighWord32(level);

            if (_currentTrack != null)
            {
                long pos = BassMix.BASS_Mixer_ChannelGetPosition(_currentTrack.Channel);
                labelTime.Text = Utils.FixTimespan(Bass.BASS_ChannelBytes2Seconds(_currentTrack.Channel, pos), "HHMMSS");
                labelRemain.Text = Utils.FixTimespan(Bass.BASS_ChannelBytes2Seconds(_currentTrack.Channel, _currentTrack.TrackLength - pos), "HHMMSS");

                DrawWavePosition(pos, _currentTrack.TrackLength);
            }
        }

        private void buttonAddFile_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == openFileDialog.ShowDialog(this))
            {
                if (File.Exists(openFileDialog.FileName))
                {
                    lock (listBoxPlaylist)
                    {
                        Track track = new Track(openFileDialog.FileName);
                        listBoxPlaylist.Items.Add(track);

                        // in the demo we already add each new track to the mixer
                        // this is in real life not the best place to do so (especially with larger playlists)
                        // but for the demo it is okay ;-)

                        // add the new track to the mixer (in PAUSED mode!)
                        BassMix.BASS_Mixer_StreamAddChannel(_mixer, track.Channel, BASSFlag.BASS_MIXER_CHAN_PAUSE | BASSFlag.BASS_MIXER_CHAN_DOWNMIX | BASSFlag.BASS_STREAM_AUTOFREE);

                        // an BASS_SYNC_END is used to trigger the next track in the playlist (if no POS sync was set)
                        track.TrackSync = new SYNCPROC(OnTrackSync);
                        BassMix.BASS_Mixer_ChannelSetSync(track.Channel, BASSSync.BASS_SYNC_END, 0L, track.TrackSync, new IntPtr(0));
                    }

                    if (_currentTrack == null)
                        PlayNextTrack();
                }
            }
        }

        private void buttonSetEnvelope_Click(object sender, EventArgs e)
        {
            if (_currentTrack.Channel != 0)
            {
                BASS_MIXER_NODE[] nodes = 
                {
                    new BASS_MIXER_NODE(Bass.BASS_ChannelSeconds2Bytes(_mixer, 10d), 1f),
                    new BASS_MIXER_NODE(Bass.BASS_ChannelSeconds2Bytes(_mixer, 13d), 0f),
                    new BASS_MIXER_NODE(Bass.BASS_ChannelSeconds2Bytes(_mixer, 17d), 0f),
                    new BASS_MIXER_NODE(Bass.BASS_ChannelSeconds2Bytes(_mixer, 20d), 1f)
                };
                BassMix.BASS_Mixer_ChannelSetEnvelope(_currentTrack.Channel, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, nodes);
                // already align the envelope position to the current playback position
                // pause mixer
                Bass.BASS_ChannelLock(_mixer, true);
                long pos = BassMix.BASS_Mixer_ChannelGetPosition(_currentTrack.Channel);
                // convert source pos to mixer pos 
                long envPos = Bass.BASS_ChannelSeconds2Bytes(_mixer, Bass.BASS_ChannelBytes2Seconds(_currentTrack.Channel, pos));
                BassMix.BASS_Mixer_ChannelSetEnvelopePos(_currentTrack.Channel, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, envPos);
                // resume mixer 
                Bass.BASS_ChannelLock(_mixer, false);

                // and show it in our waveform
                _WF.DrawVolume = WaveForm.VOLUMEDRAWTYPE.Solid;
                foreach (BASS_MIXER_NODE node in nodes)
                {
                    _WF.AddVolumePoint(node.pos, node.val);
                }
                DrawWave();
            }
        }

        private void buttonRemoveEnvelope_Click(object sender, EventArgs e)
        {
            BassMix.BASS_Mixer_ChannelSetEnvelope(_currentTrack.Channel, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, null);
            _WF.ClearAllVolumePoints();
            _WF.DrawVolume = WaveForm.VOLUMEDRAWTYPE.None;
            DrawWave();
        }

        private void SetEnvelopePos(int source, long newPos)
        {
            // pause mixer
            Bass.BASS_ChannelLock(_mixer, true);
            BassMix.BASS_Mixer_ChannelSetPosition(source, newPos);
            // convert source pos to mixer pos 
            long envPos = Bass.BASS_ChannelSeconds2Bytes(_mixer, Bass.BASS_ChannelBytes2Seconds(source, newPos));
            BassMix.BASS_Mixer_ChannelSetEnvelopePos(source, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, envPos);
            // resume mixer 
            Bass.BASS_ChannelLock(_mixer, false);
        }
	}

    public class Track
    {
        public Track(string filename)
        {
            Filename = filename;
            Tags = BassTags.BASS_TAG_GetFromFile(Filename);
            if (Tags == null)
                throw new ArgumentException("File not valid!");

            // we already create a stream handle
            // might not be the best place here (especially when having a larger playlist), but for the demo this is okay ;)
            CreateStream();
        }

        public override string ToString()
        {
            return String.Format("{0} [{1}]", Tags, Utils.FixTimespan(Tags.duration, "HHMMSS"));
        }

        // member
        public string Filename = String.Empty;
        public TAG_INFO Tags = null;
        public int Channel = 0;
        public long TrackLength = 0L;
        public SYNCPROC TrackSync;
        public int NextTrackSync = 0;
        public long NextTrackPos = 0L;

        private bool CreateStream()
        {
            Channel = Bass.BASS_StreamCreateFile(Filename, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);
            if (Channel != 0)
            {
                TrackLength = Bass.BASS_ChannelGetLength(Channel);
                return true;
            }
            return false;
        }
    }
}
