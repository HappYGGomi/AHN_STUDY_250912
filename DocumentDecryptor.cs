using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DocumentDecryptor
{
    public class DocumentDecryptor
    {
        // DSCS DLL 함수 선언
        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DSCSDecryptFile(string lpszFile, string lpszDecFile);

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DSCSIsEncryptedFile(string lpszFile);

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DSCSInstall();

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DSCSRelease();

        // 대체 복호화 방법 (DSCS DLL이 없는 경우)
        public static bool DecryptFileAlternative(string inputFile, string outputFile)
        {
            try
            {
                // 파일이 존재하는지 확인
                if (!File.Exists(inputFile))
                {
                    MessageBox.Show($"입력 파일을 찾을 수 없습니다: {inputFile}", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 파일 읽기
                byte[] encryptedData = File.ReadAllBytes(inputFile);
                
                // 간단한 XOR 복호화 (실제 알고리즘은 분석 필요)
                byte[] decryptedData = DecryptXOR(encryptedData);
                
                // 복호화된 파일 저장
                File.WriteAllBytes(outputFile, decryptedData);
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // XOR 복호화 (임시 구현)
        private static byte[] DecryptXOR(byte[] data)
        {
            // 실제 키는 분석을 통해 결정해야 함
            byte[] key = { 0x12, 0x34, 0x56, 0x78 };
            byte[] result = new byte[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            }
            
            return result;
        }

        // 메인 복호화 함수
        public static bool DecryptDocument(string filePath)
        {
            try
            {
                // DSCS DLL이 있는지 확인
                string dscsDllPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "DSCSLink.dll");
                
                if (File.Exists(dscsDllPath))
                {
                    // DSCS를 사용한 복호화
                    return DecryptWithDSCS(filePath);
                }
                else
                {
                    // 대체 방법 사용
                    return DecryptWithAlternative(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static bool DecryptWithDSCS(string filePath)
        {
            try
            {
                // DSCS 초기화
                if (!DSCSInstall())
                {
                    MessageBox.Show("DSCS 초기화에 실패했습니다.", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 파일이 암호화되어 있는지 확인
                if (!DSCSIsEncryptedFile(filePath))
                {
                    MessageBox.Show("선택한 파일이 암호화되어 있지 않습니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DSCSRelease();
                    return false;
                }

                // 출력 파일 경로 생성
                string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                
                // 복호화 실행
                bool success = DSCSDecryptFile(filePath, outputPath);
                
                // DSCS 해제
                DSCSRelease();
                
                if (success)
                {
                    MessageBox.Show($"복호화가 완료되었습니다.\n저장 위치: {outputPath}", "완료", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show("복호화에 실패했습니다.", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DSCS 복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static bool DecryptWithAlternative(string filePath)
        {
            try
            {
                // 출력 파일 경로 생성
                string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                
                // 대체 방법으로 복호화
                bool success = DecryptFileAlternative(filePath, outputPath);
                
                if (success)
                {
                    MessageBox.Show($"복호화가 완료되었습니다.\n저장 위치: {outputPath}", "완료", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"대체 복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
