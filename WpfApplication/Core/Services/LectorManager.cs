using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using VideoLecturer.Core.Models;
using WpfApplication.Core.Services;

public class LectorManager : ILectorManager
{
    public readonly string _storagePath;
    private readonly List<LectorProfile> _lectors = new();

    public LectorManager(string lectorDirectoryPath)
    {
        _storagePath = lectorDirectoryPath;
        LoadLectors();
    }

    public IEnumerable<LectorProfile> AllLectors => _lectors;

    public LectorProfile GetLectorById(Guid id) => _lectors.FirstOrDefault(l => l.Id == id);

    public void AddLector(LectorProfile lector)
    {
        _lectors.Add(lector);
        SaveLector(lector);
    }

    public void RemoveLector(Guid id)
    {
        var lector = GetLectorById(id);
        if (lector != null)
        {
            _lectors.Remove(lector);
            DeleteLectorXml(lector);
            DeleteLectorFile(lector);
        }
    }

    private void LoadLectors()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            return;
        }

        foreach (var file in Directory.GetFiles(_storagePath, "*.xml"))
        {
            try
            {
                var serializer = new XmlSerializer(typeof(LectorProfile));
                using (var reader = new StreamReader(file))
                {
                    if (serializer.Deserialize(reader) is LectorProfile lector)
                    {
                        _lectors.Add(lector);
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при загрузке лектора {file}: {ex.Message}");
            }
        }
    }
    private void SaveLectorToXml(LectorProfile lector)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, $"{lector.Id}.xml");
            var serializer = new XmlSerializer(typeof(LectorProfile));

            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, lector);
            }
        }
        catch (Exception ex)
        {
            // Логирование ошибки
            Console.WriteLine($"Ошибка при сохранении лектора {lector.Id}: {ex.Message}");
            throw;
        }
    }
    private void DeleteLectorXml(LectorProfile lector)
    {
        var filePath = Path.Combine(_storagePath, $"{lector.Id}.xml");
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при удалении образа лектора {filePath}: {ex.Message}");
            }
        }
    }
    private void SaveLector(LectorProfile lector)
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        var filePath = GetLectorFilePath(lector);
        var serializer = new XmlSerializer(typeof(LectorProfile));

        using (var writer = new StreamWriter(filePath))
        {
            serializer.Serialize(writer, lector);
        }
    }

    private void DeleteLectorFile(LectorProfile lector)
    {
        var filePath = GetLectorFilePath(lector);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                // Логирование ошибки при удалении файла
                Console.WriteLine($"Ошибка при удалении файлов лектора {filePath}: {ex.Message}");
            }
        }
    }

    private string GetLectorFilePath(LectorProfile lector)
    {
        return Path.Combine(_storagePath, $"{lector.Id}.xml");
    }

    // Обновление данных лектора
    public void UpdateLector(LectorProfile lector)
    {
        var existing = GetLectorById(lector.Id);
        if (existing != null)
        {
            existing.Name = lector.Name;
            existing.ImagePath = lector.ImagePath;
            existing.VoicePath = lector.VoicePath;
            SaveLectorToXml(existing);
        }
    }

    public string GetPath()
    {
        return _storagePath;
    }
}