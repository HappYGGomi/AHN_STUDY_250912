using System;
using System.IO;
using System.Windows.Forms;

namespace DocumentDecryptor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // 간단한 테스트 메시지
                MessageBox.Show("DocumentDecryptor 프로그램이 시작되었습니다!", "시작", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // GUI 모드로 실행
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 실행 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}