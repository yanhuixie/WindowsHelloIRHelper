namespace WindowsHelloIRHelper
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitle = new Label();
            chkEnableLightSensor = new CheckBox();
            grpConfig = new GroupBox();
            lblDetectLight = new Label();
            btnDetectLight = new Button();
            lblCameraStatus = new Label();
            btnRefreshCameras = new Button();
            btnToggleCamera = new Button();
            cmbCameraList = new ComboBox();
            lblCameraList = new Label();
            btnSaveConfig = new Button();
            lblThreshold = new Label();
            btnResetConfig = new Button();
            numThreshold = new NumericUpDown();
            btnPreview = new Button();
            pictureBoxVideo = new PictureBox();
            btnShowHistory = new Button();
            btnShowSystemStatus = new Button();
            btnExportConfig = new Button();
            btnImportConfig = new Button();
            grpService = new GroupBox();
            lblServiceStatus = new Label();
            btnInstallService = new Button();
            btnUninstallService = new Button();
            btnStartService = new Button();
            btnStopService = new Button();
            grpStatus = new GroupBox();
            btnTest = new Button();
            txtStatus = new TextBox();
            btnClearStatus = new Button();
            groupBox1 = new GroupBox();
            pictureBox1 = new PictureBox();
            btnSwitchLanguage = new Button();
            grpConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVideo).BeginInit();
            grpService.SuspendLayout();
            grpStatus.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(31, 21);
            lblTitle.Margin = new Padding(5, 0, 5, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(368, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Windows Hello IR Helper";
            // 
            // chkEnableLightSensor
            // 
            chkEnableLightSensor.AutoSize = true;
            chkEnableLightSensor.Location = new Point(24, 79);
            chkEnableLightSensor.Margin = new Padding(5, 4, 5, 4);
            chkEnableLightSensor.Name = "chkEnableLightSensor";
            chkEnableLightSensor.Size = new Size(295, 28);
            chkEnableLightSensor.TabIndex = 1;
            chkEnableLightSensor.Text = Properties.Resources.UI_Form1_CheckEnableLightSensor;
            chkEnableLightSensor.UseVisualStyleBackColor = true;
            chkEnableLightSensor.CheckedChanged += chkEnableLightSensor_CheckedChanged;
            // 
            // grpConfig
            // 
            grpConfig.Controls.Add(lblDetectLight);
            grpConfig.Controls.Add(btnDetectLight);
            grpConfig.Controls.Add(lblCameraStatus);
            grpConfig.Controls.Add(btnRefreshCameras);
            grpConfig.Controls.Add(btnToggleCamera);
            grpConfig.Controls.Add(cmbCameraList);
            grpConfig.Controls.Add(lblCameraList);
            grpConfig.Controls.Add(chkEnableLightSensor);
            grpConfig.Controls.Add(btnSaveConfig);
            grpConfig.Controls.Add(lblThreshold);
            grpConfig.Controls.Add(btnResetConfig);
            grpConfig.Controls.Add(numThreshold);
            grpConfig.Font = new Font("Microsoft YaHei UI", 9F);
            grpConfig.Location = new Point(31, 71);
            grpConfig.Margin = new Padding(5, 4, 5, 4);
            grpConfig.Name = "grpConfig";
            grpConfig.Padding = new Padding(5, 4, 5, 4);
            grpConfig.Size = new Size(1102, 169);
            grpConfig.TabIndex = 2;
            grpConfig.TabStop = false;
            grpConfig.Text = "Configuration";
            // 
            // lblDetectLight
            // 
            lblDetectLight.Location = new Point(927, 80);
            lblDetectLight.Name = "lblDetectLight";
            lblDetectLight.Size = new Size(105, 25);
            lblDetectLight.TabIndex = 1;
            // 
            // btnDetectLight
            // 
            btnDetectLight.Font = new Font("Microsoft YaHei UI", 9F);
            btnDetectLight.Location = new Point(747, 75);
            btnDetectLight.Margin = new Padding(5, 4, 5, 4);
            btnDetectLight.Name = "btnDetectLight";
            btnDetectLight.Size = new Size(168, 35);
            btnDetectLight.TabIndex = 0;
            btnDetectLight.Text = Properties.Resources.UI_Form1_ButtonDetectLight;
            btnDetectLight.UseVisualStyleBackColor = true;
            btnDetectLight.Click += btnDetectLight_Click;
            // 
            // lblCameraStatus
            // 
            lblCameraStatus.AutoSize = true;
            lblCameraStatus.Location = new Point(571, 36);
            lblCameraStatus.Margin = new Padding(5, 0, 5, 0);
            lblCameraStatus.Name = "lblCameraStatus";
            lblCameraStatus.Size = new Size(153, 24);
            lblCameraStatus.TabIndex = 3;
            lblCameraStatus.Text = "Status: Unknown";
            // 
            // btnRefreshCameras
            // 
            btnRefreshCameras.Location = new Point(426, 31);
            btnRefreshCameras.Margin = new Padding(5, 4, 5, 4);
            btnRefreshCameras.Name = "btnRefreshCameras";
            btnRefreshCameras.Size = new Size(126, 34);
            btnRefreshCameras.TabIndex = 2;
            btnRefreshCameras.Text = Properties.Resources.UI_Form1_ButtonRefreshCameras;
            btnRefreshCameras.UseVisualStyleBackColor = true;
            btnRefreshCameras.Click += btnRefreshCameras_Click;
            // 
            // btnToggleCamera
            // 
            btnToggleCamera.Location = new Point(747, 31);
            btnToggleCamera.Margin = new Padding(5, 4, 5, 4);
            btnToggleCamera.Name = "btnToggleCamera";
            btnToggleCamera.Size = new Size(168, 35);
            btnToggleCamera.TabIndex = 4;
            btnToggleCamera.Text = Properties.Resources.UI_Form1_ButtonToggleCamera;
            btnToggleCamera.UseVisualStyleBackColor = true;
            btnToggleCamera.Click += btnToggleCamera_Click;
            // 
            // cmbCameraList
            // 
            cmbCameraList.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCameraList.Location = new Point(167, 32);
            cmbCameraList.Margin = new Padding(5, 4, 5, 4);
            cmbCameraList.Name = "cmbCameraList";
            cmbCameraList.Size = new Size(249, 32);
            cmbCameraList.TabIndex = 1;
            cmbCameraList.SelectedIndexChanged += cmbCameraList_SelectedIndexChanged;
            // 
            // lblCameraList
            // 
            lblCameraList.AutoSize = true;
            lblCameraList.Location = new Point(21, 36);
            lblCameraList.Margin = new Padding(5, 0, 5, 0);
            lblCameraList.Name = "lblCameraList";
            lblCameraList.Size = new Size(136, 24);
            lblCameraList.TabIndex = 0;
            lblCameraList.Text = "Select Camera:";
            // 
            // btnSaveConfig
            // 
            btnSaveConfig.Location = new Point(21, 123);
            btnSaveConfig.Margin = new Padding(5, 4, 5, 4);
            btnSaveConfig.Name = "btnSaveConfig";
            btnSaveConfig.Size = new Size(126, 35);
            btnSaveConfig.TabIndex = 4;
            btnSaveConfig.Text = Properties.Resources.UI_Form1_ButtonSaveConfig;
            btnSaveConfig.UseVisualStyleBackColor = true;
            btnSaveConfig.Click += btnSaveConfig_Click;
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Location = new Point(362, 81);
            lblThreshold.Margin = new Padding(5, 0, 5, 0);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(190, 24);
            lblThreshold.TabIndex = 0;
            lblThreshold.Text = "Light Threshold (lux):";
            // 
            // btnResetConfig
            // 
            btnResetConfig.Location = new Point(163, 123);
            btnResetConfig.Margin = new Padding(5, 4, 5, 4);
            btnResetConfig.Name = "btnResetConfig";
            btnResetConfig.Size = new Size(126, 35);
            btnResetConfig.TabIndex = 5;
            btnResetConfig.Text = Properties.Resources.UI_Form1_ButtonResetConfig;
            btnResetConfig.UseVisualStyleBackColor = true;
            btnResetConfig.Click += btnResetConfig_Click;
            // 
            // numThreshold
            // 
            numThreshold.DecimalPlaces = 1;
            numThreshold.Location = new Point(571, 78);
            numThreshold.Margin = new Padding(5, 4, 5, 4);
            numThreshold.Maximum = new decimal(new int[] { 50000, 0, 0, 0 });
            numThreshold.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            numThreshold.Name = "numThreshold";
            numThreshold.Size = new Size(126, 30);
            numThreshold.TabIndex = 1;
            numThreshold.Value = new decimal(new int[] { 100, 0, 0, 65536 });
            // 
            // btnPreview
            // 
            btnPreview.Font = new Font("Microsoft YaHei UI", 9F);
            btnPreview.Location = new Point(726, 296);
            btnPreview.Margin = new Padding(5, 4, 5, 4);
            btnPreview.Name = "btnPreview";
            btnPreview.Size = new Size(168, 35);
            btnPreview.TabIndex = 7;
            btnPreview.Text = Properties.Resources.UI_Form1_ButtonPreview;
            btnPreview.UseVisualStyleBackColor = true;
            btnPreview.Click += btnPreview_Click;
            // 
            // pictureBoxVideo
            // 
            pictureBoxVideo.ErrorImage = Properties.Resources.preview;
            pictureBoxVideo.Image = Properties.Resources.preview;
            pictureBoxVideo.InitialImage = Properties.Resources.preview;
            pictureBoxVideo.Location = new Point(902, 247);
            pictureBoxVideo.Name = "pictureBoxVideo";
            pictureBoxVideo.Size = new Size(232, 131);
            pictureBoxVideo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxVideo.TabIndex = 6;
            pictureBoxVideo.TabStop = false;
            // 
            // btnShowHistory
            // 
            btnShowHistory.Location = new Point(165, 241);
            btnShowHistory.Margin = new Padding(5, 4, 5, 4);
            btnShowHistory.Name = "btnShowHistory";
            btnShowHistory.Size = new Size(126, 35);
            btnShowHistory.TabIndex = 2;
            btnShowHistory.Text = Properties.Resources.UI_Form1_ButtonShowHistory;
            btnShowHistory.UseVisualStyleBackColor = true;
            btnShowHistory.Click += btnShowHistory_Click;
            // 
            // btnShowSystemStatus
            // 
            btnShowSystemStatus.Location = new Point(306, 241);
            btnShowSystemStatus.Margin = new Padding(5, 4, 5, 4);
            btnShowSystemStatus.Name = "btnShowSystemStatus";
            btnShowSystemStatus.Size = new Size(126, 35);
            btnShowSystemStatus.TabIndex = 3;
            btnShowSystemStatus.Text = Properties.Resources.UI_Form1_ButtonShowSystemStatus;
            btnShowSystemStatus.UseVisualStyleBackColor = true;
            btnShowSystemStatus.Click += btnShowSystemStatus_Click;
            // 
            // btnExportConfig
            // 
            btnExportConfig.Location = new Point(448, 241);
            btnExportConfig.Margin = new Padding(5, 4, 5, 4);
            btnExportConfig.Name = "btnExportConfig";
            btnExportConfig.Size = new Size(126, 35);
            btnExportConfig.TabIndex = 4;
            btnExportConfig.Text = Properties.Resources.UI_Form1_ButtonExportConfig;
            btnExportConfig.UseVisualStyleBackColor = true;
            btnExportConfig.Click += btnExportConfig_Click;
            // 
            // btnImportConfig
            // 
            btnImportConfig.Location = new Point(589, 241);
            btnImportConfig.Margin = new Padding(5, 4, 5, 4);
            btnImportConfig.Name = "btnImportConfig";
            btnImportConfig.Size = new Size(126, 35);
            btnImportConfig.TabIndex = 5;
            btnImportConfig.Text = Properties.Resources.UI_Form1_ButtonImportConfig;
            btnImportConfig.UseVisualStyleBackColor = true;
            btnImportConfig.Click += btnImportConfig_Click;
            // 
            // grpService
            // 
            grpService.Controls.Add(lblServiceStatus);
            grpService.Controls.Add(btnInstallService);
            grpService.Controls.Add(btnUninstallService);
            grpService.Controls.Add(btnStartService);
            grpService.Controls.Add(btnStopService);
            grpService.Font = new Font("Microsoft YaHei UI", 9F);
            grpService.Location = new Point(31, 261);
            grpService.Margin = new Padding(5, 4, 5, 4);
            grpService.Name = "grpService";
            grpService.Padding = new Padding(5, 4, 5, 4);
            grpService.Size = new Size(632, 113);
            grpService.TabIndex = 4;
            grpService.TabStop = false;
            grpService.Text = "Service Management";
            // 
            // lblServiceStatus
            // 
            lblServiceStatus.AutoSize = true;
            lblServiceStatus.Location = new Point(24, 35);
            lblServiceStatus.Margin = new Padding(5, 0, 5, 0);
            lblServiceStatus.Name = "lblServiceStatus";
            lblServiceStatus.Size = new Size(218, 24);
            lblServiceStatus.TabIndex = 0;
            lblServiceStatus.Text = "Service Status: Unknown";
            // 
            // btnInstallService
            // 
            btnInstallService.Location = new Point(24, 64);
            btnInstallService.Margin = new Padding(5, 4, 5, 4);
            btnInstallService.Name = "btnInstallService";
            btnInstallService.Size = new Size(126, 35);
            btnInstallService.TabIndex = 1;
            btnInstallService.Text = Properties.Resources.UI_Form1_ButtonInstallService;
            btnInstallService.UseVisualStyleBackColor = true;
            btnInstallService.Click += btnInstallService_Click;
            // 
            // btnUninstallService
            // 
            btnUninstallService.Location = new Point(165, 64);
            btnUninstallService.Margin = new Padding(5, 4, 5, 4);
            btnUninstallService.Name = "btnUninstallService";
            btnUninstallService.Size = new Size(126, 35);
            btnUninstallService.TabIndex = 2;
            btnUninstallService.Text = Properties.Resources.UI_Form1_ButtonUninstallService;
            btnUninstallService.UseVisualStyleBackColor = true;
            btnUninstallService.Click += btnUninstallService_Click;
            // 
            // btnStartService
            // 
            btnStartService.Location = new Point(306, 64);
            btnStartService.Margin = new Padding(5, 4, 5, 4);
            btnStartService.Name = "btnStartService";
            btnStartService.Size = new Size(126, 35);
            btnStartService.TabIndex = 3;
            btnStartService.Text = Properties.Resources.UI_Form1_ButtonStartService;
            btnStartService.UseVisualStyleBackColor = true;
            btnStartService.Click += btnStartService_Click;
            // 
            // btnStopService
            // 
            btnStopService.Location = new Point(448, 64);
            btnStopService.Margin = new Padding(5, 4, 5, 4);
            btnStopService.Name = "btnStopService";
            btnStopService.Size = new Size(126, 35);
            btnStopService.TabIndex = 4;
            btnStopService.Text = Properties.Resources.UI_Form1_ButtonStopService;
            btnStopService.UseVisualStyleBackColor = true;
            btnStopService.Click += btnStopService_Click;
            // 
            // grpStatus
            // 
            grpStatus.Controls.Add(btnTest);
            grpStatus.Controls.Add(txtStatus);
            grpStatus.Controls.Add(btnClearStatus);
            grpStatus.Controls.Add(btnShowHistory);
            grpStatus.Controls.Add(btnShowSystemStatus);
            grpStatus.Controls.Add(btnExportConfig);
            grpStatus.Controls.Add(btnImportConfig);
            grpStatus.Font = new Font("Microsoft YaHei UI", 9F);
            grpStatus.Location = new Point(31, 391);
            grpStatus.Margin = new Padding(5, 4, 5, 4);
            grpStatus.Name = "grpStatus";
            grpStatus.Padding = new Padding(5, 4, 5, 4);
            grpStatus.Size = new Size(856, 282);
            grpStatus.TabIndex = 6;
            grpStatus.TabStop = false;
            grpStatus.Text = "Status Information";
            // 
            // btnTest
            // 
            btnTest.Location = new Point(726, 241);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(112, 34);
            btnTest.TabIndex = 6;
            btnTest.Text = "测试SC命令";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Visible = false;
            btnTest.Click += btnTest_Click;
            // 
            // txtStatus
            // 
            txtStatus.Font = new Font("Microsoft YaHei UI", 9F);
            txtStatus.Location = new Point(24, 35);
            txtStatus.Margin = new Padding(5, 4, 5, 4);
            txtStatus.Multiline = true;
            txtStatus.Name = "txtStatus";
            txtStatus.ReadOnly = true;
            txtStatus.ScrollBars = ScrollBars.Vertical;
            txtStatus.Size = new Size(814, 196);
            txtStatus.TabIndex = 0;
            // 
            // btnClearStatus
            // 
            btnClearStatus.Location = new Point(24, 241);
            btnClearStatus.Margin = new Padding(5, 4, 5, 4);
            btnClearStatus.Name = "btnClearStatus";
            btnClearStatus.Size = new Size(126, 35);
            btnClearStatus.TabIndex = 1;
            btnClearStatus.Text = Properties.Resources.UI_Form1_ButtonClearStatus;
            btnClearStatus.UseVisualStyleBackColor = true;
            btnClearStatus.Click += btnClearStatus_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(pictureBox1);
            groupBox1.Location = new Point(902, 391);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(231, 282);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            groupBox1.Text = "Get Help";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.gongzhonghao;
            pictureBox1.Location = new Point(16, 45);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(194, 198);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // btnSwitchLanguage
            // 
            btnSwitchLanguage.Font = new Font("Microsoft YaHei UI", 9F);
            btnSwitchLanguage.Location = new Point(966, 21);
            btnSwitchLanguage.Margin = new Padding(5, 4, 5, 4);
            btnSwitchLanguage.Name = "btnSwitchLanguage";
            btnSwitchLanguage.Size = new Size(167, 35);
            btnSwitchLanguage.TabIndex = 8;
            btnSwitchLanguage.Text = "Switch Language";
            btnSwitchLanguage.UseVisualStyleBackColor = true;
            btnSwitchLanguage.Click += btnSwitchLanguage_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1168, 694);
            Controls.Add(btnPreview);
            Controls.Add(btnSwitchLanguage);
            Controls.Add(pictureBoxVideo);
            Controls.Add(groupBox1);
            Controls.Add(grpStatus);
            Controls.Add(grpService);
            Controls.Add(grpConfig);
            Controls.Add(lblTitle);
            Margin = new Padding(5, 4, 5, 4);
            MaximumSize = new Size(1190, 750);
            MinimumSize = new Size(1190, 750);
            Name = "Form1";
            Text = "Windows Hello IR Helper - Auto Camera Control";
            Load += Form1_Load;
            grpConfig.ResumeLayout(false);
            grpConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVideo).EndInit();
            grpService.ResumeLayout(false);
            grpService.PerformLayout();
            grpStatus.ResumeLayout(false);
            grpStatus.PerformLayout();
            groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // 主界面控件
        private Label lblTitle;
        private CheckBox chkEnableLightSensor;
        
        // 参数配置区域
        private GroupBox grpConfig;
        private Label lblThreshold;
        private NumericUpDown numThreshold;
        private Button btnSaveConfig;
        private Button btnResetConfig;
        private Label lblCameraList;
        private ComboBox cmbCameraList;
        private Button btnRefreshCameras;
        private Label lblCameraStatus;
        private Button btnToggleCamera;
        
        // 服务管理区域
        private GroupBox grpService;
        private Label lblServiceStatus;
        private Button btnInstallService;
        private Button btnUninstallService;
        private Button btnStartService;
        private Button btnStopService;
        
        // 状态显示区域
        private GroupBox grpStatus;
        private TextBox txtStatus;
        private Button btnClearStatus;
        private Button btnShowHistory;
        private Button btnShowSystemStatus;
        private Button btnExportConfig;
        private Button btnImportConfig;
        private Button btnDetectLight;
        private Label lblDetectLight;
        private Button btnTest;
        private PictureBox pictureBoxVideo;
        private Button btnPreview;
        private GroupBox groupBox1;
        private PictureBox pictureBox1;
        private Button btnSwitchLanguage;
    }
}