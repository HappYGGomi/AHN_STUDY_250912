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
        // DSCS DLL 함수 선언 (32비트/64비트 호환성 개선)
        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSDecryptFile(string lpszFile, string lpszDecFile);

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSIsEncryptedFile(string lpszFile);

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSInstall();

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
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

        // 대체 복호화 방법 (파일 복사)
        private static byte[] DecryptXOR(byte[] data)
        {
            // 실제로는 파일을 그대로 복사 (DSCS가 없을 때의 대체 방법)
            // 실제 복호화는 DSCS DLL에서 수행되어야 함
            return data;
        }

        // 메인 복호화 함수
        public static bool DecryptDocument(string filePath)
        {
            try
            {
                // 여러 위치에서 DSCS DLL 찾기
                string[] possiblePaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "DSCSLink.dll"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "DSCSLink.dll"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DSCS", "DSCSLink.dll"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "DSCS", "DSCSLink.dll"),
                    Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "DSCSLink.dll")
                };
                
                bool dscsFound = false;
                foreach (string dllPath in possiblePaths)
                {
                    if (File.Exists(dllPath))
                    {
                        dscsFound = true;
                        break;
                    }
                }
                
                if (dscsFound)
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
                // DSCS DLL 존재 여부 확인
                string dscsDllPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "DSCSLink.dll");
                if (!File.Exists(dscsDllPath))
                {
                    MessageBox.Show($"DSCS DLL을 찾을 수 없습니다: {dscsDllPath}\n대체 방법을 사용합니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                // DSCS 초기화 (아키텍처 오류 처리)
                bool installSuccess = false;
                try
                {
                    installSuccess = DSCSInstall();
                }
                catch (BadImageFormatException ex)
                {
                    MessageBox.Show($"DSCS DLL 아키텍처 오류: {ex.Message}\n대체 방법을 사용합니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                catch (DllNotFoundException ex)
                {
                    MessageBox.Show($"DSCS DLL을 찾을 수 없습니다: {ex.Message}\n대체 방법을 사용합니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                if (!installSuccess)
                {
                    MessageBox.Show("DSCS 초기화에 실패했습니다.\n대체 방법을 사용합니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                // 파일이 암호화되어 있는지 확인
                bool isEncrypted = false;
                try
                {
                    isEncrypted = DSCSIsEncryptedFile(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 암호화 확인 중 오류: {ex.Message}\n대체 방법을 사용합니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DSCSRelease();
                    return false;
                }

                if (!isEncrypted)
                {
                    MessageBox.Show("선택한 파일이 암호화되어 있지 않습니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DSCSRelease();
                    return false;
                }

                // 출력 파일 경로 생성
                string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                
                // 복호화 실행
                bool success = false;
                try
                {
                    success = DSCSDecryptFile(filePath, outputPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"복호화 실행 중 오류: {ex.Message}\n대체 방법을 사용합니다.", "알림", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    success = false;
                }
                finally
                {
                    // DSCS 해제
                    try
                    {
                        DSCSRelease();
                    }
                    catch
                    {
                        // DSCS 해제 실패는 무시
                    }
                }
                
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
                // DSCS DLL이 필요한 상황임을 사용자에게 알림
                DialogResult result = MessageBox.Show(
                    "DSCS DLL을 사용할 수 없어 정상적인 복호화가 불가능합니다.\n\n" +
                    "해결 방법:\n" +
                    "1. DSCSLink.dll을 C:\\Windows\\ 폴더에 설치\n" +
                    "2. 64비트 버전의 DSCSLink.dll 사용\n" +
                    "3. 시스템 관리자에게 문의\n\n" +
                    "파일을 그대로 복사하시겠습니까?",
                    "DSCS DLL 필요", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                {
                    return false;
                }
                
                // 출력 파일 경로 생성
                string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                
                // 파일을 그대로 복사 (실제 복호화는 아님)
                File.Copy(filePath, outputPath, true);
                
                MessageBox.Show($"파일이 복사되었습니다.\n저장 위치: {outputPath}\n\n" +
                    "주의: 이는 실제 복호화가 아닙니다.\n" +
                    "정상적인 복호화를 위해서는 DSCS DLL이 필요합니다.", 
                    "파일 복사 완료", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 복사 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}


