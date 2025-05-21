using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.Misc;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!


	public class SetFX : System.Windows.Forms.Form
	{
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button buttonPlay;
		private System.Windows.Forms.TrackBar trackBarChorus;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TrackBar trackBarEcho;
		private System.Windows.Forms.Timer timer1;
        private Label label3;
        private Label label4;
        private Label label5;
        private TrackBar trackBarLowEQ;
        private TrackBar trackBarMidEQ;
        private TrackBar trackBarHighEQ;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
		private System.ComponentModel.IContainer components;

		public SetFX()
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
            this.buttonPlay = new System.Windows.Forms.Button();
            this.trackBarChorus = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBarEcho = new System.Windows.Forms.TrackBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.trackBarLowEQ = new System.Windows.Forms.TrackBar();
            this.trackBarMidEQ = new System.Windows.Forms.TrackBar();
            this.trackBarHighEQ = new System.Windows.Forms.TrackBar();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarChorus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarEcho)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarLowEQ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMidEQ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarHighEQ)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav";
            this.openFileDialog.Title = "Select an audio file to play";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(16, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(260, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Select a file to play (e.g. MP3, OGG or WAV)...";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(16, 41);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(260, 23);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // trackBarChorus
            // 
            this.trackBarChorus.AutoSize = false;
            this.trackBarChorus.Location = new System.Drawing.Point(241, 88);
            this.trackBarChorus.Maximum = 100;
            this.trackBarChorus.Name = "trackBarChorus";
            this.trackBarChorus.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarChorus.Size = new System.Drawing.Size(38, 138);
            this.trackBarChorus.TabIndex = 11;
            this.trackBarChorus.TickFrequency = 10;
            this.trackBarChorus.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarChorus.Scroll += new System.EventHandler(this.trackBarChorus_Scroll);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(233, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 16);
            this.label1.TabIndex = 10;
            this.label1.Text = "Chorus";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(189, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 16);
            this.label2.TabIndex = 8;
            this.label2.Text = "Echo";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // trackBarEcho
            // 
            this.trackBarEcho.AutoSize = false;
            this.trackBarEcho.Location = new System.Drawing.Point(198, 88);
            this.trackBarEcho.Maximum = 100;
            this.trackBarEcho.Name = "trackBarEcho";
            this.trackBarEcho.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarEcho.Size = new System.Drawing.Size(38, 138);
            this.trackBarEcho.TabIndex = 9;
            this.trackBarEcho.TickFrequency = 10;
            this.trackBarEcho.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarEcho.Scroll += new System.EventHandler(this.trackBarEcho_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "125 Hz";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(60, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "1 kHz";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(101, 72);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "8 kHz";
            // 
            // trackBarLowEQ
            // 
            this.trackBarLowEQ.AutoSize = false;
            this.trackBarLowEQ.LargeChange = 50;
            this.trackBarLowEQ.Location = new System.Drawing.Point(16, 88);
            this.trackBarLowEQ.Maximum = 150;
            this.trackBarLowEQ.Minimum = -150;
            this.trackBarLowEQ.Name = "trackBarLowEQ";
            this.trackBarLowEQ.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarLowEQ.Size = new System.Drawing.Size(38, 138);
            this.trackBarLowEQ.SmallChange = 10;
            this.trackBarLowEQ.TabIndex = 3;
            this.trackBarLowEQ.TickFrequency = 50;
            this.trackBarLowEQ.ValueChanged += new System.EventHandler(this.trackBarLowEQ_ValueChanged);
            // 
            // trackBarMidEQ
            // 
            this.trackBarMidEQ.AutoSize = false;
            this.trackBarMidEQ.LargeChange = 50;
            this.trackBarMidEQ.Location = new System.Drawing.Point(57, 88);
            this.trackBarMidEQ.Maximum = 150;
            this.trackBarMidEQ.Minimum = -150;
            this.trackBarMidEQ.Name = "trackBarMidEQ";
            this.trackBarMidEQ.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarMidEQ.Size = new System.Drawing.Size(38, 138);
            this.trackBarMidEQ.SmallChange = 10;
            this.trackBarMidEQ.TabIndex = 5;
            this.trackBarMidEQ.TickFrequency = 50;
            this.trackBarMidEQ.ValueChanged += new System.EventHandler(this.trackBarMidEQ_ValueChanged);
            // 
            // trackBarHighEQ
            // 
            this.trackBarHighEQ.AutoSize = false;
            this.trackBarHighEQ.LargeChange = 50;
            this.trackBarHighEQ.Location = new System.Drawing.Point(98, 88);
            this.trackBarHighEQ.Maximum = 150;
            this.trackBarHighEQ.Minimum = -150;
            this.trackBarHighEQ.Name = "trackBarHighEQ";
            this.trackBarHighEQ.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarHighEQ.Size = new System.Drawing.Size(38, 138);
            this.trackBarHighEQ.SmallChange = 10;
            this.trackBarHighEQ.TabIndex = 7;
            this.trackBarHighEQ.TickFrequency = 50;
            this.trackBarHighEQ.ValueChanged += new System.EventHandler(this.trackBarHighEQ_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(135, 95);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 12);
            this.label6.TabIndex = 12;
            this.label6.Text = "+15dB";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(135, 205);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(29, 12);
            this.label7.TabIndex = 12;
            this.label7.Text = "-15dB";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(172, 95);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(22, 12);
            this.label8.TabIndex = 12;
            this.label8.Text = "Wet";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(174, 205);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(20, 12);
            this.label9.TabIndex = 12;
            this.label9.Text = "Dry";
            // 
            // SetFX
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(292, 234);
            this.Controls.Add(this.trackBarHighEQ);
            this.Controls.Add(this.trackBarMidEQ);
            this.Controls.Add(this.trackBarLowEQ);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.trackBarEcho);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trackBarChorus);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label6);
            this.Name = "SetFX";
            this.Text = "SetFX Test";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.SetFX_Closing);
            this.Load += new System.EventHandler(this.SetFX_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarChorus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarEcho)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarLowEQ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMidEQ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarHighEQ)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
			Application.Run(new SetFX());
		}

		// LOCAL VARS
		private int _Stream = 0;
		private string _FileName = String.Empty;
		private int _fxChorusHandle = 0;
        private BASS_DX8_CHORUS _chorus = new BASS_DX8_CHORUS(0f, 25f, 90f, 5f, 1, 0f, BASSFXPhase.BASS_FX_PHASE_NEG_90);
		private int _fxEchoHandle = 0;
        private BASS_DX8_ECHO _echo = new BASS_DX8_ECHO(90f, 50f, 500f, 500f, false);
        private int[] _fxEQ = { 0, 0, 0 };

		private void SetFX_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your email", "your regkey");

			// a dummy at the beginning to show the use of "BASS_GetDeviceDescription"
			BASS_DEVICEINFO[] devs = Bass.BASS_GetDeviceInfos();
            foreach (BASS_DEVICEINFO dev in devs)
                Console.WriteLine(dev.ToString());
			Console.WriteLine( "TotalRealDevices={0} of {1}", Bass.BASS_GetDeviceCount(), devs.Length );

			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) )
			{
				// all fine
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void SetFX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
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

		private void buttonPlay_Click(object sender, System.EventArgs e)
		{
			Bass.BASS_StreamFree(_Stream);
			// test BASS_ChannelSetFX
			if (_FileName != String.Empty)
			{
				// create the stream
				_Stream = Bass.BASS_StreamCreateFile(_FileName, 0, 0, BASSFlag.BASS_DEFAULT);
				
				// create some FX...
				_fxChorusHandle = Bass.BASS_ChannelSetFX(_Stream, BASSFXType.BASS_FX_DX8_CHORUS, 1);
                _chorus.fWetDryMix = 0f;
                trackBarChorus.Value = 0;
                Bass.BASS_FXSetParameters(_fxChorusHandle, _chorus);

				_fxEchoHandle = Bass.BASS_ChannelSetFX(_Stream, BASSFXType.BASS_FX_DX8_ECHO, 2);
                _echo.fWetDryMix = 0f;
                trackBarEcho.Value = 0;
                Bass.BASS_FXSetParameters(_fxEchoHandle, _echo);
                
                // 3-band EQ
                BASS_DX8_PARAMEQ eq = new BASS_DX8_PARAMEQ();
                _fxEQ[0] = Bass.BASS_ChannelSetFX(_Stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
                _fxEQ[1] = Bass.BASS_ChannelSetFX(_Stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
                _fxEQ[2] = Bass.BASS_ChannelSetFX(_Stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
                eq.fBandwidth = 18f;
                
                eq.fCenter = 100f;
                eq.fGain = this.trackBarLowEQ.Value / 10f;
                Bass.BASS_FXSetParameters(_fxEQ[0], eq);
                eq.fCenter = 1000f;
                eq.fGain = this.trackBarMidEQ.Value / 10f;
                Bass.BASS_FXSetParameters(_fxEQ[1], eq);
                eq.fCenter = 8000f;
                eq.fGain = this.trackBarHighEQ.Value / 10f;
                Bass.BASS_FXSetParameters(_fxEQ[2], eq);

				// play the stream
				if (_Stream != 0 && Bass.BASS_ChannelPlay(_Stream, false) )
				{
					//playing...
				}
				else
				{
					MessageBox.Show( this, "Error: "+Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()) );
				}
			}
		}

		private void trackBarChorus_Scroll(object sender, System.EventArgs e)
		{
			// changing the chorus slide...effects the dry/wet mix...
			_chorus.fWetDryMix = (float)this.trackBarChorus.Value;
			Bass.BASS_FXSetParameters(_fxChorusHandle, _chorus);
		}

		private void trackBarEcho_Scroll(object sender, System.EventArgs e)
		{
			// changing the echo slide...effects the dry/wet mix...
            _echo.fWetDryMix = (float)this.trackBarEcho.Value;
			Bass.BASS_FXSetParameters(_fxEchoHandle, _echo);
		}

        private void UpdateEQ(int band, float gain)
        {
            BASS_DX8_PARAMEQ eq = new BASS_DX8_PARAMEQ();
            if (Bass.BASS_FXGetParameters(_fxEQ[band], eq))
            {
                eq.fGain = gain;
                Bass.BASS_FXSetParameters(_fxEQ[band], eq);
            }
        }

        private void trackBarLowEQ_ValueChanged(object sender, EventArgs e)
        {
            UpdateEQ(0, trackBarLowEQ.Value / 10f);
        }

        private void trackBarMidEQ_ValueChanged(object sender, EventArgs e)
        {
            UpdateEQ(1, trackBarMidEQ.Value / 10f);
        }

        private void trackBarHighEQ_ValueChanged(object sender, EventArgs e)
        {
            UpdateEQ(2, trackBarHighEQ.Value / 10f);
        }

	}
}
