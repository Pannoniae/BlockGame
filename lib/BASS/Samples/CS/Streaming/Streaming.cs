using System;
using System.Xml;
using System.IO;
using System.Threading;
using System.Net;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.Wma;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass.Misc;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!
	//       needs 'bassenc.dll' - copy it to your output directory first!
	//       needs 'basswma.dll' - copy it to your output directory first!

	// NOTE: At least one of the following command-line encoders are needed as well:
	//       Copy the following files to your output directory first!
	//       lame.exe        : from http://www.rarewares.org/mp3.html
	//       oggenc2.exe     : from http://www.rarewares.org/ogg.html
	//       enc_aacPlus.exe : from http://www.un4seen.com/filez/4/enc_aacPlus.zip
	//                         also needs 'enc_aacPlus.dll' and 'nscrt.dll'
	//                         which can be obtained from your Winamp/plugin directory

	public class Streaming : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonStartRec;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonStopRec;
		private System.Windows.Forms.ProgressBar progressBarRecL;
		private System.Windows.Forms.ProgressBar progressBarRecR;
		private System.Windows.Forms.TextBox textBoxSong;
		private System.Windows.Forms.Button buttonUpdateTitle;
		private System.Windows.Forms.RadioButton radioButtonLAME;
		private System.Windows.Forms.Label labelGenre;
		private System.Windows.Forms.RadioButton radioButtonAAC;
		private System.Windows.Forms.TextBox textBoxSCIRC;
		private System.Windows.Forms.TextBox textBoxSCICQ;
		private System.Windows.Forms.TextBox textBoxSCAIM;
		private System.Windows.Forms.TabPage tabPageSHOUTcast;
		private System.Windows.Forms.Label labelRMS;
		private System.Windows.Forms.ListBox listBoxMessage;
		private System.Windows.Forms.Label labelStatus;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.TabPage tabPageICEcast;
		private System.Windows.Forms.Label labelServer;
		private System.Windows.Forms.Label labelPort;
		private System.Windows.Forms.Label labelPwd;
		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.Label labelURL;
		private System.Windows.Forms.Label labelHint;
		private System.Windows.Forms.TextBox textBoxServerAddress;
		private System.Windows.Forms.RadioButton radioButtonOGG;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.TextBox textBoxPort;
		private System.Windows.Forms.CheckBox checkBoxPublic;
		private System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.TextBox textBoxGenre;
		private System.Windows.Forms.TextBox textBoxUrl;
		private System.Windows.Forms.Label labelMountpoint;
		private System.Windows.Forms.TextBox textBoxICMountpoint;
		private System.Windows.Forms.Label labelEnc;
		private System.Windows.Forms.TabControl tabControlBroadCast;
		private System.Windows.Forms.TabPage tabPageWMAcast;
		private System.Windows.Forms.RadioButton radioButtonWMA;
		private System.Windows.Forms.CheckBox checkBoxWMAPublish;
		private System.Windows.Forms.Label labelWMAPublish;
		private System.Windows.Forms.CheckBox checkBoxUseBASS;
		private System.Windows.Forms.CheckBox checkBoxAutoReconnect;
		private System.Windows.Forms.ComboBox comboBoxKbps;
		private System.Windows.Forms.Label label1;
		private System.ComponentModel.IContainer components;

		public Streaming()
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
			this.buttonStartRec = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.labelRMS = new System.Windows.Forms.Label();
			this.textBoxSong = new System.Windows.Forms.TextBox();
			this.progressBarRecR = new System.Windows.Forms.ProgressBar();
			this.labelStatus = new System.Windows.Forms.Label();
			this.progressBarRecL = new System.Windows.Forms.ProgressBar();
			this.buttonStopRec = new System.Windows.Forms.Button();
			this.buttonUpdateTitle = new System.Windows.Forms.Button();
			this.radioButtonLAME = new System.Windows.Forms.RadioButton();
			this.radioButtonAAC = new System.Windows.Forms.RadioButton();
			this.labelServer = new System.Windows.Forms.Label();
			this.labelPort = new System.Windows.Forms.Label();
			this.labelPwd = new System.Windows.Forms.Label();
			this.labelName = new System.Windows.Forms.Label();
			this.labelGenre = new System.Windows.Forms.Label();
			this.labelURL = new System.Windows.Forms.Label();
			this.textBoxSCIRC = new System.Windows.Forms.TextBox();
			this.textBoxSCICQ = new System.Windows.Forms.TextBox();
			this.textBoxSCAIM = new System.Windows.Forms.TextBox();
			this.tabControlBroadCast = new System.Windows.Forms.TabControl();
			this.tabPageSHOUTcast = new System.Windows.Forms.TabPage();
			this.tabPageICEcast = new System.Windows.Forms.TabPage();
			this.textBoxICMountpoint = new System.Windows.Forms.TextBox();
			this.labelMountpoint = new System.Windows.Forms.Label();
			this.tabPageWMAcast = new System.Windows.Forms.TabPage();
			this.labelWMAPublish = new System.Windows.Forms.Label();
			this.checkBoxWMAPublish = new System.Windows.Forms.CheckBox();
			this.listBoxMessage = new System.Windows.Forms.ListBox();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.labelHint = new System.Windows.Forms.Label();
			this.textBoxServerAddress = new System.Windows.Forms.TextBox();
			this.radioButtonOGG = new System.Windows.Forms.RadioButton();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.textBoxPort = new System.Windows.Forms.TextBox();
			this.checkBoxPublic = new System.Windows.Forms.CheckBox();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.textBoxGenre = new System.Windows.Forms.TextBox();
			this.textBoxUrl = new System.Windows.Forms.TextBox();
			this.labelEnc = new System.Windows.Forms.Label();
			this.radioButtonWMA = new System.Windows.Forms.RadioButton();
			this.checkBoxUseBASS = new System.Windows.Forms.CheckBox();
			this.checkBoxAutoReconnect = new System.Windows.Forms.CheckBox();
			this.comboBoxKbps = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.tabControlBroadCast.SuspendLayout();
			this.tabPageSHOUTcast.SuspendLayout();
			this.tabPageICEcast.SuspendLayout();
			this.tabPageWMAcast.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonStartRec
			// 
			this.buttonStartRec.Location = new System.Drawing.Point(8, 22);
			this.buttonStartRec.Name = "buttonStartRec";
			this.buttonStartRec.Size = new System.Drawing.Size(86, 23);
			this.buttonStartRec.TabIndex = 0;
			this.buttonStartRec.Text = "Connect";
			this.buttonStartRec.Click += new System.EventHandler(this.buttonStartRec_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.labelRMS);
			this.groupBox1.Controls.Add(this.textBoxSong);
			this.groupBox1.Controls.Add(this.progressBarRecR);
			this.groupBox1.Controls.Add(this.labelStatus);
			this.groupBox1.Controls.Add(this.progressBarRecL);
			this.groupBox1.Controls.Add(this.buttonStopRec);
			this.groupBox1.Controls.Add(this.buttonStartRec);
			this.groupBox1.Controls.Add(this.buttonUpdateTitle);
			this.groupBox1.Location = new System.Drawing.Point(6, 8);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(410, 120);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Live Broadcast Streaming";
			// 
			// labelRMS
			// 
			this.labelRMS.Location = new System.Drawing.Point(8, 100);
			this.labelRMS.Name = "labelRMS";
			this.labelRMS.Size = new System.Drawing.Size(394, 18);
			this.labelRMS.TabIndex = 7;
			this.labelRMS.Text = "RMS:";
			this.labelRMS.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.labelRMS.Click += new System.EventHandler(this.labelRMS_Click);
			// 
			// textBoxSong
			// 
			this.textBoxSong.Location = new System.Drawing.Point(196, 24);
			this.textBoxSong.Name = "textBoxSong";
			this.textBoxSong.Size = new System.Drawing.Size(150, 20);
			this.textBoxSong.TabIndex = 2;
			this.textBoxSong.Text = "Enter song-title here";
			// 
			// progressBarRecR
			// 
			this.progressBarRecR.Location = new System.Drawing.Point(8, 84);
			this.progressBarRecR.Maximum = 32768;
			this.progressBarRecR.Name = "progressBarRecR";
			this.progressBarRecR.Size = new System.Drawing.Size(394, 14);
			this.progressBarRecR.Step = 1;
			this.progressBarRecR.TabIndex = 6;
			// 
			// labelStatus
			// 
			this.labelStatus.Location = new System.Drawing.Point(8, 50);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(394, 18);
			this.labelStatus.TabIndex = 4;
			// 
			// progressBarRecL
			// 
			this.progressBarRecL.Location = new System.Drawing.Point(8, 68);
			this.progressBarRecL.Maximum = 32768;
			this.progressBarRecL.Name = "progressBarRecL";
			this.progressBarRecL.Size = new System.Drawing.Size(394, 14);
			this.progressBarRecL.Step = 1;
			this.progressBarRecL.TabIndex = 5;
			// 
			// buttonStopRec
			// 
			this.buttonStopRec.Location = new System.Drawing.Point(100, 22);
			this.buttonStopRec.Name = "buttonStopRec";
			this.buttonStopRec.Size = new System.Drawing.Size(86, 23);
			this.buttonStopRec.TabIndex = 1;
			this.buttonStopRec.Text = "Disconnect";
			this.buttonStopRec.Click += new System.EventHandler(this.buttonStopRec_Click);
			// 
			// buttonUpdateTitle
			// 
			this.buttonUpdateTitle.Location = new System.Drawing.Point(346, 24);
			this.buttonUpdateTitle.Name = "buttonUpdateTitle";
			this.buttonUpdateTitle.Size = new System.Drawing.Size(56, 20);
			this.buttonUpdateTitle.TabIndex = 3;
			this.buttonUpdateTitle.Text = "Update";
			this.buttonUpdateTitle.Click += new System.EventHandler(this.buttonUpdateTitle_Click);
			// 
			// radioButtonLAME
			// 
			this.radioButtonLAME.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonLAME.Checked = true;
			this.radioButtonLAME.Location = new System.Drawing.Point(500, 240);
			this.radioButtonLAME.Name = "radioButtonLAME";
			this.radioButtonLAME.Size = new System.Drawing.Size(50, 24);
			this.radioButtonLAME.TabIndex = 17;
			this.radioButtonLAME.TabStop = true;
			this.radioButtonLAME.Text = "MP3";
			this.radioButtonLAME.Click += new System.EventHandler(this.radioButtonLAME_Click);
			this.radioButtonLAME.CheckedChanged += new System.EventHandler(this.radioButtonLAME_CheckedChanged);
			// 
			// radioButtonAAC
			// 
			this.radioButtonAAC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonAAC.Location = new System.Drawing.Point(552, 240);
			this.radioButtonAAC.Name = "radioButtonAAC";
			this.radioButtonAAC.Size = new System.Drawing.Size(54, 24);
			this.radioButtonAAC.TabIndex = 18;
			this.radioButtonAAC.Text = "AAC";
			this.radioButtonAAC.Click += new System.EventHandler(this.radioButtonAAC_Click);
			this.radioButtonAAC.CheckedChanged += new System.EventHandler(this.radioButtonAAC_CheckedChanged);
			// 
			// labelServer
			// 
			this.labelServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelServer.Location = new System.Drawing.Point(432, 52);
			this.labelServer.Name = "labelServer";
			this.labelServer.Size = new System.Drawing.Size(64, 20);
			this.labelServer.TabIndex = 3;
			this.labelServer.Text = "Server:";
			this.labelServer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelPort
			// 
			this.labelPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelPort.Location = new System.Drawing.Point(616, 52);
			this.labelPort.Name = "labelPort";
			this.labelPort.Size = new System.Drawing.Size(34, 20);
			this.labelPort.TabIndex = 5;
			this.labelPort.Text = "Port:";
			this.labelPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelPwd
			// 
			this.labelPwd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelPwd.Location = new System.Drawing.Point(432, 76);
			this.labelPwd.Name = "labelPwd";
			this.labelPwd.Size = new System.Drawing.Size(64, 20);
			this.labelPwd.TabIndex = 7;
			this.labelPwd.Text = "Password:";
			this.labelPwd.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelName
			// 
			this.labelName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelName.Location = new System.Drawing.Point(434, 104);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(64, 20);
			this.labelName.TabIndex = 10;
			this.labelName.Text = "Name:";
			this.labelName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGenre
			// 
			this.labelGenre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelGenre.Location = new System.Drawing.Point(434, 128);
			this.labelGenre.Name = "labelGenre";
			this.labelGenre.Size = new System.Drawing.Size(64, 20);
			this.labelGenre.TabIndex = 12;
			this.labelGenre.Text = "Genre:";
			this.labelGenre.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelURL
			// 
			this.labelURL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelURL.Location = new System.Drawing.Point(434, 152);
			this.labelURL.Name = "labelURL";
			this.labelURL.Size = new System.Drawing.Size(64, 20);
			this.labelURL.TabIndex = 14;
			this.labelURL.Text = "URL:";
			this.labelURL.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// textBoxSCIRC
			// 
			this.textBoxSCIRC.Location = new System.Drawing.Point(74, 144);
			this.textBoxSCIRC.MaxLength = 64;
			this.textBoxSCIRC.Name = "textBoxSCIRC";
			this.textBoxSCIRC.Size = new System.Drawing.Size(44, 20);
			this.textBoxSCIRC.TabIndex = 0;
			this.textBoxSCIRC.Text = "irc";
			// 
			// textBoxSCICQ
			// 
			this.textBoxSCICQ.Location = new System.Drawing.Point(152, 144);
			this.textBoxSCICQ.MaxLength = 64;
			this.textBoxSCICQ.Name = "textBoxSCICQ";
			this.textBoxSCICQ.Size = new System.Drawing.Size(44, 20);
			this.textBoxSCICQ.TabIndex = 1;
			this.textBoxSCICQ.Text = "icq";
			// 
			// textBoxSCAIM
			// 
			this.textBoxSCAIM.Location = new System.Drawing.Point(234, 144);
			this.textBoxSCAIM.MaxLength = 64;
			this.textBoxSCAIM.Name = "textBoxSCAIM";
			this.textBoxSCAIM.Size = new System.Drawing.Size(44, 20);
			this.textBoxSCAIM.TabIndex = 2;
			this.textBoxSCAIM.Text = "aim";
			// 
			// tabControlBroadCast
			// 
			this.tabControlBroadCast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControlBroadCast.Controls.Add(this.tabPageSHOUTcast);
			this.tabControlBroadCast.Controls.Add(this.tabPageICEcast);
			this.tabControlBroadCast.Controls.Add(this.tabPageWMAcast);
			this.tabControlBroadCast.Location = new System.Drawing.Point(420, 14);
			this.tabControlBroadCast.Name = "tabControlBroadCast";
			this.tabControlBroadCast.SelectedIndex = 0;
			this.tabControlBroadCast.Size = new System.Drawing.Size(304, 260);
			this.tabControlBroadCast.TabIndex = 2;
			this.tabControlBroadCast.SelectedIndexChanged += new System.EventHandler(this.tabControlBroadCast_SelectedIndexChanged);
			// 
			// tabPageSHOUTcast
			// 
			this.tabPageSHOUTcast.Controls.Add(this.textBoxSCIRC);
			this.tabPageSHOUTcast.Controls.Add(this.textBoxSCICQ);
			this.tabPageSHOUTcast.Controls.Add(this.textBoxSCAIM);
			this.tabPageSHOUTcast.Location = new System.Drawing.Point(4, 22);
			this.tabPageSHOUTcast.Name = "tabPageSHOUTcast";
			this.tabPageSHOUTcast.Size = new System.Drawing.Size(296, 234);
			this.tabPageSHOUTcast.TabIndex = 0;
			this.tabPageSHOUTcast.Text = "SHOUTcast";
			// 
			// tabPageICEcast
			// 
			this.tabPageICEcast.Controls.Add(this.textBoxICMountpoint);
			this.tabPageICEcast.Controls.Add(this.labelMountpoint);
			this.tabPageICEcast.Location = new System.Drawing.Point(4, 22);
			this.tabPageICEcast.Name = "tabPageICEcast";
			this.tabPageICEcast.Size = new System.Drawing.Size(296, 234);
			this.tabPageICEcast.TabIndex = 1;
			this.tabPageICEcast.Text = "ICEcast";
			// 
			// textBoxICMountpoint
			// 
			this.textBoxICMountpoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxICMountpoint.Location = new System.Drawing.Point(74, 144);
			this.textBoxICMountpoint.Name = "textBoxICMountpoint";
			this.textBoxICMountpoint.Size = new System.Drawing.Size(204, 20);
			this.textBoxICMountpoint.TabIndex = 29;
			this.textBoxICMountpoint.Text = "/bass.ogg";
			// 
			// labelMountpoint
			// 
			this.labelMountpoint.Location = new System.Drawing.Point(8, 144);
			this.labelMountpoint.Name = "labelMountpoint";
			this.labelMountpoint.Size = new System.Drawing.Size(64, 20);
			this.labelMountpoint.TabIndex = 28;
			this.labelMountpoint.Text = "Mountpoint:";
			this.labelMountpoint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tabPageWMAcast
			// 
			this.tabPageWMAcast.Controls.Add(this.labelWMAPublish);
			this.tabPageWMAcast.Controls.Add(this.checkBoxWMAPublish);
			this.tabPageWMAcast.Location = new System.Drawing.Point(4, 22);
			this.tabPageWMAcast.Name = "tabPageWMAcast";
			this.tabPageWMAcast.Size = new System.Drawing.Size(296, 234);
			this.tabPageWMAcast.TabIndex = 2;
			this.tabPageWMAcast.Text = "WMAcast";
			// 
			// labelWMAPublish
			// 
			this.labelWMAPublish.Location = new System.Drawing.Point(12, 134);
			this.labelWMAPublish.Name = "labelWMAPublish";
			this.labelWMAPublish.Size = new System.Drawing.Size(106, 23);
			this.labelWMAPublish.TabIndex = 1;
			this.labelWMAPublish.Text = "of Publishing Point";
			this.labelWMAPublish.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// checkBoxWMAPublish
			// 
			this.checkBoxWMAPublish.Location = new System.Drawing.Point(136, 134);
			this.checkBoxWMAPublish.Name = "checkBoxWMAPublish";
			this.checkBoxWMAPublish.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.checkBoxWMAPublish.Size = new System.Drawing.Size(144, 23);
			this.checkBoxWMAPublish.TabIndex = 0;
			this.checkBoxWMAPublish.Text = "stream to Publish Point";
			// 
			// listBoxMessage
			// 
			this.listBoxMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.listBoxMessage.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.listBoxMessage.HorizontalScrollbar = true;
			this.listBoxMessage.ItemHeight = 14;
			this.listBoxMessage.Location = new System.Drawing.Point(6, 128);
			this.listBoxMessage.Name = "listBoxMessage";
			this.listBoxMessage.ScrollAlwaysVisible = true;
			this.listBoxMessage.Size = new System.Drawing.Size(410, 144);
			this.listBoxMessage.TabIndex = 1;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 2000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// labelHint
			// 
			this.labelHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelHint.Location = new System.Drawing.Point(12, 294);
			this.labelHint.Name = "labelHint";
			this.labelHint.Size = new System.Drawing.Size(708, 26);
			this.labelHint.TabIndex = 20;
			this.labelHint.Text = "Make sure the bass.dll, bassenc.dll and basswma.dll version 2.4 are in your execut" +
				"able directory!";
			this.labelHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// textBoxServerAddress
			// 
			this.textBoxServerAddress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxServerAddress.Location = new System.Drawing.Point(498, 52);
			this.textBoxServerAddress.Name = "textBoxServerAddress";
			this.textBoxServerAddress.Size = new System.Drawing.Size(110, 20);
			this.textBoxServerAddress.TabIndex = 4;
			this.textBoxServerAddress.Text = "localhost";
			// 
			// radioButtonOGG
			// 
			this.radioButtonOGG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonOGG.Location = new System.Drawing.Point(604, 240);
			this.radioButtonOGG.Name = "radioButtonOGG";
			this.radioButtonOGG.Size = new System.Drawing.Size(54, 24);
			this.radioButtonOGG.TabIndex = 19;
			this.radioButtonOGG.Text = "OGG";
			this.radioButtonOGG.Click += new System.EventHandler(this.radioButtonOGG_Click);
			this.radioButtonOGG.CheckedChanged += new System.EventHandler(this.radioButtonOGG_CheckedChanged);
			// 
			// textBoxPassword
			// 
			this.textBoxPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxPassword.Location = new System.Drawing.Point(498, 76);
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.Size = new System.Drawing.Size(110, 20);
			this.textBoxPassword.TabIndex = 8;
			this.textBoxPassword.Text = "changeme";
			// 
			// textBoxPort
			// 
			this.textBoxPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxPort.Location = new System.Drawing.Point(652, 52);
			this.textBoxPort.Name = "textBoxPort";
			this.textBoxPort.Size = new System.Drawing.Size(50, 20);
			this.textBoxPort.TabIndex = 6;
			this.textBoxPort.Text = "8000";
			// 
			// checkBoxPublic
			// 
			this.checkBoxPublic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxPublic.Location = new System.Drawing.Point(652, 74);
			this.checkBoxPublic.Name = "checkBoxPublic";
			this.checkBoxPublic.Size = new System.Drawing.Size(58, 24);
			this.checkBoxPublic.TabIndex = 9;
			this.checkBoxPublic.Text = "Public";
			// 
			// textBoxName
			// 
			this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxName.Location = new System.Drawing.Point(498, 104);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.Size = new System.Drawing.Size(204, 20);
			this.textBoxName.TabIndex = 11;
			this.textBoxName.Text = "Your Station Name";
			// 
			// textBoxGenre
			// 
			this.textBoxGenre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxGenre.Location = new System.Drawing.Point(498, 128);
			this.textBoxGenre.Name = "textBoxGenre";
			this.textBoxGenre.Size = new System.Drawing.Size(204, 20);
			this.textBoxGenre.TabIndex = 13;
			this.textBoxGenre.Text = "Lounge Jazz House";
			// 
			// textBoxUrl
			// 
			this.textBoxUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxUrl.Location = new System.Drawing.Point(498, 152);
			this.textBoxUrl.Name = "textBoxUrl";
			this.textBoxUrl.Size = new System.Drawing.Size(204, 20);
			this.textBoxUrl.TabIndex = 15;
			this.textBoxUrl.Text = "http://www.radio42.com";
			// 
			// labelEnc
			// 
			this.labelEnc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelEnc.Location = new System.Drawing.Point(434, 242);
			this.labelEnc.Name = "labelEnc";
			this.labelEnc.Size = new System.Drawing.Size(58, 18);
			this.labelEnc.TabIndex = 16;
			this.labelEnc.Text = "Encoder:";
			this.labelEnc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// radioButtonWMA
			// 
			this.radioButtonWMA.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonWMA.Location = new System.Drawing.Point(660, 240);
			this.radioButtonWMA.Name = "radioButtonWMA";
			this.radioButtonWMA.Size = new System.Drawing.Size(54, 24);
			this.radioButtonWMA.TabIndex = 21;
			this.radioButtonWMA.Text = "WMA";
			this.radioButtonWMA.Click += new System.EventHandler(this.radioButtonWMA_Click);
			this.radioButtonWMA.CheckedChanged += new System.EventHandler(this.radioButtonWMA_CheckedChanged);
			// 
			// checkBoxUseBASS
			// 
			this.checkBoxUseBASS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxUseBASS.Checked = true;
			this.checkBoxUseBASS.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxUseBASS.Location = new System.Drawing.Point(620, 10);
			this.checkBoxUseBASS.Name = "checkBoxUseBASS";
			this.checkBoxUseBASS.TabIndex = 22;
			this.checkBoxUseBASS.Text = "Use BASS";
			// 
			// checkBoxAutoReconnect
			// 
			this.checkBoxAutoReconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxAutoReconnect.Checked = true;
			this.checkBoxAutoReconnect.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAutoReconnect.Location = new System.Drawing.Point(438, 214);
			this.checkBoxAutoReconnect.Name = "checkBoxAutoReconnect";
			this.checkBoxAutoReconnect.TabIndex = 23;
			this.checkBoxAutoReconnect.Text = "Auto-Reconnect";
			this.checkBoxAutoReconnect.CheckedChanged += new System.EventHandler(this.checkBoxAutoReconnect_CheckedChanged);
			// 
			// comboBoxKbps
			// 
			this.comboBoxKbps.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxKbps.Items.AddRange(new object[] {
															  "56",
															  "64",
															  "96",
															  "128",
															  "160",
															  "192"});
			this.comboBoxKbps.Location = new System.Drawing.Point(658, 216);
			this.comboBoxKbps.MaxDropDownItems = 6;
			this.comboBoxKbps.Name = "comboBoxKbps";
			this.comboBoxKbps.Size = new System.Drawing.Size(48, 21);
			this.comboBoxKbps.TabIndex = 24;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(618, 216);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(34, 21);
			this.label1.TabIndex = 25;
			this.label1.Text = "Kbps:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// Streaming
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(732, 326);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxKbps);
			this.Controls.Add(this.checkBoxAutoReconnect);
			this.Controls.Add(this.checkBoxUseBASS);
			this.Controls.Add(this.textBoxUrl);
			this.Controls.Add(this.textBoxGenre);
			this.Controls.Add(this.textBoxName);
			this.Controls.Add(this.textBoxPort);
			this.Controls.Add(this.textBoxPassword);
			this.Controls.Add(this.textBoxServerAddress);
			this.Controls.Add(this.radioButtonLAME);
			this.Controls.Add(this.radioButtonWMA);
			this.Controls.Add(this.labelEnc);
			this.Controls.Add(this.checkBoxPublic);
			this.Controls.Add(this.radioButtonOGG);
			this.Controls.Add(this.radioButtonAAC);
			this.Controls.Add(this.labelServer);
			this.Controls.Add(this.labelPort);
			this.Controls.Add(this.labelPwd);
			this.Controls.Add(this.labelGenre);
			this.Controls.Add(this.labelURL);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.listBoxMessage);
			this.Controls.Add(this.tabControlBroadCast);
			this.Controls.Add(this.labelHint);
			this.Controls.Add(this.groupBox1);
			this.Name = "Streaming";
			this.Text = "Live Streaming";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.Streaming_Closing);
			this.Load += new System.EventHandler(this.Streaming_Load);
			this.groupBox1.ResumeLayout(false);
			this.tabControlBroadCast.ResumeLayout(false);
			this.tabPageSHOUTcast.ResumeLayout(false);
			this.tabPageICEcast.ResumeLayout(false);
			this.tabPageWMAcast.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new Streaming());
		}

		// LOCAL VARS
		private int _recHandle = 0;
		private DSP_PeakLevelMeter _plm;
		private BroadCast _broadCast = null;
		private RECORDPROC _recProc;
		
		private void Streaming_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your mail", "your registration key");

			if ( !Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) )
				MessageBox.Show(this, "Bass_Init error!" );

			//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_FLOATDSP, Bass.TRUE );

			// init your recording device (we use the default device)
			if ( !Bass.BASS_RecordInit(-1) )
				MessageBox.Show(this, "Bass_RecordInit error!" );

			_recProc = new RECORDPROC(RecordingHandler);
			// start recording at 44.1kHz, stereo
			_recHandle = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_DEFAULT, _recProc, IntPtr.Zero);
			if (_recHandle == Bass.FALSE)
				MessageBox.Show(this, "BASS_RecordStart error!" );

			// set up a ready-made DSP (here the PeakLevelMeter)
			_plm = new DSP_PeakLevelMeter(_recHandle, 1);
			_plm.CalcRMS = true;
			_plm.Notification += new EventHandler(_plm_Notification);

			this.comboBoxKbps.SelectedIndex = 1;
		}

		private void Streaming_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_broadCast != null && _broadCast.IsConnected)
			{
				_broadCast.Disconnect();
				_broadCast = null;
				GC.Collect();
			}

			// close bass
			Bass.BASS_ChannelStop(_recHandle);
			Bass.BASS_RecordFree();
			Bass.BASS_Stop();
			Bass.BASS_Free();
		}

		private void buttonStartRec_Click(object sender, System.EventArgs e)
		{
			this.buttonStartRec.Enabled = false;
			this.labelStatus.Text = "Connecting, please wait...";

			int kbps = Int32.Parse( this.comboBoxKbps.Text );

			IBaseEncoder encoder = null;
			IStreamingServer server = null;

			// create an encoder instance
			if (radioButtonLAME.Checked)
			{
				// MP3
				EncoderLAME lame = new EncoderLAME(_recHandle);
				lame.InputFile = null;	//STDIN
				lame.OutputFile = null;	//STDOUT
				lame.LAME_Bitrate = kbps;
				lame.LAME_Mode = EncoderLAME.LAMEMode.Default;
				lame.LAME_TargetSampleRate = (int)EncoderLAME.SAMPLERATE.Hz_44100;
				lame.LAME_Quality = EncoderLAME.LAMEQuality.Quality;
				if (lame.EncoderExists)
				{
					encoder = lame; 
				}
				else
				{
					MessageBox.Show( this, "LAME.EXE can not be found or does not exist!\r\n\r\nMake sure it exists in your executable directory.");
					this.buttonStartRec.Enabled = true;
					this.labelStatus.Text = "Not Connected";
					return;
				}
			}
			else if (radioButtonAAC.Checked)
			{
				// AACplus
				EncoderWinampAACplus aac = new EncoderWinampAACplus(_recHandle);
				aac.InputFile = null;	//STDIN
				aac.OutputFile = null;	//STDOUT
				aac.AACPlus_Bitrate = kbps;
				if (aac.EncoderExists)
				{
					encoder = aac;
				}
				else
				{
					MessageBox.Show( this, "enc_aacPlus.exe or enc_aacPlus.dll can not be found or does not exist!\r\n\r\nMake sure these libraries and the nscrt.dll exists in your executable directory.");
					this.buttonStartRec.Enabled = true;
					this.labelStatus.Text = "Not Connected";
					return;
				}
			}
			else if (radioButtonOGG.Checked)
			{
				// OGG
				EncoderOGG ogg = new EncoderOGG(_recHandle);
				ogg.InputFile = null;	//STDIN
				ogg.OutputFile = null;	//STDOUT
				ogg.OGG_UseQualityMode = false;
				ogg.OGG_UseManagedBitrate = true;
				ogg.OGG_Bitrate = kbps;
				ogg.OGG_MinBitrate = kbps;
				ogg.OGG_MaxBitrate = kbps;
				ogg.OGG_CustomOptions = "-s 42 -c EncodedBy=BASS.NET"; // just here for demo (not really needed at all ;-)
				if (ogg.EncoderExists)
				{
					encoder = ogg;
				}
				else
				{
					MessageBox.Show( this, "oggenc2.exe can not be found or does not exist!\r\n\r\nMake sure oggenc2.exe exists in your executable directory.");
					this.buttonStartRec.Enabled = true;
					this.labelStatus.Text = "Not Connected";
					return;
				}
			}
			else if (radioButtonWMA.Checked)
			{
				// WMA
				EncoderWMA wma = new EncoderWMA(_recHandle);
				wma.InputFile = null;	//STDIN
				wma.OutputFile = null;	//STDOUT
				wma.WMA_Bitrate = 64;
				encoder = wma;
			}
			
			// create an streaming server instance
			if (this.tabControlBroadCast.SelectedTab == this.tabPageSHOUTcast)
			{
				SHOUTcast shoutcast = new SHOUTcast(encoder, this.checkBoxUseBASS.Checked);
				shoutcast.ServerAddress = this.textBoxServerAddress.Text;
				shoutcast.ServerPort = int.Parse(this.textBoxPort.Text);
				shoutcast.Password = this.textBoxPassword.Text;
				shoutcast.PublicFlag = this.checkBoxPublic.Checked;
				shoutcast.Genre = this.textBoxGenre.Text;
				shoutcast.StationName = this.textBoxName.Text;
				shoutcast.Url = this.textBoxUrl.Text;
				shoutcast.Aim = this.textBoxSCAIM.Text;
				shoutcast.Icq = this.textBoxSCICQ.Text;
				shoutcast.Irc = this.textBoxSCIRC.Text;
				server = shoutcast;
			}
			else if (this.tabControlBroadCast.SelectedTab == this.tabPageICEcast)
			{
				ICEcast icecast = new ICEcast(encoder, this.checkBoxUseBASS.Checked);
				icecast.ServerAddress = this.textBoxServerAddress.Text;
				icecast.ServerPort = int.Parse(this.textBoxPort.Text);
				icecast.MountPoint = this.textBoxICMountpoint.Text;
				icecast.Username = "source";
				icecast.Password = this.textBoxPassword.Text;
				icecast.PublicFlag = this.checkBoxPublic.Checked;
				icecast.StreamGenre = this.textBoxGenre.Text;
				icecast.StreamName = this.textBoxName.Text;
				icecast.StreamDescription = this.textBoxName.Text;
				icecast.StreamUrl = this.textBoxUrl.Text;
				server = icecast;
			}
			else if (this.tabControlBroadCast.SelectedTab == this.tabPageWMAcast)
			{
				WMAcast wmacast = new WMAcast(encoder);
				wmacast.StreamAuthor = "Bernd Niedergesaess";
				wmacast.StreamPublisher = "see www.un4seen.com";
				wmacast.StreamCopyright = "(c) 2006 TEN53";
				wmacast.StreamDescription = this.textBoxName.Text;
				wmacast.StreamGenre = this.textBoxGenre.Text;
				wmacast.StreamRating = "5star broadcasting by BASS.NET";
				wmacast.StreamUrl = this.textBoxUrl.Text;
				if (this.checkBoxWMAPublish.Checked)
				{
					// streaming to a publishing point
					wmacast.PublishUrl = this.textBoxUrl.Text.Length > 0 ? this.textBoxUrl.Text : null;
					wmacast.PublishUsername = this.textBoxServerAddress.Text.Length > 0 ? this.textBoxServerAddress.Text : null;
					wmacast.PublishPassword = this.textBoxPassword.Text;
					wmacast.UsePublish = this.checkBoxWMAPublish.Checked;
				}
				else
				{
					// direct streaming to the network
					wmacast.NetworkClients = 5; 
					wmacast.NetworkPort = int.Parse(this.textBoxPort.Text);
				}
				server = wmacast;
			}
			// already set an initial song title ;-)
			server.SongTitle = "BASS.NET";

			// disconnect, if connected
			if (_broadCast != null && _broadCast.IsConnected)
			{
				_broadCast.Disconnect();
				_broadCast.Notification -= new BroadCastEventHandler(_broadCast_Notification);
			}
			_broadCast = null;
			GC.Collect();
			this.listBoxMessage.Items.Clear();

			try
			{
				// create the broadcast instance
				_broadCast = new BroadCast(server);
				_broadCast.AutoReconnect = checkBoxAutoReconnect.Checked;
				_broadCast.ReconnectTimeout = 5;
				_broadCast.Notification += new BroadCastEventHandler(_broadCast_Notification);
				if ( !_broadCast.AutoConnect() )
				{
					this.buttonStartRec.Enabled = true;
					this.labelStatus.Text = "Not Connected";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show( this, ex.Message, "Broadcast error!" );
				this.buttonStartRec.Enabled = true;
				this.labelStatus.Text = "Not Connected";
			}

			// The following code is just here to demo other possible usage scenarios of an encoder:
			
			/*
			// "Recording" To "File":
			EncoderNeroAAC n = new EncoderNeroAAC(_recHandle);
			n.InputFile = null;
			n.OutputFile = "test.m4a";
			n.NERO_Bitrate = 48;
			n.Start(null, 0);
			// your recording will be encoded until you call...
			n.Stop();
			*/

			/*
			// "Stream (Decoding)" To "File":
			int _stream = Bass.BASS_StreamCreateFile( "test.wav", 0, 0, BASSFlag.BASS_STREAM_DECODE);
			EncoderLAME l = new EncoderLAME(_stream);
			l.InputFile = null;	//STDIN
			l.OutputFile = "test.mp3";
			l.LAME_Bitrate = (int)EncoderLAME.BITRATE.kbps_64;
			l.LAME_Mode = EncoderLAME.LAMEMode.Default;
			l.LAME_Quality = EncoderLAME.LAMEQuality.Quality;
			l.Start(null, 0);
			// encode the data
			byte[] encBuffer = new byte[65536]; // our dummy encoder buffer (32KB x 16-bit - size it as you like)
			while ( Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING )
			{
				// get the decoded sample data
				int len = Bass.BASS_ChannelGetData(_stream, encBuffer, encBuffer.Length);
			}
			// finish
			l.Stop();
			*/
			
			/*
			// "File" To "File":
			EncoderOGG o = new EncoderOGG(0);
			o.OGG_UseQualityMode = true;
			o.OGG_Quality = 4;
			BaseEncoder.EncodeFile( "test.wav", "test.ogg", o, null, true, false, true);
			*/

			/*
			// "Stream (Decoding)" To "File":
			int _stream = Bass.BASS_StreamCreateFile( "test.wav", 0, 0, BASSFlag.BASS_STREAM_DECODE);
			ACMFORMAT codec = BassEnc.BASS_Encode_GetACMFormat(_stream, (int)WAVEFormatTag.WAVE_FORMAT_MPEGLAYER3, BASSACMFormat.BASS_ACM_CHANS | BASSACMFormat.BASS_ACM_RATE);
			// change the MP3 codec to 256 kbps
			codec.waveformatex.nAvgBytesPerSec = 32000; // 256kbps (256000/8)
			EncoderACM acm = new EncoderACM(_stream);
			acm.ACM_Codec = codec;
			acm.ACM_WriteWaveHeader = false;
			acm.InputFile = null;	//STDIN
			acm.OutputFile = "acmtest.mp3";
			acm.Start(null, 0);
			// encode the data
			byte[] encBuffer = new byte[32768]; // our dummy encoder buffer (16KB x 16-bit - size it as you like)
			while ( Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING )
			{
				// get the decoded sample data
				int len = Bass.BASS_ChannelGetData(_stream, encBuffer, encBuffer.Length);
			}
			// finish
			acm.Stop();
			*/

			/*
			// "Stream (Decoding)" To "File":
			int _stream = Bass.BASS_StreamCreateFile( "test.wav", 0, 0, BASSFlag.BASS_STREAM_DECODE);
			EncoderWMA wma = new EncoderWMA(_stream);
			wma.WMA_UseVBR = true;
			wma.WMA_VBRQuality = 100; // lossless
            wma.InputFile = null;	//STDIN
			wma.OutputFile = "test.wma";
			wma.Start(null, 0);
			// encode the data right away
			int reqLength = 32768;
			int recLength = 32768;
			IntPtr buffer = IntPtr.Zero;
			// allocate a buffer of that size for unmanaged code
			buffer = Marshal.AllocCoTaskMem( reqLength );
			try
			{
				wma.SetTag( "Title", "This is my title!" );
				while ( Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING )
				{
					// get the decoded sample data
					recLength = Bass.BASS_ChannelGetData(_stream, buffer, reqLength);
					// and send it to the WMA encoder
					wma.Write(buffer, recLength);
				}
			}
			catch { }
			finally
			{
				// free the allocated unmanaged memory
				Marshal.FreeCoTaskMem( buffer );
			}
			wma.Stop();
			*/

			/*
			// "File" to "File":
			EncoderWMA wma = new EncoderWMA(0);
			wma.WMA_Bitrate = 128;
			BaseEncoder.EncodeFile( "test.mp3", null, wma, new BaseEncoder.ENCODEFILEPROC(FileEncodingNotification), true, false);
			*/

		}

		public void FileEncodingNotification(long bytesTotal, long bytesDone)
		{
			// only use with the above "BaseEncoder.EncodeFile"
			this.labelHint.Text = String.Format( "Encoding: {0:P}", Math.Round( (double)bytesDone/(double)bytesTotal, 4 ) );
			Application.DoEvents();
		}

		private void buttonStopRec_Click(object sender, System.EventArgs e)
		{
			if (_broadCast != null)
			{
				_broadCast.Disconnect();
				_broadCast = null;
				GC.Collect();
			}

			this.buttonStartRec.Enabled = true;
			this.labelStatus.Text = "Not connected!";
		}

		private void buttonUpdateTitle_Click(object sender, System.EventArgs e)
		{
			if (_broadCast != null)
			{
				_broadCast.UpdateTitle(this.textBoxSong.Text, null);
			}
		}

		private void _broadCast_Notification(object sender, BroadCastEventArgs e)
		{
			// sender will be the BroadCast instance
			// you could also access it via: BroadCast broadcast = (BroadCast)sender;

			if (_broadCast == null)
				return;

			if (e.EventType != BroadCastEventType.DataSend)
			{
				this.listBoxMessage.Items.Insert(0, 
					String.Format( "{0}: {1} : {2}", e.DateTime, e.EventType, e.Data ) );
				if (_broadCast.Server.LastError != StreamingServer.STREAMINGERROR.Ok )
				{
					this.listBoxMessage.Items.Insert(1, 
						String.Format( "    {0}: {1}", _broadCast.Server.LastError, _broadCast.Server.LastErrorMessage ) );
				}
			}

			if (_broadCast.IsConnected)
			{
				this.labelStatus.Text = String.Format( "Up since: {0}D {1:00}:{2:00}:{3:00}        Bytes send: {4:N00} KB", 
					_broadCast.TotalConnectionTime.Days, _broadCast.TotalConnectionTime.Hours, _broadCast.TotalConnectionTime.Minutes, _broadCast.TotalConnectionTime.Seconds,
					_broadCast.TotalBytesSend/1024 );
			}
			else
			{
				this.labelStatus.Text = "Not connected!";
			}

			if (this.listBoxMessage.Items.Count > 100)
				this.listBoxMessage.Items.RemoveAt(100);
		}

		private void _plm_Notification(object sender, EventArgs e)
		{
			// sender will be the DSP_PeakLevelMeter instance
			// you could also access it via: DSP_PeakLevelMeter plm = (DSP_PeakLevelMeter)sender;

			this.progressBarRecL.Value = _plm.LevelL;
			this.progressBarRecR.Value = _plm.LevelR;
			this.labelRMS.Text = String.Format( "RMS: {0:#00.0} dB        AVG: {1:#00.0} dB        Peak: {2:#00.0} dB", _plm.RMS_dBV, _plm.AVG_dBV, Math.Max(_plm.PeakHoldLevelL_dBV, _plm.PeakHoldLevelR_dBV) );
		}

		private void labelRMS_Click(object sender, System.EventArgs e)
		{
			_plm.ResetPeakHold();
		}

		private bool RecordingHandler(int handle, IntPtr buffer, int length, IntPtr user)
		{
			return true;
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			if (_plm != null)
				_plm.ResetPeakHold();

			if (_broadCast != null)
			{
				this.labelStatus.Text = String.Format( "Up since: {0}D {1:00}:{2:00}:{3:00}        Bytes send: {4:N00} KB", 
					_broadCast.TotalConnectionTime.Days, _broadCast.TotalConnectionTime.Hours, _broadCast.TotalConnectionTime.Minutes, _broadCast.TotalConnectionTime.Seconds,
					_broadCast.TotalBytesSend/1024 );
			}
		}

		private void tabControlBroadCast_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (this.tabControlBroadCast.SelectedTab == this.tabPageSHOUTcast)
			{
				this.radioButtonLAME.Checked = true;
				this.labelServer.Text = "Server:";
			}
			else if (this.tabControlBroadCast.SelectedTab == this.tabPageICEcast)
			{
				this.radioButtonOGG.Checked = true;
				this.labelServer.Text = "Server:";
				if ( !textBoxICMountpoint.Text.EndsWith(".ogg") )
					textBoxICMountpoint.Text += ".ogg";
			}
			else if (this.tabControlBroadCast.SelectedTab == this.tabPageWMAcast)
			{
				this.radioButtonWMA.Checked = true;
				this.labelServer.Text = "User:";
			}
		}

		private void radioButtonLAME_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButtonLAME.Checked)
			{
				if (this.tabControlBroadCast.SelectedTab == this.tabPageWMAcast)
				{
					MessageBox.Show( this, "MP3 Encoder can not be used with the WMAcast streaming server!" );
					this.radioButtonWMA.Checked = true;
				}
				else if (this.tabControlBroadCast.SelectedTab == this.tabPageICEcast)
				{
					if ( textBoxICMountpoint.Text.EndsWith(".ogg") )
						textBoxICMountpoint.Text = textBoxICMountpoint.Text.Substring(0, textBoxICMountpoint.Text.Length-4);
				}
			}
		}

		private void radioButtonAAC_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButtonAAC.Checked)
			{
				if (this.tabControlBroadCast.SelectedTab == this.tabPageWMAcast)
				{
					MessageBox.Show( this, "AAC Encoder can not be used with the WMAcast streaming server!" );
					this.radioButtonWMA.Checked = true;
				}
				else if (this.tabControlBroadCast.SelectedTab == this.tabPageICEcast)
				{
					if ( textBoxICMountpoint.Text.EndsWith(".ogg") )
						textBoxICMountpoint.Text = textBoxICMountpoint.Text.Substring(0, textBoxICMountpoint.Text.Length-4);
				}
			}
		}

		private void radioButtonOGG_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButtonOGG.Checked)
			{
				if (this.tabControlBroadCast.SelectedTab == this.tabPageSHOUTcast)
				{
					MessageBox.Show( this, "OGG Encoder can not be used with the SHOUTcast streaming server!" );
					this.radioButtonLAME.Checked = true;
				}
				else if (this.tabControlBroadCast.SelectedTab == this.tabPageWMAcast)
				{
					MessageBox.Show( this, "OGG Encoder can not be used with the WMAcast streaming server!" );
					this.radioButtonWMA.Checked = true;
				}
				else if (this.tabControlBroadCast.SelectedTab == this.tabPageICEcast)
				{
					if ( !textBoxICMountpoint.Text.EndsWith(".ogg") )
						textBoxICMountpoint.Text += ".ogg";
				}
			}
		}

		private void radioButtonWMA_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButtonWMA.Checked)
			{
				if (this.tabControlBroadCast.SelectedTab != this.tabPageWMAcast)
				{
					MessageBox.Show( this, "WMA Encoder can not be used with the SHOUTcast or ICEcast streaming server!" );
					this.radioButtonLAME.Checked = true;
				}
			}
		}

		private void radioButtonLAME_Click(object sender, System.EventArgs e)
		{
			this.labelHint.Text = "Make sure, that \"lame.exe\" exists in your executable directory!";
		}

		private void radioButtonAAC_Click(object sender, System.EventArgs e)
		{
			this.labelHint.Text = "Make sure, that \"enc_aacPlus.exe\" (and \"enc_aacPlus.dll\" and \"nscrt.dd\"  - both can be found in your Winamp/plugin directoy) exists in your executable directory!";
		}

		private void radioButtonOGG_Click(object sender, System.EventArgs e)
		{
			this.labelHint.Text = "Make sure, that \"oggenc2.exe\" exists in your executable directory!";
		}

		private void radioButtonWMA_Click(object sender, System.EventArgs e)
		{
			this.labelHint.Text = "Make sure, that \"basswma.dll\" exists in your executable directory!";
		}

		private void checkBoxAutoReconnect_CheckedChanged(object sender, System.EventArgs e)
		{
			if (_broadCast != null)
			{
				_broadCast.AutoReconnect = checkBoxAutoReconnect.Checked;
			}
		}
	}
}
