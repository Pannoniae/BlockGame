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
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.BassAsio;
using Un4seen.Bass.AddOn.Mix;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
	// NOTE: Needs 'bass_fx.dll' - copy it to your output directory first!


	public class TestDSP : System.Windows.Forms.Form
	{
		// LOCAL VARS
		private int _stream = 0;
		private string _fileName = String.Empty;
		private int _tickCounter = 0;
		private DSP_PeakLevelMeter _plm1;
		private DSP_PeakLevelMeter _plm2;
		private Visuals _visModified = new Visuals();
		private bool _fullSpectrum = true;
		private SYNCPROC _sync = null;
		private int _syncer = 0;
		private DSP_Mono _mono;
		private DSP_Gain _gain;
		private DSP_StereoEnhancer _stereoEnh;
		private DSP_IIRDelay _delay;
		private DSP_SoftSaturation _softSat;
		private DSP_StreamCopy _streamCopy;
        private BASS_BFX_DAMP _damp = new BASS_BFX_DAMP();
        private BASS_DX8_COMPRESSOR _comp = new BASS_DX8_COMPRESSOR();
		private int _dampPrio = 3;
		private int _compPrio = 2;
		private int _deviceLatencyMS = 0; // device latency in milliseconds
		private int _deviceLatencyBytes = 0; // device latency in bytes
		private int _updateInterval = 50; // 50ms
		private Un4seen.Bass.BASSTimer _updateTimer = null;

		//

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonPlay;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBoxGainDBValue;
		private System.Windows.Forms.Button buttonSetGain;
		private System.Windows.Forms.Button buttonZoom;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.ProgressBar progressBarPeak1Left;
		private System.Windows.Forms.ProgressBar progressBarPeak1Right;
		private System.Windows.Forms.ProgressBar progressBarPeak2Right;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ProgressBar progressBarPeak2Left;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TrackBar trackBarEffect;
		private System.Windows.Forms.Label labelLevel2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label labelLevel1;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.CheckBox checkBoxLevel1Bypass;
		private System.Windows.Forms.CheckBox checkBoxLevel2Bypass;
		private System.Windows.Forms.CheckBox checkBoxCompressor;
		private System.Windows.Forms.CheckBox checkBoxDAmp;
		private System.Windows.Forms.TrackBar trackBarCompressor;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label labelCompThreshold;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.CheckBox checkBoxGainDither;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.TrackBar trackBarStereoEnhancerWide;
		private System.Windows.Forms.CheckBox checkBoxStereoEnhancer;
		private System.Windows.Forms.TrackBar trackBarStereoEnhancerWetDry;
		private System.Windows.Forms.Label labelStereoEnhancer;
		private System.Windows.Forms.TrackBar trackBarGain;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.Label label23;
		private System.Windows.Forms.PictureBox pictureBoxSpectrum;
		private System.Windows.Forms.CheckBox checkBoxMono;
		private System.Windows.Forms.CheckBox checkBoxMonoInvert;
		private System.Windows.Forms.Label label24;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.Label label26;
		private System.Windows.Forms.TrackBar trackBarIIRDelay;
		private System.Windows.Forms.CheckBox checkBoxIIRDelay;
		private System.Windows.Forms.TrackBar trackBarIIRDelayWet;
		private System.Windows.Forms.TrackBar trackBarIIRDelayFeedback;
		private System.Windows.Forms.Label labelIIRDelay;
		private System.Windows.Forms.Label label27;
		private System.Windows.Forms.CheckBox checkBoxSoftSat;
		private System.Windows.Forms.TrackBar trackBarSoftSat;
		private System.Windows.Forms.Button buttonPause;
		private System.Windows.Forms.ComboBox comboBoxStreamCopy;
		private System.Windows.Forms.Label label29;
		private System.Windows.Forms.CheckBox checkBoxStreamCopy;
		private System.Windows.Forms.TrackBar trackBarSoftSatDepth;
		private System.Windows.Forms.Label label30;
		private System.Windows.Forms.PictureBox pictureBox1;

		public TestDSP()
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
			this.progressBarPeak1Left = new System.Windows.Forms.ProgressBar();
			this.label1 = new System.Windows.Forms.Label();
			this.progressBarPeak1Right = new System.Windows.Forms.ProgressBar();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonPlay = new System.Windows.Forms.Button();
			this.labelTime = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxGainDBValue = new System.Windows.Forms.TextBox();
			this.buttonSetGain = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.buttonZoom = new System.Windows.Forms.Button();
			this.buttonStop = new System.Windows.Forms.Button();
			this.progressBarPeak2Right = new System.Windows.Forms.ProgressBar();
			this.label6 = new System.Windows.Forms.Label();
			this.progressBarPeak2Left = new System.Windows.Forms.ProgressBar();
			this.label7 = new System.Windows.Forms.Label();
			this.labelLevel2 = new System.Windows.Forms.Label();
			this.trackBarEffect = new System.Windows.Forms.TrackBar();
			this.label5 = new System.Windows.Forms.Label();
			this.labelLevel1 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.checkBoxDAmp = new System.Windows.Forms.CheckBox();
			this.checkBoxLevel1Bypass = new System.Windows.Forms.CheckBox();
			this.checkBoxLevel2Bypass = new System.Windows.Forms.CheckBox();
			this.checkBoxCompressor = new System.Windows.Forms.CheckBox();
			this.trackBarCompressor = new System.Windows.Forms.TrackBar();
			this.label11 = new System.Windows.Forms.Label();
			this.labelCompThreshold = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.checkBoxGainDither = new System.Windows.Forms.CheckBox();
			this.trackBarStereoEnhancerWide = new System.Windows.Forms.TrackBar();
			this.checkBoxStereoEnhancer = new System.Windows.Forms.CheckBox();
			this.trackBarStereoEnhancerWetDry = new System.Windows.Forms.TrackBar();
			this.labelStereoEnhancer = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.label19 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.checkBoxIIRDelay = new System.Windows.Forms.CheckBox();
			this.trackBarIIRDelay = new System.Windows.Forms.TrackBar();
			this.trackBarGain = new System.Windows.Forms.TrackBar();
			this.label14 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.label18 = new System.Windows.Forms.Label();
			this.label21 = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.label23 = new System.Windows.Forms.Label();
			this.pictureBoxSpectrum = new System.Windows.Forms.PictureBox();
			this.checkBoxMono = new System.Windows.Forms.CheckBox();
			this.checkBoxMonoInvert = new System.Windows.Forms.CheckBox();
			this.label24 = new System.Windows.Forms.Label();
			this.label25 = new System.Windows.Forms.Label();
			this.label26 = new System.Windows.Forms.Label();
			this.trackBarIIRDelayWet = new System.Windows.Forms.TrackBar();
			this.trackBarIIRDelayFeedback = new System.Windows.Forms.TrackBar();
			this.labelIIRDelay = new System.Windows.Forms.Label();
			this.label27 = new System.Windows.Forms.Label();
			this.checkBoxSoftSat = new System.Windows.Forms.CheckBox();
			this.trackBarSoftSat = new System.Windows.Forms.TrackBar();
			this.buttonPause = new System.Windows.Forms.Button();
			this.comboBoxStreamCopy = new System.Windows.Forms.ComboBox();
			this.label29 = new System.Windows.Forms.Label();
			this.checkBoxStreamCopy = new System.Windows.Forms.CheckBox();
			this.trackBarSoftSatDepth = new System.Windows.Forms.TrackBar();
			this.label30 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarEffect)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompressor)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarStereoEnhancerWide)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarStereoEnhancerWetDry)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarIIRDelay)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarGain)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxSpectrum)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarIIRDelayWet)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarIIRDelayFeedback)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSoftSat)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSoftSatDepth)).BeginInit();
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
			// progressBarPeak1Left
			// 
			this.progressBarPeak1Left.Location = new System.Drawing.Point(324, 24);
			this.progressBarPeak1Left.Maximum = 65535;
			this.progressBarPeak1Left.Name = "progressBarPeak1Left";
			this.progressBarPeak1Left.Size = new System.Drawing.Size(250, 12);
			this.progressBarPeak1Left.Step = 1;
			this.progressBarPeak1Left.TabIndex = 9;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(312, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(16, 12);
			this.label1.TabIndex = 8;
			this.label1.Text = "L";
			// 
			// progressBarPeak1Right
			// 
			this.progressBarPeak1Right.Location = new System.Drawing.Point(324, 40);
			this.progressBarPeak1Right.Maximum = 65535;
			this.progressBarPeak1Right.Name = "progressBarPeak1Right";
			this.progressBarPeak1Right.Size = new System.Drawing.Size(250, 12);
			this.progressBarPeak1Right.Step = 1;
			this.progressBarPeak1Right.TabIndex = 11;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(312, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(16, 12);
			this.label2.TabIndex = 10;
			this.label2.Text = "R";
			// 
			// buttonPlay
			// 
			this.buttonPlay.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.buttonPlay.Location = new System.Drawing.Point(10, 40);
			this.buttonPlay.Name = "buttonPlay";
			this.buttonPlay.Size = new System.Drawing.Size(92, 23);
			this.buttonPlay.TabIndex = 1;
			this.buttonPlay.Text = "PLAY";
			this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
			// 
			// labelTime
			// 
			this.labelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelTime.Location = new System.Drawing.Point(10, 152);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(260, 18);
			this.labelTime.TabIndex = 12;
			this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(446, 10);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(28, 23);
			this.label3.TabIndex = 6;
			this.label3.Text = "|0dB";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(534, 10);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(42, 23);
			this.label4.TabIndex = 7;
			this.label4.Text = "+6dB|";
			this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// textBoxGainDBValue
			// 
			this.textBoxGainDBValue.Location = new System.Drawing.Point(4, 408);
			this.textBoxGainDBValue.Name = "textBoxGainDBValue";
			this.textBoxGainDBValue.Size = new System.Drawing.Size(40, 21);
			this.textBoxGainDBValue.TabIndex = 2;
			this.textBoxGainDBValue.Text = "0";
			this.textBoxGainDBValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// buttonSetGain
			// 
			this.buttonSetGain.Location = new System.Drawing.Point(44, 408);
			this.buttonSetGain.Name = "buttonSetGain";
			this.buttonSetGain.Size = new System.Drawing.Size(54, 23);
			this.buttonSetGain.TabIndex = 3;
			this.buttonSetGain.Text = "Set Gain";
			this.buttonSetGain.Click += new System.EventHandler(this.buttonSetGain_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.WhiteSmoke;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBox1.Location = new System.Drawing.Point(10, 170);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(564, 116);
			this.pictureBox1.TabIndex = 15;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
			this.pictureBox1.Resize += new System.EventHandler(this.pictureBox1_Resize);
			// 
			// buttonZoom
			// 
			this.buttonZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonZoom.Location = new System.Drawing.Point(514, 146);
			this.buttonZoom.Name = "buttonZoom";
			this.buttonZoom.Size = new System.Drawing.Size(60, 23);
			this.buttonZoom.TabIndex = 13;
			this.buttonZoom.Text = "Zoom";
			this.buttonZoom.Click += new System.EventHandler(this.buttonZoom_Click);
			// 
			// buttonStop
			// 
			this.buttonStop.Enabled = false;
			this.buttonStop.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.buttonStop.Location = new System.Drawing.Point(108, 40);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size(92, 23);
			this.buttonStop.TabIndex = 4;
			this.buttonStop.Text = "STOP";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// progressBarPeak2Right
			// 
			this.progressBarPeak2Right.Location = new System.Drawing.Point(324, 114);
			this.progressBarPeak2Right.Maximum = 65535;
			this.progressBarPeak2Right.Name = "progressBarPeak2Right";
			this.progressBarPeak2Right.Size = new System.Drawing.Size(250, 12);
			this.progressBarPeak2Right.Step = 1;
			this.progressBarPeak2Right.TabIndex = 22;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(312, 114);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(16, 12);
			this.label6.TabIndex = 21;
			this.label6.Text = "R";
			// 
			// progressBarPeak2Left
			// 
			this.progressBarPeak2Left.Location = new System.Drawing.Point(324, 98);
			this.progressBarPeak2Left.Maximum = 65535;
			this.progressBarPeak2Left.Name = "progressBarPeak2Left";
			this.progressBarPeak2Left.Size = new System.Drawing.Size(250, 12);
			this.progressBarPeak2Left.Step = 1;
			this.progressBarPeak2Left.TabIndex = 20;
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(312, 98);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(16, 12);
			this.label7.TabIndex = 19;
			this.label7.Text = "L";
			// 
			// labelLevel2
			// 
			this.labelLevel2.Location = new System.Drawing.Point(322, 124);
			this.labelLevel2.Name = "labelLevel2";
			this.labelLevel2.Size = new System.Drawing.Size(250, 16);
			this.labelLevel2.TabIndex = 23;
			this.labelLevel2.Text = "Peak: 0 dB";
			this.labelLevel2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// trackBarEffect
			// 
			this.trackBarEffect.AutoSize = false;
			this.trackBarEffect.Enabled = false;
			this.trackBarEffect.LargeChange = 1;
			this.trackBarEffect.Location = new System.Drawing.Point(316, 68);
			this.trackBarEffect.Maximum = 32768;
			this.trackBarEffect.Minimum = -32768;
			this.trackBarEffect.Name = "trackBarEffect";
			this.trackBarEffect.Size = new System.Drawing.Size(266, 18);
			this.trackBarEffect.TabIndex = 24;
			this.trackBarEffect.TickFrequency = 32768;
			this.trackBarEffect.TickStyle = System.Windows.Forms.TickStyle.None;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(320, 10);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(38, 23);
			this.label5.TabIndex = 25;
			this.label5.Text = "|-90dB";
			// 
			// labelLevel1
			// 
			this.labelLevel1.Location = new System.Drawing.Point(322, 50);
			this.labelLevel1.Name = "labelLevel1";
			this.labelLevel1.Size = new System.Drawing.Size(250, 16);
			this.labelLevel1.TabIndex = 26;
			this.labelLevel1.Text = "Peak: 0 dB";
			this.labelLevel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(446, 84);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(28, 23);
			this.label8.TabIndex = 27;
			this.label8.Text = "|0dB";
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(534, 84);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(42, 23);
			this.label9.TabIndex = 28;
			this.label9.Text = "+6dB|";
			this.label9.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(320, 84);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(38, 23);
			this.label10.TabIndex = 29;
			this.label10.Text = "|-90dB";
			// 
			// checkBoxDAmp
			// 
			this.checkBoxDAmp.Location = new System.Drawing.Point(10, 290);
			this.checkBoxDAmp.Name = "checkBoxDAmp";
			this.checkBoxDAmp.Size = new System.Drawing.Size(118, 24);
			this.checkBoxDAmp.TabIndex = 30;
			this.checkBoxDAmp.Text = "Dyn. Amplification";
			this.checkBoxDAmp.CheckedChanged += new System.EventHandler(this.checkBoxDAmp_CheckedChanged);
			// 
			// checkBoxLevel1Bypass
			// 
			this.checkBoxLevel1Bypass.Checked = true;
			this.checkBoxLevel1Bypass.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxLevel1Bypass.Location = new System.Drawing.Point(296, 30);
			this.checkBoxLevel1Bypass.Name = "checkBoxLevel1Bypass";
			this.checkBoxLevel1Bypass.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.checkBoxLevel1Bypass.Size = new System.Drawing.Size(16, 21);
			this.checkBoxLevel1Bypass.TabIndex = 31;
			this.checkBoxLevel1Bypass.CheckedChanged += new System.EventHandler(this.checkBoxLevel1Bypass_CheckedChanged);
			// 
			// checkBoxLevel2Bypass
			// 
			this.checkBoxLevel2Bypass.Checked = true;
			this.checkBoxLevel2Bypass.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxLevel2Bypass.Location = new System.Drawing.Point(296, 104);
			this.checkBoxLevel2Bypass.Name = "checkBoxLevel2Bypass";
			this.checkBoxLevel2Bypass.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.checkBoxLevel2Bypass.Size = new System.Drawing.Size(16, 21);
			this.checkBoxLevel2Bypass.TabIndex = 32;
			this.checkBoxLevel2Bypass.CheckedChanged += new System.EventHandler(this.checkBoxLevel2Bypass_CheckedChanged);
			// 
			// checkBoxCompressor
			// 
			this.checkBoxCompressor.Location = new System.Drawing.Point(502, 406);
			this.checkBoxCompressor.Name = "checkBoxCompressor";
			this.checkBoxCompressor.Size = new System.Drawing.Size(82, 24);
			this.checkBoxCompressor.TabIndex = 33;
			this.checkBoxCompressor.Text = "Compressor";
			this.checkBoxCompressor.CheckedChanged += new System.EventHandler(this.checkBoxCompressor_CheckedChanged);
			// 
			// trackBarCompressor
			// 
			this.trackBarCompressor.Location = new System.Drawing.Point(498, 306);
			this.trackBarCompressor.Maximum = 0;
			this.trackBarCompressor.Minimum = -250;
			this.trackBarCompressor.Name = "trackBarCompressor";
			this.trackBarCompressor.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarCompressor.Size = new System.Drawing.Size(45, 104);
			this.trackBarCompressor.TabIndex = 34;
			this.trackBarCompressor.TickFrequency = 25;
			this.trackBarCompressor.Value = -60;
			this.trackBarCompressor.ValueChanged += new System.EventHandler(this.trackBarCompressor_ValueChanged);
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(528, 386);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(38, 23);
			this.label11.TabIndex = 35;
			this.label11.Text = "-25dB";
			this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelCompThreshold
			// 
			this.labelCompThreshold.Location = new System.Drawing.Point(476, 290);
			this.labelCompThreshold.Name = "labelCompThreshold";
			this.labelCompThreshold.Size = new System.Drawing.Size(110, 23);
			this.labelCompThreshold.TabIndex = 36;
			this.labelCompThreshold.Text = "Threshold: -6.0 dB";
			this.labelCompThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label13
			// 
			this.label13.Location = new System.Drawing.Point(528, 308);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(38, 23);
			this.label13.TabIndex = 37;
			this.label13.Text = "-0dB";
			this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(10, 70);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(244, 23);
			this.label12.TabIndex = 38;
			this.label12.Text = "Gain -> St.Enh -> DAmp -> Compressor -> EQ";
			// 
			// checkBoxGainDither
			// 
			this.checkBoxGainDither.Location = new System.Drawing.Point(324, 146);
			this.checkBoxGainDither.Name = "checkBoxGainDither";
			this.checkBoxGainDither.Size = new System.Drawing.Size(92, 24);
			this.checkBoxGainDither.TabIndex = 39;
			this.checkBoxGainDither.Text = "Use Dithering";
			this.checkBoxGainDither.CheckedChanged += new System.EventHandler(this.checkBoxGainDither_CheckedChanged);
			// 
			// trackBarStereoEnhancerWide
			// 
			this.trackBarStereoEnhancerWide.Location = new System.Drawing.Point(202, 306);
			this.trackBarStereoEnhancerWide.Maximum = 900;
			this.trackBarStereoEnhancerWide.Name = "trackBarStereoEnhancerWide";
			this.trackBarStereoEnhancerWide.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarStereoEnhancerWide.Size = new System.Drawing.Size(45, 104);
			this.trackBarStereoEnhancerWide.TabIndex = 40;
			this.trackBarStereoEnhancerWide.TickFrequency = 90;
			this.trackBarStereoEnhancerWide.Value = 300;
			this.trackBarStereoEnhancerWide.ValueChanged += new System.EventHandler(this.trackBarStereoEnhancerWide_ValueChanged);
			// 
			// checkBoxStereoEnhancer
			// 
			this.checkBoxStereoEnhancer.Location = new System.Drawing.Point(208, 406);
			this.checkBoxStereoEnhancer.Name = "checkBoxStereoEnhancer";
			this.checkBoxStereoEnhancer.Size = new System.Drawing.Size(106, 24);
			this.checkBoxStereoEnhancer.TabIndex = 41;
			this.checkBoxStereoEnhancer.Text = "Stereo Enhancer";
			this.checkBoxStereoEnhancer.CheckedChanged += new System.EventHandler(this.checkBoxStereoEnhancer_CheckedChanged);
			// 
			// trackBarStereoEnhancerWetDry
			// 
			this.trackBarStereoEnhancerWetDry.Location = new System.Drawing.Point(254, 306);
			this.trackBarStereoEnhancerWetDry.Maximum = 100;
			this.trackBarStereoEnhancerWetDry.Name = "trackBarStereoEnhancerWetDry";
			this.trackBarStereoEnhancerWetDry.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarStereoEnhancerWetDry.Size = new System.Drawing.Size(45, 104);
			this.trackBarStereoEnhancerWetDry.TabIndex = 42;
			this.trackBarStereoEnhancerWetDry.TickFrequency = 10;
			this.trackBarStereoEnhancerWetDry.Value = 70;
			this.trackBarStereoEnhancerWetDry.ValueChanged += new System.EventHandler(this.trackBarStereoEnhancerWetDry_ValueChanged);
			// 
			// labelStereoEnhancer
			// 
			this.labelStereoEnhancer.Location = new System.Drawing.Point(204, 290);
			this.labelStereoEnhancer.Name = "labelStereoEnhancer";
			this.labelStereoEnhancer.Size = new System.Drawing.Size(108, 23);
			this.labelStereoEnhancer.TabIndex = 43;
			this.labelStereoEnhancer.Text = "Wide: 3,00 / 0,70";
			this.labelStereoEnhancer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(284, 386);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(38, 23);
			this.label15.TabIndex = 44;
			this.label15.Text = "Dry";
			this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(284, 308);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(38, 23);
			this.label16.TabIndex = 45;
			this.label16.Text = "Wet";
			this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label19
			// 
			this.label19.Location = new System.Drawing.Point(232, 386);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(24, 23);
			this.label19.TabIndex = 47;
			this.label19.Text = "0.0";
			this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label20
			// 
			this.label20.Location = new System.Drawing.Point(232, 308);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(24, 23);
			this.label20.TabIndex = 48;
			this.label20.Text = "9.0";
			this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// checkBoxIIRDelay
			// 
			this.checkBoxIIRDelay.Location = new System.Drawing.Point(332, 406);
			this.checkBoxIIRDelay.Name = "checkBoxIIRDelay";
			this.checkBoxIIRDelay.Size = new System.Drawing.Size(72, 24);
			this.checkBoxIIRDelay.TabIndex = 51;
			this.checkBoxIIRDelay.Text = "IIR Delay";
			this.checkBoxIIRDelay.CheckedChanged += new System.EventHandler(this.checkBoxIIRDelay_CheckedChanged);
			// 
			// trackBarIIRDelay
			// 
			this.trackBarIIRDelay.LargeChange = 1024;
			this.trackBarIIRDelay.Location = new System.Drawing.Point(326, 306);
			this.trackBarIIRDelay.Maximum = 88200;
			this.trackBarIIRDelay.Name = "trackBarIIRDelay";
			this.trackBarIIRDelay.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarIIRDelay.Size = new System.Drawing.Size(45, 104);
			this.trackBarIIRDelay.TabIndex = 52;
			this.trackBarIIRDelay.TickFrequency = 10000;
			this.trackBarIIRDelay.Value = 24576;
			this.trackBarIIRDelay.ValueChanged += new System.EventHandler(this.trackBarIIRDelay_ValueChanged);
			// 
			// trackBarGain
			// 
			this.trackBarGain.LargeChange = 100;
			this.trackBarGain.Location = new System.Drawing.Point(4, 306);
			this.trackBarGain.Maximum = 16000;
			this.trackBarGain.Minimum = -16000;
			this.trackBarGain.Name = "trackBarGain";
			this.trackBarGain.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarGain.Size = new System.Drawing.Size(45, 104);
			this.trackBarGain.SmallChange = 10;
			this.trackBarGain.TabIndex = 53;
			this.trackBarGain.TickFrequency = 2000;
			this.trackBarGain.ValueChanged += new System.EventHandler(this.trackBarGain_ValueChanged);
			// 
			// label14
			// 
			this.label14.Location = new System.Drawing.Point(34, 386);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(38, 23);
			this.label14.TabIndex = 54;
			this.label14.Text = "-16dB";
			this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label17
			// 
			this.label17.Location = new System.Drawing.Point(34, 308);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(38, 23);
			this.label17.TabIndex = 55;
			this.label17.Text = "+16dB";
			this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label18
			// 
			this.label18.Location = new System.Drawing.Point(232, 360);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(24, 23);
			this.label18.TabIndex = 56;
			this.label18.Text = "3.0";
			this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label21
			// 
			this.label21.Location = new System.Drawing.Point(284, 332);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(24, 23);
			this.label21.TabIndex = 57;
			this.label21.Text = "0.7";
			this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label22
			// 
			this.label22.Location = new System.Drawing.Point(528, 326);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(38, 23);
			this.label22.TabIndex = 58;
			this.label22.Text = "-6dB";
			this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label23
			// 
			this.label23.Location = new System.Drawing.Point(34, 348);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(38, 23);
			this.label23.TabIndex = 59;
			this.label23.Text = "0dB";
			this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// pictureBoxSpectrum
			// 
			this.pictureBoxSpectrum.BackColor = System.Drawing.Color.Black;
			this.pictureBoxSpectrum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBoxSpectrum.Location = new System.Drawing.Point(10, 90);
			this.pictureBoxSpectrum.Name = "pictureBoxSpectrum";
			this.pictureBoxSpectrum.Size = new System.Drawing.Size(260, 62);
			this.pictureBoxSpectrum.TabIndex = 62;
			this.pictureBoxSpectrum.TabStop = false;
			this.pictureBoxSpectrum.Click += new System.EventHandler(this.pictureBoxSpectrum_Click);
			// 
			// checkBoxMono
			// 
			this.checkBoxMono.Location = new System.Drawing.Point(422, 146);
			this.checkBoxMono.Name = "checkBoxMono";
			this.checkBoxMono.Size = new System.Drawing.Size(52, 24);
			this.checkBoxMono.TabIndex = 63;
			this.checkBoxMono.Text = "Mono";
			this.checkBoxMono.CheckedChanged += new System.EventHandler(this.checkBoxMono_CheckedChanged);
			// 
			// checkBoxMonoInvert
			// 
			this.checkBoxMonoInvert.Location = new System.Drawing.Point(476, 146);
			this.checkBoxMonoInvert.Name = "checkBoxMonoInvert";
			this.checkBoxMonoInvert.Size = new System.Drawing.Size(42, 24);
			this.checkBoxMonoInvert.TabIndex = 64;
			this.checkBoxMonoInvert.Text = "Inv";
			this.checkBoxMonoInvert.CheckedChanged += new System.EventHandler(this.checkBoxMonoInvert_CheckedChanged);
			// 
			// label24
			// 
			this.label24.Location = new System.Drawing.Point(392, 332);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(24, 23);
			this.label24.TabIndex = 68;
			this.label24.Text = "0.7";
			this.label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label25
			// 
			this.label25.Location = new System.Drawing.Point(392, 386);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(38, 23);
			this.label25.TabIndex = 66;
			this.label25.Text = "Dry";
			this.label25.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label26
			// 
			this.label26.Location = new System.Drawing.Point(392, 308);
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size(38, 23);
			this.label26.TabIndex = 67;
			this.label26.Text = "Wet";
			this.label26.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// trackBarIIRDelayWet
			// 
			this.trackBarIIRDelayWet.Location = new System.Drawing.Point(362, 306);
			this.trackBarIIRDelayWet.Maximum = 100;
			this.trackBarIIRDelayWet.Name = "trackBarIIRDelayWet";
			this.trackBarIIRDelayWet.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarIIRDelayWet.Size = new System.Drawing.Size(45, 104);
			this.trackBarIIRDelayWet.TabIndex = 65;
			this.trackBarIIRDelayWet.TickFrequency = 10;
			this.trackBarIIRDelayWet.Value = 50;
			this.trackBarIIRDelayWet.ValueChanged += new System.EventHandler(this.trackBarIIRDelayWetDry_ValueChanged);
			// 
			// trackBarIIRDelayFeedback
			// 
			this.trackBarIIRDelayFeedback.Location = new System.Drawing.Point(414, 306);
			this.trackBarIIRDelayFeedback.Maximum = 100;
			this.trackBarIIRDelayFeedback.Name = "trackBarIIRDelayFeedback";
			this.trackBarIIRDelayFeedback.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarIIRDelayFeedback.Size = new System.Drawing.Size(45, 104);
			this.trackBarIIRDelayFeedback.TabIndex = 69;
			this.trackBarIIRDelayFeedback.TickFrequency = 10;
			this.trackBarIIRDelayFeedback.Value = 50;
			this.trackBarIIRDelayFeedback.ValueChanged += new System.EventHandler(this.trackBarIIRDelayFeedback_ValueChanged);
			// 
			// labelIIRDelay
			// 
			this.labelIIRDelay.Location = new System.Drawing.Point(322, 292);
			this.labelIIRDelay.Name = "labelIIRDelay";
			this.labelIIRDelay.Size = new System.Drawing.Size(156, 23);
			this.labelIIRDelay.TabIndex = 71;
			this.labelIIRDelay.Text = "Delay: 4096 samples";
			this.labelIIRDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label27
			// 
			this.label27.Location = new System.Drawing.Point(400, 404);
			this.label27.Name = "label27";
			this.label27.Size = new System.Drawing.Size(66, 23);
			this.label27.TabIndex = 72;
			this.label27.Text = "Feedback";
			this.label27.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// checkBoxSoftSat
			// 
			this.checkBoxSoftSat.Location = new System.Drawing.Point(138, 406);
			this.checkBoxSoftSat.Name = "checkBoxSoftSat";
			this.checkBoxSoftSat.Size = new System.Drawing.Size(70, 24);
			this.checkBoxSoftSat.TabIndex = 74;
			this.checkBoxSoftSat.Text = "Soft Sat.";
			this.checkBoxSoftSat.CheckedChanged += new System.EventHandler(this.checkBoxSoftSat_CheckedChanged);
			// 
			// trackBarSoftSat
			// 
			this.trackBarSoftSat.Location = new System.Drawing.Point(134, 306);
			this.trackBarSoftSat.Maximum = 100;
			this.trackBarSoftSat.Name = "trackBarSoftSat";
			this.trackBarSoftSat.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarSoftSat.Size = new System.Drawing.Size(45, 104);
			this.trackBarSoftSat.TabIndex = 75;
			this.trackBarSoftSat.TickFrequency = 10;
			this.trackBarSoftSat.Value = 50;
			this.trackBarSoftSat.ValueChanged += new System.EventHandler(this.trackBarSoftSat_ValueChanged);
			// 
			// buttonPause
			// 
			this.buttonPause.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.buttonPause.Location = new System.Drawing.Point(208, 40);
			this.buttonPause.Name = "buttonPause";
			this.buttonPause.Size = new System.Drawing.Size(62, 23);
			this.buttonPause.TabIndex = 76;
			this.buttonPause.Text = "PAUSE";
			this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
			// 
			// comboBoxStreamCopy
			// 
			this.comboBoxStreamCopy.Location = new System.Drawing.Point(96, 444);
			this.comboBoxStreamCopy.Name = "comboBoxStreamCopy";
			this.comboBoxStreamCopy.Size = new System.Drawing.Size(176, 21);
			this.comboBoxStreamCopy.TabIndex = 77;
			this.comboBoxStreamCopy.SelectedIndexChanged += new System.EventHandler(this.comboBoxStreamCopy_SelectedIndexChanged);
			// 
			// label29
			// 
			this.label29.Location = new System.Drawing.Point(4, 444);
			this.label29.Name = "label29";
			this.label29.Size = new System.Drawing.Size(88, 23);
			this.label29.TabIndex = 78;
			this.label29.Text = "StreamCopy To:";
			this.label29.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// checkBoxStreamCopy
			// 
			this.checkBoxStreamCopy.Checked = true;
			this.checkBoxStreamCopy.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxStreamCopy.Location = new System.Drawing.Point(278, 444);
			this.checkBoxStreamCopy.Name = "checkBoxStreamCopy";
			this.checkBoxStreamCopy.Size = new System.Drawing.Size(124, 21);
			this.checkBoxStreamCopy.TabIndex = 79;
			this.checkBoxStreamCopy.Text = "with DSP Effects";
			this.checkBoxStreamCopy.CheckedChanged += new System.EventHandler(this.checkBoxStreamCopy_CheckedChanged);
			// 
			// trackBarSoftSatDepth
			// 
			this.trackBarSoftSatDepth.Location = new System.Drawing.Point(164, 306);
			this.trackBarSoftSatDepth.Maximum = 100;
			this.trackBarSoftSatDepth.Name = "trackBarSoftSatDepth";
			this.trackBarSoftSatDepth.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarSoftSatDepth.Size = new System.Drawing.Size(45, 104);
			this.trackBarSoftSatDepth.TabIndex = 80;
			this.trackBarSoftSatDepth.TickFrequency = 10;
			this.trackBarSoftSatDepth.Value = 50;
			this.trackBarSoftSatDepth.ValueChanged += new System.EventHandler(this.trackBarSoftSatDepth_ValueChanged);
			// 
			// label30
			// 
			this.label30.Location = new System.Drawing.Point(138, 290);
			this.label30.Name = "label30";
			this.label30.Size = new System.Drawing.Size(60, 23);
			this.label30.TabIndex = 81;
			this.label30.Text = "Sat./Depth";
			this.label30.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// TestDSP
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(586, 484);
			this.Controls.Add(this.label30);
			this.Controls.Add(this.trackBarSoftSatDepth);
			this.Controls.Add(this.checkBoxStreamCopy);
			this.Controls.Add(this.label29);
			this.Controls.Add(this.comboBoxStreamCopy);
			this.Controls.Add(this.buttonPause);
			this.Controls.Add(this.trackBarSoftSat);
			this.Controls.Add(this.checkBoxSoftSat);
			this.Controls.Add(this.label27);
			this.Controls.Add(this.labelIIRDelay);
			this.Controls.Add(this.trackBarIIRDelayFeedback);
			this.Controls.Add(this.label24);
			this.Controls.Add(this.label25);
			this.Controls.Add(this.label26);
			this.Controls.Add(this.trackBarIIRDelayWet);
			this.Controls.Add(this.buttonZoom);
			this.Controls.Add(this.checkBoxMonoInvert);
			this.Controls.Add(this.checkBoxMono);
			this.Controls.Add(this.pictureBoxSpectrum);
			this.Controls.Add(this.label17);
			this.Controls.Add(this.checkBoxDAmp);
			this.Controls.Add(this.buttonSetGain);
			this.Controls.Add(this.textBoxGainDBValue);
			this.Controls.Add(this.label23);
			this.Controls.Add(this.label22);
			this.Controls.Add(this.label13);
			this.Controls.Add(this.label21);
			this.Controls.Add(this.label18);
			this.Controls.Add(this.label14);
			this.Controls.Add(this.trackBarGain);
			this.Controls.Add(this.trackBarIIRDelay);
			this.Controls.Add(this.checkBoxIIRDelay);
			this.Controls.Add(this.checkBoxLevel2Bypass);
			this.Controls.Add(this.checkBoxLevel1Bypass);
			this.Controls.Add(this.label19);
			this.Controls.Add(this.label20);
			this.Controls.Add(this.label15);
			this.Controls.Add(this.label16);
			this.Controls.Add(this.labelStereoEnhancer);
			this.Controls.Add(this.checkBoxStereoEnhancer);
			this.Controls.Add(this.checkBoxGainDither);
			this.Controls.Add(this.label12);
			this.Controls.Add(this.labelCompThreshold);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.progressBarPeak2Right);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.progressBarPeak2Left);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.labelTime);
			this.Controls.Add(this.buttonPlay);
			this.Controls.Add(this.progressBarPeak1Right);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.progressBarPeak1Left);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.trackBarEffect);
			this.Controls.Add(this.labelLevel2);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.labelLevel1);
			this.Controls.Add(this.checkBoxCompressor);
			this.Controls.Add(this.trackBarCompressor);
			this.Controls.Add(this.trackBarStereoEnhancerWetDry);
			this.Controls.Add(this.trackBarStereoEnhancerWide);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "TestDSP";
			this.Text = "Test DSP";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.TestDSP_Closing);
			this.Load += new System.EventHandler(this.TestDSP_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarEffect)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompressor)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarStereoEnhancerWide)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarStereoEnhancerWetDry)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarIIRDelay)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarGain)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxSpectrum)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarIIRDelayWet)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarIIRDelayFeedback)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSoftSat)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarSoftSatDepth)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new TestDSP());
		}

		private void TestDSP_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your email", "your regkey");

			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY, this.Handle) )
			{
				_info = Bass.BASS_GetInfo();
				_deviceLatencyMS = _info.latency;
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );

			BassFx.BASS_FX_GetVersion(); 

			// init some FX
			_comp.Preset_Soft();
			_damp.Preset_Medium();

			// create a secure timer
			_updateTimer = new Un4seen.Bass.BASSTimer(_updateInterval);
			_updateTimer.Tick += new EventHandler(timerUpdate_Tick);

			_sync = new SYNCPROC(SetPosition);

			_visModified.MaxFFT = BASSData.BASS_DATA_FFT1024;
			_visModified.MaxFrequencySpectrum = Utils.FFTFrequency2Index( 16000, 1024, 44100 );
			
			comboBoxStreamCopy.Items.AddRange( Bass.BASS_GetDeviceInfos() );
			comboBoxStreamCopy.SelectedIndex = -1;
		}

		private void TestDSP_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
			_updateTimer.Tick -= new EventHandler(timerUpdate_Tick);
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
				}
				else
					_fileName = String.Empty;
			}
		}

		private void buttonPlay_Click(object sender, System.EventArgs e)
		{
			_updateTimer.Stop();
			Bass.BASS_StreamFree(_stream);
			if (_fileName != String.Empty)
			{
				// create the stream
				_stream = Bass.BASS_StreamCreateFile(_fileName, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_SPEAKER_FRONT);
				if (_stream != 0)
				{
					// latency from milliseconds to bytes
					_deviceLatencyBytes = (int)Bass.BASS_ChannelSeconds2Bytes(_stream, _deviceLatencyMS/1000f);

					if (_plm1 != null)
						_plm1.Notification -= new EventHandler(_plm1_Notification);
					// set up a ready-made DSP (here the PeakLevelMeter)
					_plm1 = new DSP_PeakLevelMeter(_stream, 2000);
					_plm1.CalcRMS = true;
					_plm1.Notification += new EventHandler(_plm1_Notification);
					checkBoxLevel1Bypass_CheckedChanged(this, EventArgs.Empty);

					checkBoxMono.Checked = false;
					_mono = new DSP_Mono();
					
					comboBoxStreamCopy.SelectedIndex = -1;
					checkBoxStreamCopy.Checked = true;

					_gain = new DSP_Gain(_stream, 0);
					buttonSetGain_Click(this, EventArgs.Empty);
					trackBarGain_ValueChanged(this, EventArgs.Empty);

					_stereoEnh = new DSP_StereoEnhancer(_stream, 0);
					checkBoxStereoEnhancer_CheckedChanged(this, EventArgs.Empty);
					trackBarStereoEnhancerWetDry_ValueChanged(this, EventArgs.Empty);
					trackBarStereoEnhancerWide_ValueChanged(this, EventArgs.Empty);

					_delay = new DSP_IIRDelay(_stream, 0, 2f);
					checkBoxIIRDelay_CheckedChanged(this, EventArgs.Empty);
					trackBarIIRDelay_ValueChanged(this, EventArgs.Empty);
					trackBarIIRDelayWetDry_ValueChanged(this, EventArgs.Empty);
					trackBarIIRDelayFeedback_ValueChanged(this, EventArgs.Empty);

					_softSat = new DSP_SoftSaturation(_stream, 0);
					checkBoxSoftSat_CheckedChanged(this, EventArgs.Empty);
					trackBarSoftSat_ValueChanged(this, EventArgs.Empty);
					trackBarSoftSatDepth_ValueChanged(this, EventArgs.Empty);

					checkBoxDAmp_CheckedChanged(this, EventArgs.Empty);
					
					checkBoxCompressor_CheckedChanged(this, EventArgs.Empty);
					trackBarCompressor_ValueChanged(this, EventArgs.Empty);

					checkBoxGainDither_CheckedChanged(this, EventArgs.Empty);
					
					if (_plm2 != null)
						_plm2.Notification -= new EventHandler(_plm2_Notification);
					_plm2 = new DSP_PeakLevelMeter(_stream, -2000);
					_plm2.CalcRMS = true;
					_plm2.Notification += new EventHandler(_plm2_Notification);
					checkBoxLevel2Bypass_CheckedChanged(this, EventArgs.Empty);
				}

				if (_stream != 0 && Bass.BASS_ChannelPlay(_stream, false) )
				{
					// render wave form (this is done in a background thread, so that we already play the channel in parallel)
					if (this._zoomed)
						this.buttonZoom.PerformClick();
					GetWaveForm();

					Console.WriteLine( "Playing" );

					_updateTimer.Start();

					this.buttonStop.Enabled = true;
					this.buttonPlay.Enabled = false;
				}
				else
				{
                    Console.WriteLine("Error = {0}", Bass.BASS_ErrorGetCode());
				}
			}
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			StopStream();
		}

		private void buttonPause_Click(object sender, System.EventArgs e)
		{
			if (Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING)
			{
				Bass.BASS_ChannelPause(_stream);
			}
			else
			{
				Bass.BASS_ChannelPlay(_stream, false);
			}
		}

		private void StopStream()
		{
			_updateTimer.Stop();
			// kills rendering, if still in progress, e.g. if a large file was selected
			if (WF != null && WF.IsRenderingInProgress)
				WF.RenderStop();
			DrawWavePosition(-1, -1);

			Bass.BASS_StreamFree(_stream);
			_stream = 0;
			this.labelTime.Text = "Stopped";
			this.button1.Text = "Select a file to play (e.g. MP3, OGG or WAV)...";
			this.buttonStop.Enabled = false;
			this.buttonPlay.Enabled = true;
		}

		private void timerUpdate_Tick(object sender, System.EventArgs e)
		{
			// here we gather info about the stream, when it is playing...
			if ( Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_STOPPED )
			{
				// the stream is NOT playing anymore...
				//StopStream();
				return;
			}

			// from here on, the stream is for sure playing...
			_tickCounter++;
			long pos = Bass.BASS_ChannelGetPosition(_stream); // position in bytes
			long len = Bass.BASS_ChannelGetLength(_stream); // length in bytes

			if (_tickCounter == 20)
			{
				_tickCounter = 0;
				// reset the peak level every 1000ms (since timer is 50ms)
				if (_plm1 != null)
					_plm1.ResetPeakHold();
				if (_plm2 != null)
					_plm2.ResetPeakHold();
			}
			if (_tickCounter % 5 == 0)
			{
				// display the position every 250ms (since timer is 50ms)
				double totaltime = Bass.BASS_ChannelBytes2Seconds(_stream, len); // the total time length
                double elapsedtime = Bass.BASS_ChannelBytes2Seconds(_stream, pos); // the elapsed time length
                double remainingtime = totaltime - elapsedtime;
				this.labelTime.Text = String.Format( "Elapsed: {0:#0.00} - Total: {1:#0.00} - Remain: {2:#0.00}", Utils.FixTimespan(elapsedtime,"MMSS"), Utils.FixTimespan(totaltime,"MMSS"), Utils.FixTimespan(remainingtime,"MMSS"));
				this.Text = String.Format( "Bass-CPU: {0:0.00}% (not including Waves & Spectrum!)", Bass.BASS_GetCPU() );
			}
			
			// update the wave position
			DrawWavePosition(pos, len);
			if (_fullSpectrum)
				this.pictureBoxSpectrum.Image = _visModified.CreateSpectrumLinePeak(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Wheat, Color.Gold, Color.DarkOrange, Color.Black, 2, 1, 1, 13, false, true, false);
			else
				this.pictureBoxSpectrum.Image = _visModified.CreateWaveForm(_stream, this.pictureBoxSpectrum.Width, this.pictureBoxSpectrum.Height, Color.Green, Color.Red, Color.Gray, Color.Linen, 1, true, false, true);
		}
		
		private void pictureBoxSpectrum_Click(object sender, System.EventArgs e)
		{
			_fullSpectrum = !_fullSpectrum;
		}
		
		#region Wave Form 

		// zoom helper varibales
		private bool _zoomed = false;
		private int _zoomStart = -1;
		private long _zoomStartBytes = -1;
		private int _zoomEnd = -1;
		private float _zoomDistance = 5.0f; // zoom = 5sec.

		private Un4seen.Bass.Misc.WaveForm WF = null;
		private void GetWaveForm()
		{
			// render a wave form2
			WF = new WaveForm(this._fileName, new WAVEFORMPROC(MyWaveFormCallback), this);
			WF.FrameResolution = 0.005f; // 5ms are very nice
			WF.CallbackFrequency = 4000; // every 20 seconds rendered (4000*5ms=20sec)
			WF.ColorBackground = SystemColors.Control;
			WF.ColorLeft = Color.Gainsboro;
			WF.ColorLeftEnvelope = Color.Gray;
			WF.ColorRight = Color.LightGray;
			WF.ColorRightEnvelope = Color.DimGray;
			WF.ColorMarker = Color.DarkBlue;
			WF.DrawWaveForm = WaveForm.WAVEFORMDRAWTYPE.Stereo;
			WF.DrawMarker = WaveForm.MARKERDRAWTYPE.Line | WaveForm.MARKERDRAWTYPE.Name | WaveForm.MARKERDRAWTYPE.NamePositionAlternate;
			WF.RenderStart( true, BASSFlag.BASS_DEFAULT );
			WF.SyncPlayback(_stream);
		}

		private void MyWaveFormCallback(int framesDone, int framesTotal, TimeSpan elapsedTime, bool finished)
		{
			// will be called during rendering...
			DrawWave();
			if (finished)
			{
				Console.WriteLine( "Finished rendering in {0}sec.", elapsedTime);
				Console.WriteLine( "FramesRendered={0} of {1}", WF.FramesRendered, WF.FramesToRender);
				// eg.g use this to save the rendered wave form...
				//WF.WaveFormSaveToFile( Path.ChangeExtension(_fileName, ".wf") );
			}
		}

		private void pictureBox1_Resize(object sender, System.EventArgs e)
		{
			DrawWave();
		}

		private void DrawWave()
		{
			if (WF != null)
				this.pictureBox1.BackgroundImage = WF.CreateBitmap( this.pictureBox1.Width, this.pictureBox1.Height, _zoomStart, _zoomEnd, true);
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
					len = WF.Frame2Bytes(_zoomEnd) - _zoomStartBytes;

					int scrollOffset = 1;
					// if we scroll out the window...(scrollOffset*20ms before the zoom window ends)
					if ( pos > (_zoomStartBytes + len - scrollOffset*WF.Wave.bpf) )
					{
						// we 'scroll' our zoom with a little offset
						_zoomStart = WF.Position2Frames(pos - scrollOffset*WF.Wave.bpf);
						_zoomStartBytes = WF.Frame2Bytes(_zoomStart);
						_zoomEnd = _zoomStart + WF.Position2Frames( _zoomDistance ) - 1;
						if (_zoomEnd >= WF.Wave.data.Length)
						{
							// beyond the end, so we zoom from end - _zoomDistance.
							_zoomEnd = WF.Wave.data.Length-1;
							_zoomStart = _zoomEnd - WF.Position2Frames( _zoomDistance ) + 1;
							if (_zoomStart < 0)
								_zoomStart = 0;
							_zoomStartBytes = WF.Frame2Bytes(_zoomStart);
							// total length doesn't have to be _zoomDistance sec. here
							len = WF.Frame2Bytes(_zoomEnd) - _zoomStartBytes;
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
				g.Clear( Color.White );
				int x = (int)Math.Round(pos/bpp);  // position (x) where to draw the line
				g.DrawLine( p, x, 0, x,  this.pictureBox1.Height-1);
				bitmap.MakeTransparent( Color.White );
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
			if (WF == null)
				return;

			// WF is not null, so the stream must be playing...
			if (_zoomed)
			{
				// unzoom...(display the whole wave form)
				_zoomStart = -1;
				_zoomStartBytes = -1;
				_zoomEnd = -1;
				_zoomDistance = 5.0f; // zoom = 5sec.
			}
			else
			{
				// zoom...(display only a partial wave form)
				long pos = Bass.BASS_ChannelGetPosition(this._stream);
				// calculate the window to display
				_zoomStart = WF.Position2Frames(pos);
				_zoomStartBytes = WF.Frame2Bytes(_zoomStart);
				_zoomEnd = _zoomStart + WF.Position2Frames( _zoomDistance ) - 1;
				if (_zoomEnd >= WF.Wave.data.Length)
				{
					// beyond the end, so we zoom from end - _zoomDistance.
					_zoomEnd = WF.Wave.data.Length-1;
					_zoomStart = _zoomEnd - WF.Position2Frames( _zoomDistance ) + 1;
					_zoomStartBytes = WF.Frame2Bytes(_zoomStart);
				}
			}
			_zoomed = !_zoomed;
			// and display this new wave form
			DrawWave();
		}

        private void pictureBox1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (WF == null)
                return;

            if (e.Button == MouseButtons.Left)
            {
                long pos = WF.GetBytePositionFromX(e.X, this.pictureBox1.Width, _zoomStart, _zoomEnd);
                // set Start marker
                WF.AddMarker("START", pos);
                Bass.BASS_ChannelSetPosition(_stream, pos);
                if (WF.Wave.marker.ContainsKey("END"))
                {
                    long endpos = WF.Wave.marker["END"];
                    if (endpos < pos)
                    {
                        WF.RemoveMarker("END");
                    }
                }
                DrawWave();
            }
            else if (e.Button == MouseButtons.Right)
            {
                long pos = WF.GetBytePositionFromX(e.X, this.pictureBox1.Width, _zoomStart, _zoomEnd);
                // set End marker
                WF.AddMarker("END", pos);
                Bass.BASS_ChannelRemoveSync(_stream, _syncer);
                _syncer = Bass.BASS_ChannelSetSync(_stream, BASSSync.BASS_SYNC_POS, pos, _sync, IntPtr.Zero);
                if (WF.Wave.marker.ContainsKey("START"))
                {
                    long startpos = WF.Wave.marker["START"];
                    if (startpos > pos)
                    {
                        WF.RemoveMarker("START");
                    }
                }
                DrawWave();
            }
        }

        private void SetPosition(int handle, int channel, int data, IntPtr user)
        {
            if (WF.Wave.marker.ContainsKey("START"))
            {
                long startpos = WF.Wave.marker["START"];
                Bass.BASS_ChannelSetPosition(_stream, (long)startpos);
                if (_zoomed)
                {
                    _zoomStart = WF.Position2Frames((long)startpos) - 1;
                    _zoomStartBytes = WF.Frame2Bytes(_zoomStart);
                    if (WF.Wave.marker.ContainsKey("END"))
                    {
                        long endpos = WF.Wave.marker["END"];
                        _zoomEnd = WF.Position2Frames((long)endpos) + 10;
                        _zoomDistance = WF.Frame2Bytes(_zoomEnd) - WF.Frame2Bytes(_zoomStart);
                    }
                    DrawWave();
                }
            }
            else
                Bass.BASS_ChannelSetPosition(_stream, 0);

        }

		#endregion

		#region PeakLevelMeter

		private void _plm1_Notification(object sender, EventArgs e)
		{
			try
			{
				// sender will be the DSP_PeakLevelMeter instance
				// you could also access it via: DSP_PeakLevelMeter plm = (DSP_PeakLevelMeter)sender;
				this.progressBarPeak1Left.Value = _plm1.LevelL;
				this.progressBarPeak1Right.Value = _plm1.LevelR;
				this.labelLevel1.Text = String.Format( "RMS: {0:#00.0} dB    AVG: {1:#00.0} dB    Peak: {2:#00.0} dB", _plm1.RMS_dBV, _plm1.AVG_dBV, Math.Max(_plm1.PeakHoldLevelL_dBV, _plm1.PeakHoldLevelR_dBV) );
			}
			catch {}
		}

		private void _plm2_Notification(object sender, EventArgs e)
		{
			try
			{
				// sender will be the DSP_PeakLevelMeter instance
				// you could also access it via: DSP_PeakLevelMeter plm = (DSP_PeakLevelMeter)sender;
				this.progressBarPeak2Left.Value = _plm2.LevelL;
				this.progressBarPeak2Right.Value = _plm2.LevelR;
				this.labelLevel2.Text = String.Format( "RMS: {0:#00.0} dB    AVG: {1:#00.0} dB    Peak: {2:#00.0} dB", _plm2.RMS_dBV, _plm2.AVG_dBV, Math.Max(_plm2.PeakHoldLevelL_dBV, _plm2.PeakHoldLevelR_dBV) );
				// display the effect
				int effect = Math.Max(_plm2.LevelL, _plm2.LevelR) - Math.Max(_plm1.LevelL, _plm1.LevelR);
				this.trackBarEffect.Value = effect;
			}
			catch {}
		}

		private void checkBoxLevel1Bypass_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_plm1 != null)
				_plm1.SetBypass(!checkBoxLevel1Bypass.Checked);
		}

		private void checkBoxLevel2Bypass_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_plm2 != null)
				_plm2.SetBypass(!checkBoxLevel2Bypass.Checked);
		}

		#endregion PeakLevelMeter

		#region DSP_Gain

		private void buttonSetGain_Click(object sender, System.EventArgs e)
		{
			if (_gain != null)
			{
				try
				{
					double gainDB = double.Parse(this.textBoxGainDBValue.Text);
					if (gainDB == 0.0)
						_gain.SetBypass(true);
					else
					{
						_gain.SetBypass(false);
						_gain.Gain_dBV = gainDB;
					}
					trackBarGain.Value = (int)(gainDB*1000d);
				}
				catch { }
			}
		}

		private void trackBarGain_ValueChanged(object sender, System.EventArgs e)
		{
			if (_gain != null)
				this.textBoxGainDBValue.Text = Convert.ToString(trackBarGain.Value/1000d);
			buttonSetGain_Click(this, EventArgs.Empty);
		}

		private void checkBoxGainDither_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_gain != null)
			{
				_gain.UseDithering = checkBoxGainDither.Checked;
			}
			if (_stereoEnh != null)
			{
				_stereoEnh.UseDithering = checkBoxGainDither.Checked;
			}
		}

		#endregion DSP_Gain

		#region DynAmp

        private int _dampFX = 0;
		private void checkBoxDAmp_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_stream == 0)
				return;

			if (checkBoxDAmp.Checked)
			{
                _dampFX = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_BFX_DAMP, _dampPrio);
                Bass.BASS_FXSetParameters(_dampFX, _damp);

			}
			else
			{
                Bass.BASS_ChannelRemoveFX(_stream, _dampFX);
                _dampFX = 0;
			}
		}

		#endregion DynAmp

		#region Compressor

        private int _compFX = 0;
		private void checkBoxCompressor_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_stream == 0)
				return;

			if (checkBoxCompressor.Checked)
			{
                _compFX = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_COMPRESSOR, _compPrio);
                Bass.BASS_FXSetParameters(_compFX, _comp);

            }
            else
            {
                Bass.BASS_ChannelRemoveFX(_stream, _compFX);
                _compFX = 0;
            }
		}

		private void trackBarCompressor_ValueChanged(object sender, System.EventArgs e)
		{
			if (_stream == 0)
				return;

			_comp.fThreshold = (float)Utils.DBToLevel( trackBarCompressor.Value/10d, 1.0 );
            Bass.BASS_FXSetParameters(_compFX, _comp);

			labelCompThreshold.Text = String.Format( "Threshold: {0:#0.0} dB", trackBarCompressor.Value/10d);
		}

		#endregion Compressor

		#region StereoEnhancer

		private void checkBoxStereoEnhancer_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_stereoEnh != null)
				_stereoEnh.SetBypass(!checkBoxStereoEnhancer.Checked);
		}

		private void trackBarStereoEnhancerWide_ValueChanged(object sender, System.EventArgs e)
		{
			if (_stereoEnh != null)
				_stereoEnh.WideCoeff = trackBarStereoEnhancerWide.Value/100d;
			labelStereoEnhancer.Text = String.Format( "Wide: {0:#0.00}, {1:#0.00}", trackBarStereoEnhancerWide.Value/100d, trackBarStereoEnhancerWetDry.Value/100d);
		}

		private void trackBarStereoEnhancerWetDry_ValueChanged(object sender, System.EventArgs e)
		{
			if (_stereoEnh != null)
				_stereoEnh.WetDry = trackBarStereoEnhancerWetDry.Value/100d;
			labelStereoEnhancer.Text = String.Format( "Wide: {0:#0.00} / {1:#0.00}", trackBarStereoEnhancerWide.Value/100d, trackBarStereoEnhancerWetDry.Value/100d);
		}

		#endregion StereoEnhancer

		#region Mono

		private void checkBoxMono_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_stream == 0)		
				return;

			if (_mono.IsAssigned)
			{
				_mono.Stop();
			}
			else
			{
				_mono.ChannelHandle = _stream;
				_mono.DSPPriority = 0;
				_mono.UseDithering = true;
				_mono.Invert = checkBoxMonoInvert.Checked;
				_mono.Start();
			}
		}

		private void checkBoxMonoInvert_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_mono != null)
				_mono.Invert = checkBoxMonoInvert.Checked;
		}

		#endregion Mono

		#region IIR Delay

		private void checkBoxIIRDelay_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_delay != null)
				_delay.SetBypass(!checkBoxIIRDelay.Checked);
		}

		private void trackBarIIRDelay_ValueChanged(object sender, System.EventArgs e)
		{
			if (_delay != null)
				_delay.Delay = trackBarIIRDelay.Value;
			labelIIRDelay.Text = String.Format( "Delay: {0} samples", trackBarIIRDelay.Value);
		}

		private void trackBarIIRDelayWetDry_ValueChanged(object sender, System.EventArgs e)
		{
			if (_delay != null)
				_delay.WetDry = trackBarIIRDelayWet.Value/100d;
		}

		private void trackBarIIRDelayFeedback_ValueChanged(object sender, System.EventArgs e)
		{
			if (_delay != null)
				_delay.Feedback = trackBarIIRDelayFeedback.Value/100d;
		}

		#endregion IIR Delay

		#region Soft Saturation

		private void checkBoxSoftSat_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_softSat != null)
				_softSat.SetBypass(!checkBoxSoftSat.Checked);
		}

		private void trackBarSoftSat_ValueChanged(object sender, System.EventArgs e)
		{
			if (_softSat != null)
				_softSat.Factor = trackBarSoftSat.Value/100d;
		}

		private void trackBarSoftSatDepth_ValueChanged(object sender, System.EventArgs e)
		{
			if (_softSat != null)
				_softSat.Depth = trackBarSoftSatDepth.Value/100d;
		}

		#endregion Soft Saturation

		#region Stream Copy

		private BASS_INFO _info = Bass.BASS_GetInfo();
		private void comboBoxStreamCopy_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (_streamCopy != null)
			{
				_streamCopy.Stop();
				_streamCopy = null;
				return;
			}

			int dev = comboBoxStreamCopy.SelectedIndex;
			if (_stream != 0)
			{
				if (!Bass.BASS_Init(dev, 44100, BASSInit.BASS_DEVICE_LATENCY, this.Handle))
					Bass.BASS_SetDevice(dev); // already?!
				_info = Bass.BASS_GetInfo();

				// add the stream copy option
				_streamCopy = new DSP_StreamCopy();
				_streamCopy.OutputLatency = _info.latency;
				_streamCopy.ChannelHandle = _stream;
				_streamCopy.DSPPriority = checkBoxStreamCopy.Checked ? -4000 : 4000;
				_streamCopy.StreamCopyDevice = dev;
				//_streamCopy.StreamCopyFlags = BASSFlag.BASS_SPEAKER_REAR;
				_streamCopy.Start();
			}
		}

		private void checkBoxStreamCopy_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_streamCopy != null)
			{
				_streamCopy.DSPPriority = checkBoxStreamCopy.Checked ? -4000 : 4000;
			}
		}

		#endregion Stream Copy

	}
}
