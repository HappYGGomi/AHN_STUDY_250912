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
                // 명령행 인수가 있으면 파일 복호화 실행
                if (args.Length > 0)
                {
                    string filePath = args[0];
                    
                    // 파일이 존재하는지 확인
                    if (File.Exists(filePath))
                    {
                        // 복호화 실행
                        bool success = DocumentDecryptor.DecryptDocument(filePath);
                        
                        if (success)
                        {
                            Console.WriteLine("복호화가 완료되었습니다.");
                        }
                        else
                        {
                            Console.WriteLine("복호화에 실패했습니다.");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"파일을 찾을 수 없습니다: {filePath}");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    // GUI 모드로 실행
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 실행 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
    }
}
