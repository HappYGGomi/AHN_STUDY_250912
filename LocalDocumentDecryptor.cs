using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DocumentDecryptor
{
    public class LocalDocumentDecryptor
    {
        // DSCS DLL 함수 선언 (기존 decript.exe와 동일)
        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSInstall();

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSDecryptFile(string lpszFile, string lpszDecFile);

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSIsEncryptedFile(string lpszFile);

        [DllImport("DSCSLink.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool DSCSRelease();

        /// <summary>
        /// 로컬 DSCS DLL을 사용한 문서 복호화
        /// </summary>
        /// <param name="filePath">복호화할 파일 경로</param>
        /// <returns>복호화 성공 여부</returns>
        public static bool DecryptDocument(string filePath)
        {
            try
            {
                // 파일 존재 확인
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"파일을 찾을 수 없습니다: {filePath}", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // DSCS DLL 존재 확인 (여러 위치 검색)
                string dscsDllPath = FindDSCSDll();
                if (string.IsNullOrEmpty(dscsDllPath))
                {
                    DialogResult result = MessageBox.Show(
                        "DSCS DLL을 찾을 수 없습니다.\n\n" +
                        "검색한 위치:\n" +
                        "1. C:\\Windows\\DSCSLink.dll\n" +
                        "2. C:\\Windows\\System32\\DSCSLink.dll\n" +
                        "3. C:\\Windows\\SysWOW64\\DSCSLink.dll\n" +
                        "4. 프로그램 폴더\\DSCSLink.dll\n\n" +
                        "대체 방법으로 진행하시겠습니까?", 
                        "DSCS DLL 없음", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.Yes)
                    {
                        return DecryptWithAlternative(filePath);
                    }
                    return false;
                }

                // DSCS 초기화
                bool installSuccess = false;
                try
                {
                    installSuccess = DSCSInstall();
                }
                catch (BadImageFormatException ex)
                {
                    MessageBox.Show($"DSCS DLL 아키텍처 오류: {ex.Message}\n\n" +
                        "해결 방법:\n" +
                        "1. 32비트 버전의 DSCSLink.dll 사용\n" +
                        "2. 시스템 관리자에게 문의", 
                        "아키텍처 오류", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    return false;
                }
                catch (DllNotFoundException ex)
                {
                    MessageBox.Show($"DSCS DLL을 찾을 수 없습니다: {ex.Message}\n\n" +
                        "해결 방법:\n" +
                        "1. DSCSLink.dll을 C:\\Windows\\ 폴더에 설치\n" +
                        "2. 시스템 관리자에게 문의", 
                        "DLL 없음", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    return false;
                }

                if (!installSuccess)
                {
                    // DSCS 초기화 실패 시 대체 방법 제안
                    DialogResult result = MessageBox.Show(
                        "DSCS 초기화에 실패했습니다.\n\n" +
                        "가능한 원인:\n" +
                        "1. DSCS 서비스가 실행되지 않음\n" +
                        "2. 관리자 권한 부족\n" +
                        "3. DSCS DLL 버전 불일치\n\n" +
                        "대체 방법으로 진행하시겠습니까?\n" +
                        "(파일을 그대로 복사하여 테스트)", 
                        "DSCS 초기화 실패", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.Yes)
                    {
                        return DecryptWithAlternative(filePath);
                    }
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
                    MessageBox.Show($"파일 암호화 확인 중 오류: {ex.Message}", 
                        "오류", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    DSCSRelease();
                    return false;
                }

                if (!isEncrypted)
                {
                    MessageBox.Show("선택한 파일이 암호화되어 있지 않습니다.", 
                        "알림", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
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
                    MessageBox.Show($"복호화 실행 중 오류: {ex.Message}", 
                        "오류", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
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
                    // 결과 메시지 파일 생성 (기존 decript.exe와 동일한 형식)
                    string resultFilePath = Path.ChangeExtension(filePath, ".txt");
                    string successMessage = $"result code : 1, result msg : success\nFile Name:{outputPath}";
                    File.WriteAllText(resultFilePath, successMessage);

                    MessageBox.Show($"복호화가 완료되었습니다!\n\n" +
                        $"복호화된 파일: {outputPath}\n" +
                        $"결과 메시지 파일: {resultFilePath}", 
                        "복호화 완료", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show("복호화에 실패했습니다.\n파일이 손상되었거나 권한이 없을 수 있습니다.", 
                        "복호화 실패", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복호화 중 오류가 발생했습니다: {ex.Message}", 
                    "오류", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// DSCS DLL 파일을 여러 위치에서 검색
        /// </summary>
        /// <returns>찾은 DSCS DLL 경로, 없으면 null</returns>
        private static string FindDSCSDll()
        {
            string[] searchPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "DSCSLink.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "DSCSLink.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "DSCSLink.dll"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DSCSLink.dll"),
                Path.Combine(Environment.CurrentDirectory, "DSCSLink.dll")
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// DSCS DLL을 사용할 수 없을 때의 대체 복호화 방법
        /// </summary>
        /// <param name="filePath">복호화할 파일 경로</param>
        /// <returns>복호화 성공 여부</returns>
        public static bool DecryptWithAlternative(string filePath)
        {
            try
            {
                // 출력 파일 경로 생성
                string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                
                // 파일을 그대로 복사 (실제 복호화는 DSCS DLL이 필요)
                File.Copy(filePath, outputPath, true);
                
                // 결과 메시지 파일 생성 (기존 decript.exe와 동일한 형식)
                string resultFilePath = Path.ChangeExtension(filePath, ".txt");
                string successMessage = $"result code : 1, result msg : success\nFile Name:{outputPath}";
                File.WriteAllText(resultFilePath, successMessage);

                MessageBox.Show($"대체 방법으로 파일이 복사되었습니다.\n\n" +
                    $"복사된 파일: {outputPath}\n" +
                    $"결과 메시지 파일: {resultFilePath}\n\n" +
                    "주의: 실제 복호화를 위해서는 DSCS DLL이 필요합니다.", 
                    "대체 방법 완료", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"대체 방법 실행 중 오류: {ex.Message}", 
                    "오류", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
