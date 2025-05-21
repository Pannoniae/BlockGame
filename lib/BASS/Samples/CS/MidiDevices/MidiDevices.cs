using System;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using System.Runtime.InteropServices;
using radio42.Multimedia.Midi;

namespace Sample
{
	public class MidiDevices : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox textBoxMsg;
		private System.Windows.Forms.Button buttonStart;
		private System.Windows.Forms.Button buttonStop;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox checkBoxChannel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox checkBoxSystemCommon;
		private System.Windows.Forms.CheckBox checkBoxSystemRealtime;
		private System.Windows.Forms.CheckBox checkBoxSystemExclusive;
		private System.Windows.Forms.CheckBox checkBoxAutoPair;
		private System.Windows.Forms.CheckBox checkBoxHex;
		private System.ComponentModel.Container components = null;

		public MidiDevices()
		{
			InitializeComponent();

			int[] inPorts = MidiInputDevice.GetMidiPorts();
			foreach (int port in inPorts)
			{
				string name = MidiInputDevice.GetDeviceDescription(port);
				Console.WriteLine( "Input : {0}={1}", port, name );
				this.comboBox1.Items.Add( name );
			}

			int[] outPorts = MidiOutputDevice.GetMidiPorts();
			foreach (int port in outPorts)
			{
				string name = MidiOutputDevice.GetDeviceDescription(port);
				Console.WriteLine( "Output : {0}={1}", port, name );
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				StopDevice();

				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonStart = new System.Windows.Forms.Button();
			this.textBoxMsg = new System.Windows.Forms.TextBox();
			this.buttonStop = new System.Windows.Forms.Button();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkBoxChannel = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.checkBoxSystemCommon = new System.Windows.Forms.CheckBox();
			this.checkBoxSystemRealtime = new System.Windows.Forms.CheckBox();
			this.checkBoxSystemExclusive = new System.Windows.Forms.CheckBox();
			this.checkBoxAutoPair = new System.Windows.Forms.CheckBox();
			this.checkBoxHex = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// buttonStart
			// 
			this.buttonStart.Location = new System.Drawing.Point(200, 8);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.TabIndex = 2;
			this.buttonStart.Text = "Start";
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
			// 
			// textBoxMsg
			// 
			this.textBoxMsg.AcceptsReturn = true;
			this.textBoxMsg.AcceptsTab = true;
			this.textBoxMsg.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.textBoxMsg.Location = new System.Drawing.Point(0, 68);
			this.textBoxMsg.Multiline = true;
			this.textBoxMsg.Name = "textBoxMsg";
			this.textBoxMsg.ReadOnly = true;
			this.textBoxMsg.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxMsg.Size = new System.Drawing.Size(688, 274);
			this.textBoxMsg.TabIndex = 9;
			this.textBoxMsg.Text = "";
			this.textBoxMsg.WordWrap = false;
			// 
			// buttonStop
			// 
			this.buttonStop.Enabled = false;
			this.buttonStop.Location = new System.Drawing.Point(280, 8);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.TabIndex = 3;
			this.buttonStop.Text = "Stop";
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
			// 
			// comboBox1
			// 
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Location = new System.Drawing.Point(64, 8);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(121, 21);
			this.comboBox1.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(48, 21);
			this.label2.TabIndex = 0;
			this.label2.Text = "Midi In:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// checkBoxChannel
			// 
			this.checkBoxChannel.Checked = true;
			this.checkBoxChannel.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxChannel.Location = new System.Drawing.Point(104, 42);
			this.checkBoxChannel.Name = "checkBoxChannel";
			this.checkBoxChannel.Size = new System.Drawing.Size(72, 21);
			this.checkBoxChannel.TabIndex = 5;
			this.checkBoxChannel.Text = "Channel";
			this.checkBoxChannel.CheckedChanged += new System.EventHandler(this.FilterChanged);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 40);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(88, 21);
			this.label1.TabIndex = 4;
			this.label1.Text = "Message Types:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// checkBoxSystemCommon
			// 
			this.checkBoxSystemCommon.Checked = true;
			this.checkBoxSystemCommon.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxSystemCommon.Location = new System.Drawing.Point(180, 42);
			this.checkBoxSystemCommon.Name = "checkBoxSystemCommon";
			this.checkBoxSystemCommon.Size = new System.Drawing.Size(112, 21);
			this.checkBoxSystemCommon.TabIndex = 6;
			this.checkBoxSystemCommon.Text = "System-Common";
			this.checkBoxSystemCommon.CheckedChanged += new System.EventHandler(this.FilterChanged);
			// 
			// checkBoxSystemRealtime
			// 
			this.checkBoxSystemRealtime.Location = new System.Drawing.Point(298, 42);
			this.checkBoxSystemRealtime.Name = "checkBoxSystemRealtime";
			this.checkBoxSystemRealtime.Size = new System.Drawing.Size(112, 21);
			this.checkBoxSystemRealtime.TabIndex = 7;
			this.checkBoxSystemRealtime.Text = "System-Realtime";
			this.checkBoxSystemRealtime.CheckedChanged += new System.EventHandler(this.FilterChanged);
			// 
			// checkBoxSystemExclusive
			// 
			this.checkBoxSystemExclusive.Checked = true;
			this.checkBoxSystemExclusive.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxSystemExclusive.Location = new System.Drawing.Point(420, 42);
			this.checkBoxSystemExclusive.Name = "checkBoxSystemExclusive";
			this.checkBoxSystemExclusive.Size = new System.Drawing.Size(120, 21);
			this.checkBoxSystemExclusive.TabIndex = 8;
			this.checkBoxSystemExclusive.Text = "System-Exclusive";
			this.checkBoxSystemExclusive.CheckedChanged += new System.EventHandler(this.FilterChanged);
			// 
			// checkBoxAutoPair
			// 
			this.checkBoxAutoPair.Location = new System.Drawing.Point(418, 10);
			this.checkBoxAutoPair.Name = "checkBoxAutoPair";
			this.checkBoxAutoPair.Size = new System.Drawing.Size(74, 24);
			this.checkBoxAutoPair.TabIndex = 10;
			this.checkBoxAutoPair.Text = "Auto-Pair";
			this.checkBoxAutoPair.CheckedChanged += new System.EventHandler(this.OnAutoPair);
			// 
			// checkBoxHex
			// 
			this.checkBoxHex.Location = new System.Drawing.Point(528, 10);
			this.checkBoxHex.Name = "checkBoxHex";
			this.checkBoxHex.TabIndex = 11;
			this.checkBoxHex.Text = "Show in Hex";
			// 
			// MidiDevices
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(688, 342);
			this.Controls.Add(this.checkBoxHex);
			this.Controls.Add(this.checkBoxAutoPair);
			this.Controls.Add(this.checkBoxSystemExclusive);
			this.Controls.Add(this.checkBoxSystemCommon);
			this.Controls.Add(this.checkBoxChannel);
			this.Controls.Add(this.textBoxMsg);
			this.Controls.Add(this.checkBoxSystemRealtime);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBox1);
			this.Controls.Add(this.buttonStop);
			this.Controls.Add(this.buttonStart);
			this.Controls.Add(this.label2);
			this.Name = "MidiDevices";
			this.Text = "Midi Messages (Input)";
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new MidiDevices());
		}

		private MidiInputDevice _inDevice = null;

		private void buttonStart_Click(object sender, System.EventArgs e)
		{
			StartDevice(this.comboBox1.SelectedIndex);
		}

		private void buttonStop_Click(object sender, System.EventArgs e)
		{
			StopDevice();
		}

		private void StartDevice(int deviceID)
		{
			this.textBoxMsg.Clear();
			_inDevice = new MidiInputDevice(deviceID);
			_inDevice.AutoPairController = this.checkBoxAutoPair.Checked;
			_inDevice.MessageFilter = BuildFilter();
			_inDevice.MessageReceived += new MidiMessageEventHandler(InDevice_MessageReceived);
			if ( _inDevice.Open() )
			{
				this.buttonStop.Enabled = true;
				this.buttonStart.Enabled = false;
				if ( !_inDevice.Start() )
					MessageBox.Show( this, "Midi device could not be started! Error " + _inDevice.LastErrorCode.ToString(), "Midi Error");
			}
			else
				MessageBox.Show( this, "Midi device could not be opened! Error " + _inDevice.LastErrorCode.ToString(), "Midi Error");
		}

		private void StopDevice()
		{
			if (_inDevice != null && _inDevice.IsStarted)
			{
				_inDevice.Stop();
				_inDevice.Close();
				_inDevice.MessageReceived -= new MidiMessageEventHandler(InDevice_MessageReceived);
				this.buttonStop.Enabled = false;
				this.buttonStart.Enabled = true;
			}
		}

		private void FilterChanged(object sender, System.EventArgs e)
		{
			if (_inDevice != null && _inDevice.IsStarted)
			{
				_inDevice.MessageFilter = BuildFilter();
			}
		}

		private MIDIMessageType BuildFilter()
		{
			MIDIMessageType filter = MIDIMessageType.Unknown;
			if (!this.checkBoxChannel.Checked)
				filter |= MIDIMessageType.Channel;
			if (!this.checkBoxSystemCommon.Checked)
				filter |= MIDIMessageType.SystemCommon;
			if (!this.checkBoxSystemRealtime.Checked)
				filter |= MIDIMessageType.SystemRealtime;
			if (!this.checkBoxSystemExclusive.Checked)
				filter |= MIDIMessageType.SystemExclusive;
			return filter;
		}

		private void OnAutoPair(object sender, System.EventArgs e)
		{
			if (_inDevice != null && _inDevice.IsStarted)
			{
				_inDevice.AutoPairController = this.checkBoxAutoPair.Checked;
			}
		}

		private void SendSysExBuffer(IntPtr handle, byte[] data)
		{
			MidiSysExMessage sysex = new MidiSysExMessage(true, handle);
			sysex.CreateBuffer(data);
			// If the header was perpared successfully.
			if (sysex.Prepare())
			{
				// send a system-exclusive message to the output device
				Midi.MIDI_OutLongMsg(handle, sysex.MessageAsIntPtr);
			}
		}

		private void InDevice_MessageReceived(object sender, MidiMessageEventArgs e)
		{
			if (this.textBoxMsg.Text.Length > 32000)
				this.textBoxMsg.Clear();

			if (e.IsShortMessage)
			{
				if (this.checkBoxHex.Checked)
					this.textBoxMsg.AppendText( String.Format( "{0} : {1}\r\n", e.ShortMessage.ID, e.ShortMessage.ToString("{T}\t{A} {H}") ) );
				else
					this.textBoxMsg.AppendText( String.Format( "{0} : {1}\r\n", e.ShortMessage.ID, e.ShortMessage.ToString("G") ) );
			}
			else if (e.IsSysExMessage)
			{
				this.textBoxMsg.AppendText( String.Format( "{0} : {1}\r\n", e.SysExMessage.ID, e.SysExMessage.ToString() ) );
			}
			else if (e.EventType == MidiMessageEventType.Opened)
			{
				this.textBoxMsg.AppendText( String.Format( "Midi device {0} opened.\r\n", e.DeviceID ) );
			}
			else if (e.EventType == MidiMessageEventType.Closed)
			{
				this.textBoxMsg.AppendText( String.Format( "Midi device {0} closed.\r\n", e.DeviceID ) );
			}
			else if (e.EventType == MidiMessageEventType.Started)
			{
				this.textBoxMsg.AppendText( String.Format( "Midi device {0} started.\r\n", e.DeviceID ) );
			}
			else if (e.EventType == MidiMessageEventType.Stopped)
			{
				this.textBoxMsg.AppendText( String.Format( "Midi device {0} stopped.\r\n", e.DeviceID ) );
			}
		}

	}
}
