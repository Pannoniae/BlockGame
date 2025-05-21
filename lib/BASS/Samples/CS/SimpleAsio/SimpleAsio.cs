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
using Un4seen.BassAsio;


namespace Sample
{
	// NOTE: Needs 'bassasio.dll' - copy it to your output directory first!
	//  also needs 'bass.dll' - copy it to your output directory first!


	public class SimpleAsio : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonOpenFile;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button buttonStop;

		// LOCAL VARS
		private int _stream = 0;
        private string _fileName = String.Empty;
        private GroupBox groupBox2;
        private ComboBox comboBoxAsioOutputDevice;
        private GroupBox groupBox1;
        private ComboBox comboBoxAsioInputDevice;
        private Label label1;
        private ComboBox comboBoxBassDevice;
        private Button buttonStartRecording;
        private RadioButton radioButtonAsioFullDuplex;
        private RadioButton radioButtonNoFullDuplex;
        private RadioButton radioButtonBassFullDuplex;
        private ProgressBar progressBarRecR;
        private ProgressBar progressBarRecL;
        private CheckBox checkBoxSaveToWave;
        private ProgressBar progressBarPlayR;
        private ProgressBar progressBarPlayL;
        private ComboBox comboBoxAsioOutputChannel;
        private Button buttonStopRecording;
        private Label label2;
        private NumericUpDown numericUpDownAsioBuffer;
        private ComboBox comboBoxAsioInputChannel;

		public SimpleAsio()
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
            this.buttonOpenFile = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.buttonStop = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.comboBoxAsioOutputChannel = new System.Windows.Forms.ComboBox();
            this.comboBoxAsioOutputDevice = new System.Windows.Forms.ComboBox();
            this.progressBarPlayR = new System.Windows.Forms.ProgressBar();
            this.progressBarPlayL = new System.Windows.Forms.ProgressBar();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBoxAsioInputChannel = new System.Windows.Forms.ComboBox();
            this.checkBoxSaveToWave = new System.Windows.Forms.CheckBox();
            this.progressBarRecR = new System.Windows.Forms.ProgressBar();
            this.progressBarRecL = new System.Windows.Forms.ProgressBar();
            this.radioButtonBassFullDuplex = new System.Windows.Forms.RadioButton();
            this.radioButtonAsioFullDuplex = new System.Windows.Forms.RadioButton();
            this.radioButtonNoFullDuplex = new System.Windows.Forms.RadioButton();
            this.comboBoxBassDevice = new System.Windows.Forms.ComboBox();
            this.comboBoxAsioInputDevice = new System.Windows.Forms.ComboBox();
            this.buttonStopRecording = new System.Windows.Forms.Button();
            this.buttonStartRecording = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownAsioBuffer = new System.Windows.Forms.NumericUpDown();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAsioBuffer)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOpenFile
            // 
            this.buttonOpenFile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOpenFile.Location = new System.Drawing.Point(6, 46);
            this.buttonOpenFile.Name = "buttonOpenFile";
            this.buttonOpenFile.Size = new System.Drawing.Size(221, 23);
            this.buttonOpenFile.TabIndex = 2;
            this.buttonOpenFile.Text = "Select a file to play...";
            this.buttonOpenFile.Click += new System.EventHandler(this.buttonOpenFile_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav";
            this.openFileDialog.Title = "Select an audio file to play";
            // 
            // buttonStop
            // 
            this.buttonStop.Enabled = false;
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonStop.Location = new System.Drawing.Point(6, 105);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(221, 23);
            this.buttonStop.TabIndex = 6;
            this.buttonStop.Text = "STOP";
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.comboBoxAsioOutputChannel);
            this.groupBox2.Controls.Add(this.comboBoxAsioOutputDevice);
            this.groupBox2.Controls.Add(this.progressBarPlayR);
            this.groupBox2.Controls.Add(this.progressBarPlayL);
            this.groupBox2.Controls.Add(this.buttonOpenFile);
            this.groupBox2.Controls.Add(this.buttonStop);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(233, 208);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Asio Playback";
            // 
            // comboBoxAsioOutputChannel
            // 
            this.comboBoxAsioOutputChannel.FormattingEnabled = true;
            this.comboBoxAsioOutputChannel.Location = new System.Drawing.Point(117, 19);
            this.comboBoxAsioOutputChannel.Name = "comboBoxAsioOutputChannel";
            this.comboBoxAsioOutputChannel.Size = new System.Drawing.Size(110, 21);
            this.comboBoxAsioOutputChannel.TabIndex = 1;
            // 
            // comboBoxAsioOutputDevice
            // 
            this.comboBoxAsioOutputDevice.FormattingEnabled = true;
            this.comboBoxAsioOutputDevice.Location = new System.Drawing.Point(6, 19);
            this.comboBoxAsioOutputDevice.Name = "comboBoxAsioOutputDevice";
            this.comboBoxAsioOutputDevice.Size = new System.Drawing.Size(105, 21);
            this.comboBoxAsioOutputDevice.TabIndex = 0;
            this.comboBoxAsioOutputDevice.SelectedIndexChanged += new System.EventHandler(this.comboBoxAsioOutputDevice_SelectedIndexChanged);
            // 
            // progressBarPlayR
            // 
            this.progressBarPlayR.Location = new System.Drawing.Point(6, 87);
            this.progressBarPlayR.Maximum = 32768;
            this.progressBarPlayR.Name = "progressBarPlayR";
            this.progressBarPlayR.Size = new System.Drawing.Size(221, 12);
            this.progressBarPlayR.Step = 1;
            this.progressBarPlayR.TabIndex = 4;
            // 
            // progressBarPlayL
            // 
            this.progressBarPlayL.Location = new System.Drawing.Point(6, 75);
            this.progressBarPlayL.Maximum = 32768;
            this.progressBarPlayL.Name = "progressBarPlayL";
            this.progressBarPlayL.Size = new System.Drawing.Size(221, 12);
            this.progressBarPlayL.Step = 1;
            this.progressBarPlayL.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBoxAsioInputChannel);
            this.groupBox1.Controls.Add(this.checkBoxSaveToWave);
            this.groupBox1.Controls.Add(this.progressBarRecR);
            this.groupBox1.Controls.Add(this.progressBarRecL);
            this.groupBox1.Controls.Add(this.radioButtonBassFullDuplex);
            this.groupBox1.Controls.Add(this.radioButtonAsioFullDuplex);
            this.groupBox1.Controls.Add(this.radioButtonNoFullDuplex);
            this.groupBox1.Controls.Add(this.comboBoxBassDevice);
            this.groupBox1.Controls.Add(this.comboBoxAsioInputDevice);
            this.groupBox1.Controls.Add(this.buttonStopRecording);
            this.groupBox1.Controls.Add(this.buttonStartRecording);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(251, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(233, 249);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Asio Recording";
            // 
            // comboBoxAsioInputChannel
            // 
            this.comboBoxAsioInputChannel.FormattingEnabled = true;
            this.comboBoxAsioInputChannel.Location = new System.Drawing.Point(117, 20);
            this.comboBoxAsioInputChannel.Name = "comboBoxAsioInputChannel";
            this.comboBoxAsioInputChannel.Size = new System.Drawing.Size(110, 21);
            this.comboBoxAsioInputChannel.TabIndex = 1;
            // 
            // checkBoxSaveToWave
            // 
            this.checkBoxSaveToWave.AutoSize = true;
            this.checkBoxSaveToWave.Enabled = false;
            this.checkBoxSaveToWave.Location = new System.Drawing.Point(7, 223);
            this.checkBoxSaveToWave.Name = "checkBoxSaveToWave";
            this.checkBoxSaveToWave.Size = new System.Drawing.Size(210, 17);
            this.checkBoxSaveToWave.TabIndex = 11;
            this.checkBoxSaveToWave.Text = "Save Recording (Encode to Wave File)";
            this.checkBoxSaveToWave.UseVisualStyleBackColor = true;
            this.checkBoxSaveToWave.CheckedChanged += new System.EventHandler(this.checkBoxSaveToWave_CheckedChanged);
            // 
            // progressBarRecR
            // 
            this.progressBarRecR.Location = new System.Drawing.Point(6, 87);
            this.progressBarRecR.Maximum = 32768;
            this.progressBarRecR.Name = "progressBarRecR";
            this.progressBarRecR.Size = new System.Drawing.Size(221, 12);
            this.progressBarRecR.Step = 1;
            this.progressBarRecR.TabIndex = 5;
            // 
            // progressBarRecL
            // 
            this.progressBarRecL.Location = new System.Drawing.Point(6, 75);
            this.progressBarRecL.Maximum = 32768;
            this.progressBarRecL.Name = "progressBarRecL";
            this.progressBarRecL.Size = new System.Drawing.Size(221, 12);
            this.progressBarRecL.Step = 1;
            this.progressBarRecL.TabIndex = 4;
            // 
            // radioButtonBassFullDuplex
            // 
            this.radioButtonBassFullDuplex.AutoSize = true;
            this.radioButtonBassFullDuplex.Enabled = false;
            this.radioButtonBassFullDuplex.Location = new System.Drawing.Point(6, 151);
            this.radioButtonBassFullDuplex.Name = "radioButtonBassFullDuplex";
            this.radioButtonBassFullDuplex.Size = new System.Drawing.Size(222, 17);
            this.radioButtonBassFullDuplex.TabIndex = 8;
            this.radioButtonBassFullDuplex.Text = "Full-Duplex (using BASS Playback Device)";
            this.radioButtonBassFullDuplex.UseVisualStyleBackColor = true;
            this.radioButtonBassFullDuplex.CheckedChanged += new System.EventHandler(this.radioButtonBassFullDuplex_CheckedChanged);
            // 
            // radioButtonAsioFullDuplex
            // 
            this.radioButtonAsioFullDuplex.AutoSize = true;
            this.radioButtonAsioFullDuplex.Enabled = false;
            this.radioButtonAsioFullDuplex.Location = new System.Drawing.Point(6, 128);
            this.radioButtonAsioFullDuplex.Name = "radioButtonAsioFullDuplex";
            this.radioButtonAsioFullDuplex.Size = new System.Drawing.Size(217, 17);
            this.radioButtonAsioFullDuplex.TabIndex = 7;
            this.radioButtonAsioFullDuplex.Text = "Full-Duplex (using Asio Playback Device)";
            this.radioButtonAsioFullDuplex.UseVisualStyleBackColor = true;
            this.radioButtonAsioFullDuplex.CheckedChanged += new System.EventHandler(this.radioButtonAsioFullDuplex_CheckedChanged);
            // 
            // radioButtonNoFullDuplex
            // 
            this.radioButtonNoFullDuplex.AutoSize = true;
            this.radioButtonNoFullDuplex.Checked = true;
            this.radioButtonNoFullDuplex.Enabled = false;
            this.radioButtonNoFullDuplex.Location = new System.Drawing.Point(7, 105);
            this.radioButtonNoFullDuplex.Name = "radioButtonNoFullDuplex";
            this.radioButtonNoFullDuplex.Size = new System.Drawing.Size(94, 17);
            this.radioButtonNoFullDuplex.TabIndex = 6;
            this.radioButtonNoFullDuplex.TabStop = true;
            this.radioButtonNoFullDuplex.Text = "No Full-Duplex";
            this.radioButtonNoFullDuplex.UseVisualStyleBackColor = true;
            this.radioButtonNoFullDuplex.CheckedChanged += new System.EventHandler(this.radioButtonNoFullDuplex_CheckedChanged);
            // 
            // comboBoxBassDevice
            // 
            this.comboBoxBassDevice.Enabled = false;
            this.comboBoxBassDevice.FormattingEnabled = true;
            this.comboBoxBassDevice.Location = new System.Drawing.Point(25, 187);
            this.comboBoxBassDevice.Name = "comboBoxBassDevice";
            this.comboBoxBassDevice.Size = new System.Drawing.Size(202, 21);
            this.comboBoxBassDevice.TabIndex = 10;
            this.comboBoxBassDevice.SelectedIndexChanged += new System.EventHandler(this.comboBoxBassDevice_SelectedIndexChanged);
            // 
            // comboBoxAsioInputDevice
            // 
            this.comboBoxAsioInputDevice.FormattingEnabled = true;
            this.comboBoxAsioInputDevice.Location = new System.Drawing.Point(6, 19);
            this.comboBoxAsioInputDevice.Name = "comboBoxAsioInputDevice";
            this.comboBoxAsioInputDevice.Size = new System.Drawing.Size(105, 21);
            this.comboBoxAsioInputDevice.TabIndex = 0;
            this.comboBoxAsioInputDevice.SelectedIndexChanged += new System.EventHandler(this.comboBoxAsioInputDevice_SelectedIndexChanged);
            // 
            // buttonStopRecording
            // 
            this.buttonStopRecording.Enabled = false;
            this.buttonStopRecording.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonStopRecording.Location = new System.Drawing.Point(117, 46);
            this.buttonStopRecording.Name = "buttonStopRecording";
            this.buttonStopRecording.Size = new System.Drawing.Size(110, 23);
            this.buttonStopRecording.TabIndex = 3;
            this.buttonStopRecording.Text = "Stop Recording";
            this.buttonStopRecording.Click += new System.EventHandler(this.buttonStopRecording_Click);
            // 
            // buttonStartRecording
            // 
            this.buttonStartRecording.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonStartRecording.Location = new System.Drawing.Point(6, 46);
            this.buttonStartRecording.Name = "buttonStartRecording";
            this.buttonStartRecording.Size = new System.Drawing.Size(105, 23);
            this.buttonStartRecording.TabIndex = 2;
            this.buttonStartRecording.Text = "Start Recording";
            this.buttonStartRecording.Click += new System.EventHandler(this.buttonStartRecording_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 171);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "BASS Output Device:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 235);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "ASIO Buffer:";
            // 
            // numericUpDownAsioBuffer
            // 
            this.numericUpDownAsioBuffer.Increment = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.numericUpDownAsioBuffer.Location = new System.Drawing.Point(93, 233);
            this.numericUpDownAsioBuffer.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
            this.numericUpDownAsioBuffer.Name = "numericUpDownAsioBuffer";
            this.numericUpDownAsioBuffer.Size = new System.Drawing.Size(72, 21);
            this.numericUpDownAsioBuffer.TabIndex = 3;
            // 
            // SimpleAsio
            // 
            this.ClientSize = new System.Drawing.Size(494, 272);
            this.Controls.Add(this.numericUpDownAsioBuffer);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "SimpleAsio";
            this.Text = "Bass ASIO Handler";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.SimpleAsio_Closing);
            this.Load += new System.EventHandler(this.SimpleAsio_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAsioBuffer)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new SimpleAsio());
		}

        private void SimpleAsio_Load(object sender, System.EventArgs e)
        {
	        //BassNet.Registration("your email", "your regkey");

            // get the Asio devices and init them
            this.comboBoxAsioOutputDevice.Items.AddRange(BassAsio.BASS_ASIO_GetDeviceInfos());
            for (int i = 0; i < this.comboBoxAsioOutputDevice.Items.Count; i++)
            {
                if (!BassAsio.BASS_ASIO_Init(i, BASSASIOInit.BASS_ASIO_DEFAULT))
                    this.comboBoxAsioOutputDevice.Items[i] = BassAsio.BASS_ASIO_ErrorGetCode();
            }
            this.comboBoxAsioOutputDevice.SelectedIndex = 0;
            this.comboBoxAsioInputDevice.Items.AddRange(BassAsio.BASS_ASIO_GetDeviceInfos());
            this.comboBoxAsioInputDevice.SelectedIndex = this.comboBoxAsioOutputDevice.SelectedIndex;

            // get the bass devices, init them and set the default one
            this.comboBoxBassDevice.Items.AddRange(Bass.BASS_GetDeviceInfos());
            int n = 0;
            foreach (BASS_DEVICEINFO info in this.comboBoxBassDevice.Items)
            {
                Bass.BASS_Init(n, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle);
                if (info.IsDefault)
                {
                    this.comboBoxBassDevice.SelectedItem = info;
                }
                n++;
            }
        }

		private void SimpleAsio_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            if (_asioOut != null)
                _asioOut.Dispose();
            if (_asioIn != null)
                _asioIn.Dispose();

			// free all ASIO devices
            for (int i = 0; i < this.comboBoxAsioOutputDevice.Items.Count; i++)
            {
                BassAsio.BASS_ASIO_SetDevice(i);
                BassAsio.BASS_ASIO_Free();
            }
			
			// free all BASS devices
            int n = 0;
            foreach (BASS_DEVICEINFO info in this.comboBoxBassDevice.Items)
            {
                Bass.BASS_SetDevice(n);
                Bass.BASS_Free();
                n++;
            }
        }

        #region Asio Playback

        private BassAsioHandler _asioOut;
        private DSP_PeakLevelMeter _plmPlay;

        private void comboBoxAsioOutputDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            // get the available Asio channels
            this.comboBoxAsioOutputChannel.Items.Clear();
            this.comboBoxAsioOutputChannel.Text = "";
            int n = this.comboBoxAsioOutputDevice.SelectedIndex;
            if (BassAsio.BASS_ASIO_GetDevice() == n || BassAsio.BASS_ASIO_SetDevice(n))
            {
                BASS_ASIO_INFO info = BassAsio.BASS_ASIO_GetInfo();
                if (info != null)
                {
                    // assuming stereo output
                    for (int i = 0; i < info.outputs; i += 2)
                    {
                        BASS_ASIO_CHANNELINFO chanInfo = BassAsio.BASS_ASIO_ChannelGetInfo(false, i);
                        if (chanInfo != null)
                            this.comboBoxAsioOutputChannel.Items.Add(chanInfo);
                    }
                    if (this.comboBoxAsioOutputChannel.Items.Count > 0)
                        this.comboBoxAsioOutputChannel.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show(String.Format("AsioError = {0}", BassAsio.BASS_ASIO_ErrorGetCode()));
                }
            }
            else
            {
                MessageBox.Show(String.Format("AsioError = {0}", BassAsio.BASS_ASIO_ErrorGetCode()));
            }
        }

        private void buttonOpenFile_Click(object sender, System.EventArgs e)
        {
            this.openFileDialog.FileName = _fileName;
            if (DialogResult.OK == this.openFileDialog.ShowDialog(this))
            {
                if (File.Exists(this.openFileDialog.FileName))
                {
                    _fileName = this.openFileDialog.FileName;
                    // create the decoding stream
                    _stream = Bass.BASS_StreamCreateFile(_fileName, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
                    if (_stream != 0)
                    {
                        // now setup the ASIO handler
                        _asioOut = new BassAsioHandler(comboBoxAsioOutputDevice.SelectedIndex, comboBoxAsioOutputChannel.SelectedIndex * 2, _stream);
                        if (_asioOut.Start((int)this.numericUpDownAsioBuffer.Value, 1))
                        {
                            // use a DSP to measure the Level
                            _plmPlay = new DSP_PeakLevelMeter(_asioOut.OutputChannel, 0);
                            _plmPlay.Notification += new EventHandler(_plm_Play_Notification);

                            this.comboBoxAsioOutputDevice.Enabled = false;
                            this.comboBoxAsioOutputChannel.Enabled = false;
                            this.buttonOpenFile.Enabled = false;
                            this.buttonStop.Enabled = true;
                        }
                        else
                        {
                            _asioOut.Dispose();
                            _asioOut = null;
                            MessageBox.Show("Asio Device could not be started!");
                        }
                    }
                    else
                    {
                        MessageBox.Show(String.Format("Error = {0}", Bass.BASS_ErrorGetCode()));
                    }
                }
                else
                    _fileName = String.Empty;
            }
        }

        private void _plm_Play_Notification(object sender, EventArgs e)
        {
            if (_plmPlay != null)
            {
                this.progressBarPlayL.Value = _plmPlay.LevelL;
                this.progressBarPlayR.Value = _plmPlay.LevelR;
            }
        }

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
            if (_asioOut != null)
            {
                // stop the DSP
                _plmPlay.Stop();
                _plmPlay = null;
                // stop the device, since we don't need it anymore
                _asioOut.Stop();
                // and dispose (unjoin and disable any channel)
                _asioOut.Dispose();
                _asioOut = null;
                // and free the source channel
                Bass.BASS_StreamFree(_stream);
                _stream = 0;

                this.comboBoxAsioOutputDevice.Enabled = true;
                this.comboBoxAsioOutputChannel.Enabled = true;
                this.buttonOpenFile.Enabled = true;
                this.buttonStop.Enabled = false;
                this.progressBarPlayL.Value = 0;
                this.progressBarPlayR.Value = 0;
            }
        }

        #endregion Asio Playback

        #region Asio Recording

        private BassAsioHandler _asioIn;
        private DSP_PeakLevelMeter _plmRec;
        private EncoderWAV _wavEncoder;

        private void comboBoxAsioInputDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            // get the available Asio channels
            this.comboBoxAsioInputChannel.Items.Clear();
            this.comboBoxAsioInputChannel.Text = "";
            int n = this.comboBoxAsioInputDevice.SelectedIndex;
            if (BassAsio.BASS_ASIO_GetDevice() == n || BassAsio.BASS_ASIO_SetDevice(n))
            {
                BASS_ASIO_INFO info = BassAsio.BASS_ASIO_GetInfo();
                if (info != null)
                {
                    // assuming stereo input
                    for (int i = 0; i < info.inputs; i += 2)
                    {
                        BASS_ASIO_CHANNELINFO chanInfo = BassAsio.BASS_ASIO_ChannelGetInfo(true, i);
                        if (chanInfo != null)
                            this.comboBoxAsioInputChannel.Items.Add(chanInfo);
                    }
                    if (this.comboBoxAsioInputChannel.Items.Count > 0)
                        this.comboBoxAsioInputChannel.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show(String.Format("AsioError = {0}", BassAsio.BASS_ASIO_ErrorGetCode()));
                }
            }
            else
            {
                MessageBox.Show(String.Format("AsioError = {0}", BassAsio.BASS_ASIO_ErrorGetCode()));
            }
        }

        private void buttonStartRecording_Click(object sender, EventArgs e)
        {
            // now setup the ASIO handler (recording 48kHz, stereo)
            _asioIn = new BassAsioHandler(true, comboBoxAsioInputDevice.SelectedIndex, comboBoxAsioInputChannel.SelectedIndex * 2, 2, BASSASIOFormat.BASS_ASIO_FORMAT_FLOAT, 48000d);
            if (_asioIn.Start((int)this.numericUpDownAsioBuffer.Value, 1))
            {
                // use a DSP to measure the Level of the input
                _plmRec = new DSP_PeakLevelMeter(_asioIn.InputChannel, 0);
                _plmRec.Notification += new EventHandler(_plm_Rec_Notification);

                this.comboBoxAsioInputDevice.Enabled = false;
                this.comboBoxAsioInputChannel.Enabled = false;
                this.buttonStopRecording.Enabled = true;
                this.buttonStartRecording.Enabled = false;
                this.radioButtonNoFullDuplex.Enabled = true;
                this.radioButtonAsioFullDuplex.Enabled = true;
                this.radioButtonBassFullDuplex.Enabled = true;
                this.comboBoxBassDevice.Enabled = true;
                this.checkBoxSaveToWave.Enabled = true;
            }
            else
            {
                _asioIn.Dispose();
                _asioIn = null;
                MessageBox.Show("Asio Device could not be started!");
            }
        }

        private void _plm_Rec_Notification(object sender, EventArgs e)
        {
            if (_plmRec != null)
            {
                this.progressBarRecL.Value = _plmRec.LevelL;
                this.progressBarRecR.Value = _plmRec.LevelR;
            }
        }

        private void buttonStopRecording_Click(object sender, EventArgs e)
        {
            if (_asioIn != null)
            {
                if (_plmRec != null)
                {
                    _plmRec.Stop();
                    _plmRec = null;
                }
                // stop the device, since we don't need it anymore
                _asioIn.Stop();
                // and dispose (unjoin and disable any channel)
                _asioIn.Dispose();
                _asioIn = null;

                this.comboBoxAsioInputDevice.Enabled = true;
                this.comboBoxAsioInputChannel.Enabled = true;
                this.buttonStartRecording.Enabled = true;
                this.buttonStopRecording.Enabled = false;
                this.radioButtonNoFullDuplex.Checked = true;
                this.radioButtonAsioFullDuplex.Checked = false;
                this.radioButtonBassFullDuplex.Checked = false;
                this.radioButtonNoFullDuplex.Enabled = false;
                this.radioButtonAsioFullDuplex.Enabled = false;
                this.radioButtonBassFullDuplex.Enabled = false;
                this.comboBoxBassDevice.Enabled = false;
                this.checkBoxSaveToWave.Enabled = false;
                this.progressBarRecL.Value = 0;
                this.progressBarRecR.Value = 0;
            }
        }

        private void radioButtonNoFullDuplex_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoFullDuplex.Checked && _asioIn != null)
            {
                // remove the full-duplex output DSP
                if (_plmPlay != null)
                {
                    _plmPlay.Stop();
                    _plmPlay = null;
                    this.progressBarPlayL.Value = 0;
                    this.progressBarPlayR.Value = 0;
                }
                // remove the full-duplex option and disable/unjoin any output channels
                _asioIn.RemoveFullDuplex(true);
            }
        }

        private void radioButtonAsioFullDuplex_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonAsioFullDuplex.Checked && _asioIn != null)
            {
                // remove the previous one?
                if (_asioIn.IsInputFullDuplex)
                    _asioIn.RemoveFullDuplex(true);
                // set a new one
                _asioIn.SetFullDuplex(comboBoxAsioOutputDevice.SelectedIndex, comboBoxAsioOutputChannel.SelectedIndex * 2);
                if (_asioIn.StartFullDuplex(0))
                {
                    // set up a DSP on the full-duplex asio output
                    _plmPlay = new DSP_PeakLevelMeter(_asioIn.OutputChannel, 0);
                    _plmPlay.Notification += new EventHandler(_plm_Play_Notification);
                }
            }
        }

        private void radioButtonBassFullDuplex_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonBassFullDuplex.Checked && _asioIn != null)
            {
                // remove the previous one?
                if (_asioIn.IsInputFullDuplex)
                    _asioIn.RemoveFullDuplex(true);
                // set a new one
                if (_asioIn.SetFullDuplex(comboBoxBassDevice.SelectedIndex, BASSFlag.BASS_SAMPLE_SOFTWARE, true))
                {
                    // set up a DSP on the full-duplex bass output
                    _plmPlay = new DSP_PeakLevelMeter(_asioIn.OutputChannel, 0);
                    _plmPlay.Notification += new EventHandler(_plm_Play_Notification);
                }
            }
        }

        private void checkBoxSaveToWave_CheckedChanged(object sender, EventArgs e)
        {
            if (_asioIn != null)
            {
                if (_wavEncoder != null)
                {
                    // stop the encoder
                    _wavEncoder.Stop();
                    _wavEncoder = null;
                }

                if (checkBoxSaveToWave.Checked)
                {
                    // setup an encoder on the asio input channel
                    // Note: this will write a 32-bit, 48kHz, stereo Wave file
                    _wavEncoder = new EncoderWAV(_asioIn.InputChannel);
                    _wavEncoder.InputFile = null; // use STDIN (the above channel)
                    _wavEncoder.OutputFile = Path.Combine(Application.StartupPath, "output.wav");
                    _wavEncoder.Start(null, IntPtr.Zero, false);
                }
            }
        }

        private void comboBoxBassDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (radioButtonBassFullDuplex.Checked && _asioIn != null)
            {
                // remove the previous one?
                if (_asioIn.IsInputFullDuplex)
                    _asioIn.RemoveFullDuplex(false);
                // set a new one
                _asioIn.SetFullDuplex(comboBoxBassDevice.SelectedIndex, BASSFlag.BASS_DEFAULT, true);
            }
        }

        #endregion Asio Recording

    }


}
