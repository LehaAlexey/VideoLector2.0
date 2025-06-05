using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoLecturer.Core.Models;
using VideoLecturer.Core.Services;

namespace VideoLecturer.Infrastructure.PythonAPI
{
    public class PythonApiService : IPythonApiService
    {
        private readonly HttpClient _httpClient = new();
        private const string BaseUrl = "http://localhost:54547";
        private CancellationTokenSource _generationCts;

        public async Task<string> GenerateVideoFragment(
            string generatedPath,
            string faceImagePath,
            string voicePath,
            string text,
            Guid projectId)
        {
            try
            {
                // Проверяем доступность сервера
                if (!await TryToPing())
                {
                    throw new Exception("Python API сервер недоступен");
                }

                // Создаем папку для проекта
                var projectDir = Path.Combine(generatedPath, projectId.ToString());
                Directory.CreateDirectory(projectDir);

                // Создаем временный файл для текста
                var textFilePath = Path.Combine(projectDir, $"{Guid.NewGuid()}.txt");
                await File.WriteAllTextAsync(textFilePath, text);

                _generationCts = new CancellationTokenSource();

                var digits = Regex.Replace(projectId.ToString(), "[^0-9]", "");
                int Id = int.Parse(digits.Length > 9 ? digits.Substring(0, 9) : digits);
                var requestData = new
                {
                    face_path = faceImagePath.Replace("\\", "/"),
                    voice_path = voicePath.Replace("\\", "/"),
                    lecture_text_path = textFilePath.Replace("\\", "/"),
                    generated_dir_path = projectDir.Replace("\\", "/"),
                    id = Id
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{BaseUrl}/generation",
                    requestData,
                    _generationCts.Token);

                response.EnsureSuccessStatusCode();

                try
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                }
                catch
                { 
                }
                // Возвращаем путь к папке с результатами
                return projectDir;
            }
            catch (OperationCanceledException)
            {
                Directory.Delete(Path.Combine(generatedPath, projectId.ToString()), true);
                throw new Exception("Генерация отменена пользователем");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка генерации: {ex.Message}");
            }
        }

        public async Task CancelGeneration()
        {
            try
            {
                if (_generationCts != null && !_generationCts.IsCancellationRequested)
                {
                    _generationCts.Cancel();

                    // Отправляем запрос на отмену на сервер
                    var response = await _httpClient.GetAsync($"{BaseUrl}/generation/cancel");
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при отмене генерации: {ex.Message}");
            }
        }

        public async Task<bool> TryToPing()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/ping");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                return result?.result == true;
            }
            catch
            {
                return false;
            }
        }

        private class ApiResponse
        {
            public bool error { get; set; }
            public string message { get; set; }
            public bool? result { get; set; }
        }
    }
}