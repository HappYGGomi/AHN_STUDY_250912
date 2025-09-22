using System;
using System.IO;
using System.Windows.Forms;

namespace DocumentDecryptor
{
    public partial class MainForm : Form
    {
        private Button btnSelectFile;
        private Button btnDecrypt;
        private TextBox txtFilePath;
        private Label lblStatus;
        private ProgressBar progressBar;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // 폼 설정
            this.Text = "문서 복호화 도구";
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 파일 선택 버튼
            btnSelectFile = new Button();
            btnSelectFile.Text = "파일 선택";
            btnSelectFile.Location = new System.Drawing.Point(20, 20);
            btnSelectFile.Size = new System.Drawing.Size(100, 30);
            btnSelectFile.Click += BtnSelectFile_Click;

            // 파일 경로 텍스트박스
            txtFilePath = new TextBox();
            txtFilePath.Location = new System.Drawing.Point(130, 20);
            txtFilePath.Size = new System.Drawing.Size(320, 30);
            txtFilePath.ReadOnly = true;

            // 복호화 버튼
            btnDecrypt = new Button();
            btnDecrypt.Text = "복호화 실행";
            btnDecrypt.Location = new System.Drawing.Point(20, 70);
            btnDecrypt.Size = new System.Drawing.Size(100, 40);
            btnDecrypt.Enabled = false;
            btnDecrypt.Click += BtnDecrypt_Click;

            // 상태 레이블
            lblStatus = new Label();
            lblStatus.Text = "복호화할 파일을 선택하세요.";
            lblStatus.Location = new System.Drawing.Point(20, 130);
            lblStatus.Size = new System.Drawing.Size(430, 20);

            // 진행률 표시바
            progressBar = new ProgressBar();
            progressBar.Location = new System.Drawing.Point(20, 160);
            progressBar.Size = new System.Drawing.Size(430, 20);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;

            // 컨트롤 추가
            this.Controls.Add(btnSelectFile);
            this.Controls.Add(txtFilePath);
            this.Controls.Add(btnDecrypt);
            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);

            this.ResumeLayout(false);
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "모든 파일 (*.*)|*.*|암호화된 파일 (*.enc)|*.enc|텍스트 파일 (*.txt)|*.txt";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    btnDecrypt.Enabled = true;
                    lblStatus.Text = "선택된 파일: " + Path.GetFileName(openFileDialog.FileName);
                }
            }
        }

        private async void BtnDecrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
            {
                MessageBox.Show("유효한 파일을 선택하세요.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // UI 비활성화
            btnDecrypt.Enabled = false;
            btnSelectFile.Enabled = false;
            progressBar.Visible = true;
            lblStatus.Text = "복호화 중...";

            try
            {
                // 비동기로 복호화 실행
                bool success = await System.Threading.Tasks.Task.Run(() => 
                    DocumentDecryptor.DecryptDocument(txtFilePath.Text));

                if (success)
                {
                    lblStatus.Text = "복호화가 완료되었습니다.";
                    MessageBox.Show("복호화가 성공적으로 완료되었습니다.", "완료", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "복호화에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "오류가 발생했습니다.";
                MessageBox.Show($"복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // UI 활성화
                btnDecrypt.Enabled = true;
                btnSelectFile.Enabled = true;
                progressBar.Visible = false;
            }
        }
    }
}
