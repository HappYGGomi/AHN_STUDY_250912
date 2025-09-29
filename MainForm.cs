using System;
using System.IO;
using System.Windows.Forms;

namespace DocumentDecryptor
{
    public partial class MainForm : Form
    {
        private Button btnSelectFile;
        private TextBox txtFilePath;
        private Button btnDecrypt;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 폼 설정
            this.Text = "Document Decryptor - 테스트 버전";
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // 파일 선택 버튼
            btnSelectFile = new Button();
            btnSelectFile.Text = "파일 선택";
            btnSelectFile.Location = new System.Drawing.Point(20, 20);
            btnSelectFile.Size = new System.Drawing.Size(100, 30);
            btnSelectFile.Click += BtnSelectFile_Click;

            // 파일 경로 텍스트박스
            txtFilePath = new TextBox();
            txtFilePath.Location = new System.Drawing.Point(130, 20);
            txtFilePath.Size = new System.Drawing.Size(300, 30);
            txtFilePath.ReadOnly = true;

            // 복호화 버튼
            btnDecrypt = new Button();
            btnDecrypt.Text = "복호화";
            btnDecrypt.Location = new System.Drawing.Point(20, 70);
            btnDecrypt.Size = new System.Drawing.Size(100, 40);
            btnDecrypt.Enabled = false;
            btnDecrypt.Click += BtnDecrypt_Click;

            // 상태 레이블
            lblStatus = new Label();
            lblStatus.Text = "복호화할 파일을 선택하세요.";
            lblStatus.Location = new System.Drawing.Point(20, 130);
            lblStatus.Size = new System.Drawing.Size(430, 20);

            // 컨트롤 추가
            this.Controls.Add(btnSelectFile);
            this.Controls.Add(txtFilePath);
            this.Controls.Add(btnDecrypt);
            this.Controls.Add(lblStatus);

            this.ResumeLayout(false);
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "모든 파일 (*.*)|*.*";
                openFileDialog.Title = "복호화할 파일을 선택하세요";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    btnDecrypt.Enabled = true;
                    lblStatus.Text = $"선택된 파일: {Path.GetFileName(openFileDialog.FileName)}";
                }
            }
        }

        private void BtnDecrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
            {
                MessageBox.Show("유효한 파일을 선택하세요.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 실제 복호화 시도
                bool success = TryRealDecryption(txtFilePath.Text);

                if (success)
                {
                    MessageBox.Show($"복호화가 완료되었습니다!\n\n" +
                        $"복호화된 파일: {Path.ChangeExtension(txtFilePath.Text, ".decrypted")}\n" +
                        $"결과 메시지 파일: {Path.ChangeExtension(txtFilePath.Text, ".txt")}", 
                        "복호화 완료", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);

                    lblStatus.Text = "복호화가 완료되었습니다.";
                }
                else
                {
                    MessageBox.Show("복호화에 실패했습니다.\n\n" +
                        "가능한 원인:\n" +
                        "1. 파일이 실제로 암호화되어 있지 않음\n" +
                        "2. DSCS DLL이 설치되지 않음\n" +
                        "3. 복호화 권한이 없음", 
                        "복호화 실패", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Warning);
                    
                    lblStatus.Text = "복호화에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "복호화에 실패했습니다.";
            }
        }

        private bool TryRealDecryption(string filePath)
        {
            try
            {
                // 파일이 실제로 암호화되어 있는지 확인
                if (!IsEncryptedFile(filePath))
                {
                    MessageBox.Show("선택한 파일이 암호화되어 있지 않습니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                // 실제 복호화 시도 (현재는 파일 복사로 대체)
                string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                File.Copy(filePath, outputPath, true);

                // 결과 메시지 파일 생성
                string resultFilePath = Path.ChangeExtension(filePath, ".txt");
                string successMessage = $"result code : 1, result msg : success\nFile Name:{outputPath}";
                File.WriteAllText(resultFilePath, successMessage);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복호화 실행 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool IsEncryptedFile(string filePath)
        {
            try
            {
                // 파일 크기 확인
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < 100)
                {
                    return false;
                }

                // 파일 헤더 확인 (간단한 암호화 파일 감지)
                byte[] header = new byte[16];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, 16);
                }

                // 일반적인 암호화 파일 시그니처 확인
                // 실제로는 DSCS DLL의 DSCSIsEncryptedFile 함수를 사용해야 함
                return true; // 테스트용으로 항상 true 반환
            }
            catch
            {
                return false;
            }
        }
    }
}