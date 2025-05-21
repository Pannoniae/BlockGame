using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Cd;

namespace Sample
{
	// NOTE: Needs 'bass.dll' - copy it to your output directory first!

	public class CreateFileUser : System.Windows.Forms.Form
	{
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button buttonSelectFile;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button buttonStreamCreateUser;
		private System.Windows.Forms.Button buttonStreamCreate;

		private System.ComponentModel.Container components = null;

		public CreateFileUser()
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
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.buttonSelectFile = new System.Windows.Forms.Button();
            this.buttonStreamCreate = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.buttonStreamCreateUser = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Audio Files (*.wav;*.mp3;*.ogg)|*.wav;*.mp3;*.ogg";
            this.openFileDialog.Title = "Select an audio file to play";
            // 
            // buttonSelectFile
            // 
            this.buttonSelectFile.Location = new System.Drawing.Point(16, 14);
            this.buttonSelectFile.Name = "buttonSelectFile";
            this.buttonSelectFile.Size = new System.Drawing.Size(260, 23);
            this.buttonSelectFile.TabIndex = 1;
            this.buttonSelectFile.Text = "Select a file to play (WAV)...";
            this.buttonSelectFile.Click += new System.EventHandler(this.buttonSelectFile_Click);
            // 
            // buttonStreamCreate
            // 
            this.buttonStreamCreate.Location = new System.Drawing.Point(180, 52);
            this.buttonStreamCreate.Name = "buttonStreamCreate";
            this.buttonStreamCreate.Size = new System.Drawing.Size(94, 23);
            this.buttonStreamCreate.TabIndex = 6;
            this.buttonStreamCreate.Text = "StreamCreate";
            this.buttonStreamCreate.Click += new System.EventHandler(this.buttonStreamCreate_Click);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(20, 90);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(256, 166);
            this.textBox1.TabIndex = 7;
            this.textBox1.WordWrap = false;
            // 
            // buttonStreamCreateUser
            // 
            this.buttonStreamCreateUser.Location = new System.Drawing.Point(18, 52);
            this.buttonStreamCreateUser.Name = "buttonStreamCreateUser";
            this.buttonStreamCreateUser.Size = new System.Drawing.Size(112, 23);
            this.buttonStreamCreateUser.TabIndex = 8;
            this.buttonStreamCreateUser.Text = "StreamCreateUser";
            this.buttonStreamCreateUser.Click += new System.EventHandler(this.buttonStreamCreateUser_Click);
            // 
            // CreateFileUser
            // 
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.buttonStreamCreateUser);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.buttonStreamCreate);
            this.Controls.Add(this.buttonSelectFile);
            this.Name = "CreateFileUser";
            this.Text = "CreateFileUser Test";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.CreateFileUser_Closing);
            this.Load += new System.EventHandler(this.CreateFileUser_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		[STAThread]
		static void Main() 
		{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new CreateFileUser());
		}

		// LOCAL VARS
		private int _stream = 0;
		private string _fileName = String.Empty;
		private STREAMPROC _myStreamCreate;
		private BASS_FILEPROCS _myStreamCreateUser;
		private FileStream _fs;

		private void CreateFileUser_Load(object sender, System.EventArgs e)
		{
			//BassNet.Registration("your email", "your regkey");

			if ( Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, this.Handle) )
			{
				// all fine
				// we alraedy create the user callback methods...
				_myStreamCreate = new STREAMPROC(MyFileProc);

                _myStreamCreateUser = new BASS_FILEPROCS(
                    new FILECLOSEPROC(MyFileProcUserClose), 
                    new FILELENPROC(MyFileProcUserLength), 
                    new FILEREADPROC(MyFileProcUserRead), 
                    new FILESEEKPROC(MyFileProcUserSeek));
			}
			else
				MessageBox.Show(this, "Bass_Init error!" );
		}

		private void CreateFileUser_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// close bass
			Bass.BASS_Stop();
			Bass.BASS_Free();
		}

		private void buttonSelectFile_Click(object sender, System.EventArgs e)
		{
			this.openFileDialog.FileName = _fileName;
			if ( DialogResult.OK == this.openFileDialog.ShowDialog(this) )
			{
				if ( File.Exists(this.openFileDialog.FileName) )
					_fileName = this.openFileDialog.FileName;
				else
					_fileName = String.Empty;
			}
		}

        private void buttonStreamCreateUser_Click(object sender, System.EventArgs e)
		{
            Bass.BASS_StreamFree(_stream);
			this.textBox1.Text = "";
			
            // test BASS_StreamCreateUser
			if (_fileName != String.Empty)
			{
				// open the file...
				_fs = File.OpenRead(_fileName);
				this.textBox1.Text += "Open file: "+_fs.Name+Environment.NewLine;
				
				// create the stream
                // Note: The BASS_STREAM_PRESCAN flag is only used to demo what is happening when set.
                //       As it generates a lot of output at the beginning you might remove it.
				_stream = Bass.BASS_StreamCreateFileUser(BASSStreamSystem.STREAMFILE_NOBUFFER, BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_AUTOFREE, _myStreamCreateUser, IntPtr.Zero);
				this.textBox1.Text += "StreamCreate: "+_stream.ToString()+Environment.NewLine;
				// play the stream
				if (_stream != 0 && Bass.BASS_ChannelPlay(_stream, false) )
				{
					this.textBox1.Text += "ChannelPlay: playing"+Environment.NewLine;
				}
				else
				{
                    this.textBox1.Text += String.Format("Error: {0}\n", Bass.BASS_ErrorGetCode());
				}
			}
		}

        private void buttonStreamCreate_Click(object sender, System.EventArgs e)
		{
			Bass.BASS_StreamFree(_stream);
			this.textBox1.Text = "";
			
            // test BASS_StreamCreate
			if (_fileName != String.Empty)
			{
				// open any file supported by BASS...
				_fs = File.OpenRead(_fileName);
				this.textBox1.Text += "Open file: "+_fs.Name+Environment.NewLine;
				
				// create the stream
				_stream = Bass.BASS_StreamCreate(44100, 2, BASSFlag.BASS_DEFAULT, _myStreamCreate, IntPtr.Zero);
				this.textBox1.Text += "StreamCreateUser: "+_stream.ToString()+Environment.NewLine;
				// play the stream
				if (_stream != 0 && Bass.BASS_ChannelPlay(_stream, false) )
				{
					this.textBox1.Text += "ChannelPlay: playing"+Environment.NewLine;
				}
				else
				{
                    this.textBox1.Text += String.Format("Error: {0}\n", Bass.BASS_ErrorGetCode());
				}
			}
		}

		private int MyFileProc(int handle, IntPtr buffer, int length, IntPtr user)
		{
			// implementing the callback for BASS_StreamCreate...
			// here we need to read the file...
			// at first we need to create a byte[] with the size of the requested length
			byte[] data = new byte[length];
			// read the file into data
			int bytesread = _fs.Read( data, 0, length );

			this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "BytesRead: "+bytesread.ToString()+Environment.NewLine });
			// you might instead also use "this.BeginInvoke(...)", which would call the delegate asynchron!

			// and now we need to copy the data to the given buffer...
			// meaning we need to copy the data into the unmanaged buffer...
			// we write as many bytes as we read via the file operation
			Marshal.Copy( data, 0, buffer, bytesread );
			// end of the file/stream?
			// we check this
			if ( bytesread < length )
			{
				bytesread |= (int)BASSStreamProc.BASS_STREAMPROC_END; // set indicator flag

				this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "EoF"+Environment.NewLine });

				_fs.Close();
			}
			
			return bytesread;
		}

        private void MyFileProcUserClose(IntPtr user)
        {
	        if (_fs == null)
		        return;

	        _fs.Close();

            this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "FileClose" + Environment.NewLine });
        }

        private long MyFileProcUserLength(IntPtr user)
        {
            if (_fs == null)
                return 0L;

            long len = _fs.Length;

            this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "FileLen: " + len.ToString() + Environment.NewLine });

            return len;
        }

        private int MyFileProcUserRead(IntPtr buffer, int length, IntPtr user)
        {
            if (_fs == null)
                return 0;

            try
            {
                // at first we need to create a byte[] with the size of the requested length
                byte[] data = new byte[length];
                // read the file into data
                int bytesread = _fs.Read(data, 0, length);

                this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "BytesRead: " + bytesread.ToString() + Environment.NewLine });

                // and now we need to copy the data to the buffer
                // we write as many bytes as we read via the file operation
                Marshal.Copy(data, 0, buffer, bytesread);

                return bytesread;
            }
            catch { return 0; }
        }

        private bool MyFileProcUserSeek(long offset, IntPtr user)
        {
            if (_fs == null)
                return false;

            // implementing the callback for BASS_StreamCreateUser...
            try
            {
                long pos = _fs.Seek(offset, SeekOrigin.Begin);

                this.Invoke(new UpdateMessageDelegate(UpdateMessageDisplay), new object[] { "SeekPos: " + pos.ToString() + Environment.NewLine });

                return true;
            }
            catch
            {
                return false;
            }
        }

		public delegate void UpdateMessageDelegate(string txt);
		private void UpdateMessageDisplay(string txt)
		{
			this.textBox1.Text += txt;
			this.textBox1.SelectionStart = this.textBox1.Text.Length;
			this.textBox1.ScrollToCaret();
		}

	}
}
