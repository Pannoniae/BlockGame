using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
	//       Needs 'bass_fx.dll' - copy it to your output directory first!


	public class SimpleFX : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Timer timerUpdate;
		private System.Windows.Forms.ProgressBar progressBarPeakLeft;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ProgressBar progressBarPeakRight;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonPlay;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.CheckBox checkBoxFlanger;
		private System.Windows.Forms.TextBox textBoxFlangerSpeed;
		private System.Windows.Forms.Button buttonFlangerApply;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox checkBoxEcho;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button buttonEchoApply;
		private System.Windows.Forms.TextBox textBoxEchoDelay;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.TrackBar trackBarSpeed;
		private System.Windows.Forms.Button buttonSpeedreset;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxSwap;
		private System.Windows.Forms.Label labelRatio;
		private System.Windows.Forms.Label labelBPM;
		private System.Windows.Forms.Label labelRMS;
		private System.Windows.Forms.Label labelRMSValue;
		private System.ComponentModel.IContainer components;

		public SimpleFX()
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
			this.timerUpdate = new System.Windows.Forms.Timer(this.components);
			this.progressBarPeakLeft = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.progressBarPeakRight = new System.Windows.Forms.ProgressBar();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonPlay = new System.Windows.Forms.Button();
			this.labelRMS = new System.Windows.Forms.Label();
			this.labelRMSValue = new System.Windows.Forms.Label();
			this.labelTime = new System.Windows.Forms.Label();
			this.checkBoxFlanger = new System.Windows.Forms.CheckBox();
			this.textBoxFlangerSpeed = new System.Windows.Forms.TextBox();
			this.buttonFlangerApply = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.buttonEchoApply = new System.Windows.Forms.Button();
			this.textBoxEchoDelay = new System.Windows.Forms.TextBox();
			this.checkBoxEcho = new System.Windows.Forms.CheckBox();
			this.label7 = new System.Windows.Forms.Label();
			this.buttonStop = new System.Windows.Forms.Button();
			this.trackBarSpeed = new System.Windows.Forms.TrackBar();
			this.buttonSpeedreset = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.checkBoxSwap = new System.Windows.Forms.CheckBox();
			this.labelRatio = new System.Windows.Forms.Label();
			this.labelBPM = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSpeed)).BeginInit();
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
			// timerUpdate
			// 
			this.timerUpdate.Interval = 50;
			this.timerUpdate.Tick += new System.EventHandler(this.timerUpdate_Tick);
			// 
			// progressBarPeakLeft
			// 
			this.progressBarPeakLeft.Location = new System.Drawing.Point(20, 90);
			this.progressBarPeakLeft.Maximum = 32768;
			this.progressBarPeakLeft.Name = "progressBarPeakLeft";
			this.progressBarPeakLeft.Size = new System.Drawing.Size(206, 12);
			this.progressBarPeakLeft.Step = 1;
			this.progressBarPeakLeft.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 90);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(16, 12);
			this.label1.TabIndex = 2;
			this.label1.Text = "L";
			// 
			// progressBarPeakRight
			// 
			this.progressBarPeakRight.Location = new System.Drawing.Point(20, 106);
			this.progressBarPeakRight.Maximum = 32768;
			this.progressBarPeakRight.Name = "progressBarPeakRight";
			this.progressBarPeakRight.Size = new System.Drawing.Size(206, 12);
			this.progressBarPeakRight.Step = 1;
			this.progressBarPeakRight.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 106);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(16, 12);
			this.label2.TabIndex = 4;
			this.label2.Text = "R";
			// 
			// buttonPlay
			// 
			this.buttonPlay.Location = new System.Drawing.Point(12, 46);
			this.buttonPlay.Name = "buttonPlay";
			this.buttonPlay.TabIndex = 5;
			this.buttonPlay.Text = "PLAY";
			this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
			// 
			// labelRMS
			// 
			this.labelRMS.Location = new System.Drawing.Point(230, 90);
			this.labelRMS.Name = "labelRMS";
			this.labelRMS.Size = new System.Drawing.Size(50, 12);
			this.labelRMS.TabIndex = 6;
			this.labelRMS.Text = "RMS dB:";
			this.labelRMS.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelRMSValue
			// 
			this.labelRMSValue.Location = new System.Drawing.Point(230, 106);
			this.labelRMSValue.Name = "labelRMSValue";
			this.labelRMSValue.Size = new System.Drawing.Size(48, 12);
			this.labelRMSValue.TabIndex = 7;
			this.labelRMSValue.Text = "0";
			this.labelRMSValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelTime
			// 
			this.labelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelTime.Location = new System.Drawing.Point(10, 126);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(260, 18);
			this.labelTime.TabIndex = 8;
			this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// checkBoxFlanger
			// 
			this.checkBoxFlanger.Location = new System.Drawing.Point(12, 176);
			this.checkBoxFlanger.Name = "checkBoxFlanger";
			this.checkBoxFlanger.Size = new System.Drawing.Size(84, 24);
			this.checkBoxFlanger.TabIndex = 12;
			this.checkBoxFlanger.Text = "Flanger";
			this.checkBoxFlanger.CheckedChanged += new System.EventHandler(this.checkBoxFlanger_CheckedChanged);
			// 
			// textBoxFlangerSpeed
			// 
			this.textBoxFlangerSpeed.Location = new System.Drawing.Point(108, 178);
			this.textBoxFlangerSpeed.Name = "textBoxFlangerSpeed";
			this.textBoxFlangerSpeed.Size = new System.Drawing.Size(76, 20);
			this.textBoxFlangerSpeed.TabIndex = 13;
			this.textBoxFlangerSpeed.Text = "0";
			// 
			// buttonFlangerApply
			// 
			this.buttonFlangerApply.Location = new System.Drawing.Point(195, 176);
			this.buttonFlangerApply.Name = "buttonFlangerApply";
			this.buttonFlangerApply.TabIndex = 14;
			this.buttonFlangerApply.Text = "Apply";
			this.buttonFlangerApply.Click += new System.EventHandler(this.buttonFlangerApply_Click);
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(108, 162);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(164, 23);
			this.label6.TabIndex = 15;
			this.label6.Text = "Fanger Speed (eg. 0,01)";
			// 
			// buttonEchoApply
			// 
			this.buttonEchoApply.Location = new System.Drawing.Point(195, 222);
			this.buttonEchoApply.Name = "buttonEchoApply";
			this.buttonEchoApply.TabIndex = 18;
			this.buttonEchoApply.Text = "Apply";
			this.buttonEchoApply.Click += new System.EventHandler(this.buttonEchoApply_Click);
			// 
			// textBoxEchoDelay
			// 
			this.textBoxEchoDelay.Location = new System.Drawing.Point(108, 222);
			this.textBoxEchoDelay.Name = "textBoxEchoDelay";
			this.textBoxEchoDelay.Size = new System.Drawing.Size(76, 20);
			this.textBoxEchoDelay.TabIndex = 17;
			this.textBoxEchoDelay.Text = "0";
			// 
			// checkBoxEcho
			// 
			this.checkBoxEcho.Location = new System.Drawing.Point(12, 220);
			this.checkBoxEcho.Name = "checkBoxEcho";
			this.checkBoxEcho.Size = new System.Drawing.Size(84, 24);
			this.checkBoxEcho.TabIndex = 16;
			this.checkBoxEcho.Text = "Echo3";
			this.checkBoxEcho.CheckedChanged += new System.EventHandler(this.checkBoxEcho_CheckedChanged);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(108, 206);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(164, 23);
			this.label7.TabIndex = 19;
			this.label7.Text = "Echo Delay in sec:";
			// 
			// buttonStop
			// 
			this.buttonStop.Location = new System.Drawing.Point(195, 46);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.TabIndex = 20;
			this.buttonStop.Text = "STOP";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// trackBarSpeed
			// 
			this.trackBarSpeed.LargeChange = 5000;
			this.trackBarSpeed.Location = new System.Drawing.Point(10, 252);
			this.trackBarSpeed.Maximum = 96000;
			this.trackBarSpeed.Minimum = 1000;
			this.trackBarSpeed.Name = "trackBarSpeed";
			this.trackBarSpeed.Size = new System.Drawing.Size(260, 45);
			this.trackBarSpeed.SmallChange = 500;
			this.trackBarSpeed.TabIndex = 21;
			this.trackBarSpeed.TickFrequency = 5000;
			this.trackBarSpeed.Value = 44100;
			this.trackBarSpeed.Scroll += new System.EventHandler(this.trackBarSpeed_Scroll);
			// 
			// buttonSpeedreset
			// 
			this.buttonSpeedreset.Location = new System.Drawing.Point(190, 300);
			this.buttonSpeedreset.Name = "buttonSpeedreset";
			this.buttonSpeedreset.Size = new System.Drawing.Size(80, 23);
			this.buttonSpeedreset.TabIndex = 22;
			this.buttonSpeedreset.Text = "Reset Speed";
			this.buttonSpeedreset.Click += new System.EventHandler(this.buttonSpeedreset_Click);
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(10, 300);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(56, 23);
			this.label8.TabIndex = 23;
			this.label8.Text = "Speed";
			// 
			// checkBoxSwap
			// 
			this.checkBoxSwap.Location = new System.Drawing.Point(12, 148);
			this.checkBoxSwap.Name = "checkBoxSwap";
			this.checkBoxSwap.Size = new System.Drawing.Size(90, 24);
			this.checkBoxSwap.TabIndex = 24;
			this.checkBoxSwap.Text = "Rotate";
			this.checkBoxSwap.CheckedChanged += new System.EventHandler(this.checkBoxSwap_CheckedChanged);
			// 
			// labelRatio
			// 
			this.labelRatio.Location = new System.Drawing.Point(88, 300);
			this.labelRatio.Name = "labelRatio";
			this.labelRatio.Size = new System.Drawing.Size(80, 23);
			this.labelRatio.TabIndex = 25;
			// 
			// labelBPM
			// 
			this.labelBPM.Location = new System.Drawing.Point(90, 46);
			this.labelBPM.Name = "labelBPM";
			this.labelBPM.TabIndex = 26;
			this.labelBPM.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// SimpleFX
			// 
			this.ClientSize = new System.Drawing.Size(282, 326);
			this.Controls.Add(this.labelBPM);
			this.Controls.Add(this.labelRatio);
			this.Controls.Add(this.checkBoxSwap);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.buttonSpeedreset);
			this.Controls.Add(this.trackBarSpeed);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.buttonEchoApply);
			this.Controls.Add(this.textBoxEchoDelay);
			this.Controls.Add(this.textBoxFlangerSpeed);
			this.Controls.Add(this.checkBoxEcho);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.buttonFlangerApply);
			this.Controls.Add(this.checkBoxFlanger);
			this.Controls.Add(this.labelRMSValue);
			this.Controls.Add(this.labelRMS);
			this.Controls.Add(this.buttonPlay);
			this.Controls.Add(this.progressBarPeakRight);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.progressBarPeakLeft);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.labelTime);
			this.Name = "SimpleFX";
			this.Text = "Simple FX";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.SimpleFX_Closing);
			this.Load += new System.EventHandler(this.SimpleFX_Load);
			((System.ComponentModel.ISupportInitialize)(this.trackBarSpeed)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
			Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new SimpleFX());
		}

		// LOCAL VARS
		private int _Stream = 0;
		private int _StreamFX = 0;
		private string _FileName = String.Empty;
		private int _TickCounter = 0;

		private void SimpleFX_Load(object sender, System.EventArgs e)
		{
			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero) )
			{
				// all ok
                // load BASS_FX
                BassFx.BASS_FX_GetVersion();
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void SimpleFX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
		}

		private int _20mslength = 0;
		private float[] _rmsData;     // our global data buffer used at RMS
		private double RMS(int channel, out int peakL, out int peakR)
		{
			double sum = 0f;
			float maxL = 0f;
			float maxR = 0f;
			int length = _20mslength;
			int l4 = length/4; // the number of 32-bit floats required (since length is in bytes!)

			// increase our data buffer as needed
			if (_rmsData == null || _rmsData.Length < l4)
				_rmsData = new float[l4];

			try
			{
				length = Bass.BASS_ChannelGetData(channel, _rmsData, length);
				l4 = length/4; // the number of 32-bit floats received

				for (int a=0; a < l4; a++)
				{
					sum += _rmsData[a] * _rmsData[a]; // sum the squares
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
			catch {}

			peakL = (int)Math.Round(32768f * maxL);
			if (peakL > 32768)
				peakL = 32768;
			peakR = (int)Math.Round(32768f * maxR);
			if (peakR > 32768)
				peakR = 32768;

			return Math.Sqrt( sum / (l4/2));  // l4/2, since we use 2 channels!
		}

		private void timerUpdate_Tick(object sender, System.EventArgs e)
		{
			// here we gather info about the stream, when it is playing...
			if ( Bass.BASS_ChannelIsActive(_StreamFX) == BASSActive.BASS_ACTIVE_PLAYING )
			{
				// the stream is still playing...
				this.labelBPM.BackColor = SystemColors.Control;
			}
			else
			{
				// the stream is NOT playing anymore...
				this.timerUpdate.Stop();
				this.progressBarPeakLeft.Value = 0;
				this.progressBarPeakRight.Value = 0;
				this.labelTime.Text = "Stopped";
				return;
			}

			// display the level bars
			int peakL = 0;
			int peakR = 0;
			double rms = RMS(_StreamFX, out peakL, out peakR);
			this.progressBarPeakLeft.Value = peakL;
			this.progressBarPeakRight.Value = peakR;

			// from here on, the stream is for sure playing...
			_TickCounter++;
			if (_TickCounter == 4)
			{
				// display the position every 200ms (since timer is 50ms)
				_TickCounter = 0;
				long len = Bass.BASS_ChannelGetLength(_StreamFX); // length in bytes
				long pos = Bass.BASS_ChannelGetPosition(_StreamFX); // position in bytes
				double totaltime = Bass.BASS_ChannelBytes2Seconds(_StreamFX, len); // the total time length
                double elapsedtime = Bass.BASS_ChannelBytes2Seconds(_StreamFX, pos); // the elapsed time length
				double remainingtime = totaltime - elapsedtime;
				this.labelTime.Text = String.Format( "Elapsed: {0} - Total: {1} - Remain: {2}", Utils.FixTimespan(elapsedtime, "MMSS"), Utils.FixTimespan(totaltime, "MMSS"), Utils.FixTimespan(remainingtime, "MMSS"));
				this.Text = String.Format( "CPU: {0:0.00}%", Bass.BASS_GetCPU() );
				this.labelRMSValue.Text = Utils.LevelToDB( rms, 1d).ToString("0.0");
			}
		}

		private void buttonPlay_Click(object sender, System.EventArgs e)
		{
			this.checkBoxSwap.Checked = false;
			this.checkBoxFlanger.Checked = false;
			this.checkBoxEcho.Checked = false;
			Bass.BASS_StreamFree(_StreamFX);
			if (_FileName != String.Empty)
			{
				// create the decoding stream
				_Stream = Bass.BASS_StreamCreateFile(_FileName, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);
				if (_Stream != 0)
				{
					_20mslength = (int)Bass.BASS_ChannelSeconds2Bytes(_Stream, 0.02f); // 20ms window
					// and start to get the BPM...BEFORE! playing
					bpmProc = new BPMPROGRESSPROC(MyBPMProc);
					float bpm = BassFx.BASS_FX_BPM_DecodeGet(_Stream, 1f, 180f, Utils.MakeLong(50,180), BASSFXBpm.BASS_FX_BPM_BKGRND | BASSFXBpm.BASS_FX_FREESOURCE | BASSFXBpm.BASS_FX_BPM_MULT2, bpmProc, IntPtr.Zero);
					this.labelBPM.Text = String.Format( "BPM={0}", bpm );

					// and set the position back...so that we hear the playback from the beginning...
					// never get the BPM from '_Stream' while playing...this will steel the data from the decoding channel
					Bass.BASS_ChannelSetPosition(_Stream, 0);

					// now we create a Tempo channel...the actual playing channel
                    _StreamFX = BassFx.BASS_FX_TempoCreate(_Stream, BASSFlag.BASS_FX_FREESOURCE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_SAMPLE_LOOP);
				}

				if (_StreamFX != 0 && Bass.BASS_ChannelPlay(_StreamFX, false) )
				{
					this.timerUpdate.Start();

					// real-time beat position
					beatProc = new BPMBEATPROC(MyBeatProc);
					BassFx.BASS_FX_BPM_BeatCallbackSet(_StreamFX, beatProc, IntPtr.Zero);
				}
				else
				{
                    Console.WriteLine("Error = {0}", Bass.BASS_ErrorGetCode());
				}
			}
		}

		private BPMPROGRESSPROC bpmProc;
        private void MyBPMProc(int chan, float percent, IntPtr user)
		{
            BeginInvoke((MethodInvoker)delegate()
            {
                // this code runs on the UI thread!
                this.labelBPM.Text = String.Format("{0}%", percent);
            });
		}

		private BPMBEATPROC beatProc;
		private void MyBeatProc(int handle, double beatpos, IntPtr user)
		{
            BeginInvoke((MethodInvoker)delegate()
            {
                // this code runs on the UI thread!
                this.labelBPM.BackColor = Color.Red;
            });
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			this.timerUpdate.Stop();
			Bass.BASS_StreamFree(_StreamFX);
			BassFx.BASS_FX_BPM_BeatFree(_StreamFX);
			_StreamFX = 0;
			_Stream = 0;
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.openFileDialog.FileName = _FileName;
			if ( DialogResult.OK == this.openFileDialog.ShowDialog(this) )
			{
				if ( File.Exists(this.openFileDialog.FileName) )
					_FileName = this.openFileDialog.FileName;
				else
					_FileName = String.Empty;
			}
		}

        private int _rotateFX = 0;
		private void checkBoxSwap_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_StreamFX == 0)
				return;

			if (this.checkBoxSwap.Checked)
			{
				// and add a DSP here as well...
                _rotateFX = Bass.BASS_ChannelSetFX(_StreamFX, BASSFXType.BASS_FX_BFX_ROTATE, -5);
                // the rotate effect has no parameters, so nothing to set
			}
			else
			{
				// remove the DSP
                Bass.BASS_ChannelRemoveFX(_StreamFX, _rotateFX);
                _rotateFX = 0;
			}
		}

        private int _flangerFX = 0;
		private void checkBoxFlanger_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_StreamFX == 0)
				return;

			if (this.checkBoxFlanger.Checked)
			{
                // and add a DSP here as well...
                _flangerFX = Bass.BASS_ChannelSetFX(_StreamFX, BASSFXType.BASS_FX_BFX_FLANGER, -4);
                BASS_BFX_FLANGER flanger = new BASS_BFX_FLANGER();
                flanger.Preset_Default();
                Bass.BASS_FXSetParameters(_flangerFX, flanger);

                // just to demo how to get
                BASS_BFX_FLANGER f = new BASS_BFX_FLANGER();
                Bass.BASS_FXGetParameters(_flangerFX, f);
            }
            else
            {
                // remove the DSP
                Bass.BASS_ChannelRemoveFX(_StreamFX, _flangerFX);
                _flangerFX = 0;
            }
		}

		private void buttonFlangerApply_Click(object sender, System.EventArgs e)
		{
			if (_StreamFX == 0)
				return;

            BASS_BFX_FLANGER flanger = new BASS_BFX_FLANGER();
            // get the current
            Bass.BASS_FXGetParameters(_flangerFX, flanger);
            flanger.fSpeed = float.Parse(this.textBoxFlangerSpeed.Text);
            // set the new values
            Bass.BASS_FXSetParameters(_flangerFX, flanger);
		}

        private int _echoFX = 0;
		private void checkBoxEcho_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_StreamFX == 0)
				return;

			if (this.checkBoxEcho.Checked)
			{
                // and add a DSP here as well...
                _echoFX = Bass.BASS_ChannelSetFX(_StreamFX, BASSFXType.BASS_FX_BFX_ECHO3, -3);
                BASS_BFX_ECHO3 echo = new BASS_BFX_ECHO3();
                echo.Preset_LongEcho();
                Bass.BASS_FXSetParameters(_echoFX, echo);
            }
            else
            {
                // remove the DSP
                Bass.BASS_ChannelRemoveFX(_StreamFX, _echoFX);
                _echoFX = 0;
            }
		}

		private void buttonEchoApply_Click(object sender, System.EventArgs e)
		{
			if (_StreamFX == 0)
				return;

            BASS_BFX_ECHO3 echo = new BASS_BFX_ECHO3();
            // get the current
            Bass.BASS_FXGetParameters(_echoFX, echo);
            echo.fDelay = float.Parse(this.textBoxEchoDelay.Text);
            // set the new values
            Bass.BASS_FXSetParameters(_echoFX, echo);
		}

		private void trackBarSpeed_Scroll(object sender, System.EventArgs e)
		{
			// in Hz
            Bass.BASS_ChannelSetAttribute(_StreamFX, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, (float)trackBarSpeed.Value);
			
			// get the resulting ratio...
			float ratio = BassFx.BASS_FX_TempoGetRateRatio(_StreamFX);
			this.labelRatio.Text = String.Format( "{0}", ratio );
		}

		private void buttonSpeedreset_Click(object sender, System.EventArgs e)
		{
			trackBarSpeed.Value = 44100;
            Bass.BASS_ChannelSetAttribute(_StreamFX, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, (float)trackBarSpeed.Value);
			this.labelRatio.Text = "";
		}

	}
}
