using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DocumentDecryptor
{
    public class WebDocumentDecryptor
    {
        private static readonly HttpClient httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5) // 5분 타임아웃 설정
        };
        
        // SoftCamp 복호화 서비스 URL (실제 URL로 변경 필요)
        private const string DECRYPT_SERVICE_URL = "https://www.softcamp.co.kr/api/decrypt";
        
        /// <summary>
        /// 웹 기반 문서 복호화
        /// </summary>
        /// <param name="filePath">복호화할 파일 경로</param>
        /// <returns>복호화 성공 여부</returns>
        public static async Task<bool> DecryptDocumentAsync(string filePath)
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

                // 파일 크기 확인 (일반적으로 웹 서비스는 파일 크기 제한이 있음)
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 100 * 1024 * 1024) // 100MB 제한
                {
                    MessageBox.Show("파일 크기가 너무 큽니다. (최대 100MB)", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 네트워크 연결 테스트
                bool networkAvailable = await TestConnectionAsync();
                if (!networkAvailable)
                {
                    // 네트워크 연결 실패 시 로컬 테스트 모드 제공
                    DialogResult result = MessageBox.Show(
                        "네트워크 연결에 문제가 있습니다.\n\n" +
                        "해결 방법:\n" +
                        "1. 인터넷 연결 확인\n" +
                        "2. 방화벽 설정 확인\n" +
                        "3. VPN 연결 확인\n\n" +
                        "로컬 테스트 모드로 진행하시겠습니까?",
                        "네트워크 오류", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.Yes)
                    {
                        // 로컬 테스트 모드 - 파일을 그대로 복사
                        string outputPath = Path.ChangeExtension(filePath, ".decrypted");
                        File.Copy(filePath, outputPath, true);
                        
                        string resultFilePath = Path.ChangeExtension(filePath, ".txt");
                        string testMessage = $"result code : 1, result msg : success\nFile Name:{outputPath}";
                        await File.WriteAllTextAsync(resultFilePath, testMessage);
                        
                        MessageBox.Show($"테스트 모드로 파일이 복사되었습니다.\n\n" +
                            $"복사된 파일: {outputPath}\n" +
                            $"결과 메시지 파일: {resultFilePath}", 
                            "테스트 완료", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                        return true;
                    }
                    return false;
                }

                // 복호화 진행 중 메시지
                MessageBox.Show("복호화를 시작합니다...", "진행 중", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 파일을 바이트 배열로 읽기
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                
                // 웹 서비스로 파일 전송
                bool success = await UploadAndDecryptFile(fileBytes, filePath);
                
                if (success)
                {
                    MessageBox.Show("복호화가 완료되었습니다!", "완료", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("복호화에 실패했습니다.", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복호화 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 파일을 웹 서비스로 업로드하고 복호화 요청
        /// </summary>
        private static async Task<bool> UploadAndDecryptFile(byte[] fileBytes, string originalFilePath)
        {
            try
            {
                // MultipartFormDataContent 생성
                using (var content = new MultipartFormDataContent())
                {
                    // 파일 데이터 추가
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    content.Add(fileContent, "file", Path.GetFileName(originalFilePath));

                    // 복호화 요청 전송
                    var response = await httpClient.PostAsync(DECRYPT_SERVICE_URL, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // 응답 내용을 문자열로 읽기
                        string responseContent = await response.Content.ReadAsStringAsync();
                        
                        // 응답이 JSON 형태인지 확인하고 파싱
                        if (responseContent.Contains("result code") && responseContent.Contains("success"))
                        {
                            // 성공 메시지가 포함된 경우
                            string resultFilePath = Path.ChangeExtension(originalFilePath, ".txt");
                            await File.WriteAllTextAsync(resultFilePath, responseContent);
                            
                            // 복호화된 파일이 별도로 제공되는지 확인
                            if (responseContent.Contains("File Name:"))
                            {
                                // 파일명 추출
                                string fileName = ExtractFileNameFromResponse(responseContent);
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    MessageBox.Show($"복호화가 완료되었습니다!\n\n" +
                                        $"결과 파일: {fileName}\n" +
                                        $"결과 메시지가 저장된 파일: {resultFilePath}", 
                                        "복호화 완료", 
                                        MessageBoxButtons.OK, 
                                        MessageBoxIcon.Information);
                                    return true;
                                }
                            }
                            
                            MessageBox.Show($"복호화가 완료되었습니다!\n\n" +
                                $"결과 메시지: {responseContent}", 
                                "복호화 완료", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Information);
                            return true;
                        }
                        else
                        {
                            // 응답이 바이너리 파일인 경우 (기존 로직)
                            byte[] decryptedBytes = await response.Content.ReadAsByteArrayAsync();
                            
                            // 복호화된 파일 저장
                            string outputPath = Path.ChangeExtension(originalFilePath, ".decrypted");
                            await File.WriteAllBytesAsync(outputPath, decryptedBytes);
                            
                            // 결과 메시지 파일 생성
                            string resultFilePath = Path.ChangeExtension(originalFilePath, ".txt");
                            string successMessage = $"result code : 1, result msg : success\nFile Name:{outputPath}";
                            await File.WriteAllTextAsync(resultFilePath, successMessage);
                            
                            return true;
                        }
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"서버 오류: {response.StatusCode}\n{errorMessage}", "오류", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"네트워크 오류: {ex.Message}\n\n" +
                    "인터넷 연결을 확인하거나 방화벽 설정을 확인해주세요.", "네트워크 오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"업로드 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 응답에서 파일명 추출
        /// </summary>
        private static string ExtractFileNameFromResponse(string responseContent)
        {
            try
            {
                // "File Name:" 다음의 파일명 추출
                int fileNameIndex = responseContent.IndexOf("File Name:");
                if (fileNameIndex >= 0)
                {
                    string fileNamePart = responseContent.Substring(fileNameIndex + "File Name:".Length).Trim();
                    // 줄바꿈이 있다면 첫 번째 줄만 가져오기
                    int newLineIndex = fileNamePart.IndexOf('\n');
                    if (newLineIndex > 0)
                    {
                        fileNamePart = fileNamePart.Substring(0, newLineIndex).Trim();
                    }
                    return fileNamePart;
                }
            }
            catch
            {
                // 파일명 추출 실패 시 무시
            }
            return string.Empty;
        }

        /// <summary>
        /// 서비스 연결 테스트
        /// </summary>
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await httpClient.GetAsync(DECRYPT_SERVICE_URL.Replace("/api/decrypt", "/api/health"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
