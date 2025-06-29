using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VideoLecturer.Core.Models;
using VideoLecturer.Core.Services;

namespace VideoLecturer.Infrastructure.Storage
{
    public class ProjectStorage : IProjectManager
    {
        public readonly string _storagePath;
        public readonly string _fragmentsPath;

        public ProjectStorage(string storagePath, string fragmentsPath)
        {
            _storagePath = storagePath;
            _fragmentsPath = fragmentsPath;
            EnsureDirectoryExists(_storagePath);
            EnsureDirectoryExists(_fragmentsPath);
        }

        public IEnumerable<Project> GetAllProjects()
        {
            var files = Directory.GetFiles(_storagePath, "*.xml");
            var serializer = new XmlSerializer(typeof(Project));

            foreach (var file in files)
            {
                using var reader = new StreamReader(file);
                yield return (Project)serializer.Deserialize(reader);
            }
        }

        public Project CreateNewProject() => new() { Name = "Новый проект", Id = Guid.NewGuid()};

        public void SaveProject(Project project)
        {
            var filePath = Path.Combine(_storagePath, $"{project.Id}.xml");
            var serializer = new XmlSerializer(typeof(Project));

            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, project);
            }
        }

        public Project LoadProject(Guid id)
        {
            var filePath = Path.Combine(_storagePath, $"{id}.xml");
            var serializer = new XmlSerializer(typeof(Project));

            using (var reader = new StreamReader(filePath))
            {
                var project = (Project)serializer.Deserialize(reader);
                project.Fragments = new System.Collections.ObjectModel.ObservableCollection<VideoFragment>();

                // Путь к директории с фрагментами
                var fragmentsDir = Path.Combine("generated", id.ToString(), "video_fragments");

                // Проверяем существование директории
                if (Directory.Exists(fragmentsDir))
                {
                    // Получаем все файлы фрагментов по шаблону video_fragment_0_*.mp4
                    var fragmentFiles = Directory.GetFiles(fragmentsDir, "video_fragment_0_*_1.mp4");

                    // Сортируем файлы по номеру фрагмента (из имени файла)
                    var sortedFiles = fragmentFiles
                        .OrderBy(f =>
                        {
                            var fileName = Path.GetFileNameWithoutExtension(f);
                            var parts = fileName.Split('_');
                            return int.Parse(parts[3]); // Получаем id-фрагмента из video_fragment_0_{id}
                        })
                        .ToList();

                    // Создаем объекты VideoFragment
                    for (int i = 0; i < sortedFiles.Count; i++)
                    {
                        // Номер слайда всегда 0 (как в имени файла), но можно использовать i+1 если нужно
                        var fragment = new VideoFragment(0, sortedFiles[i]);

                        // Если нужно, можно установить Duration (например, с помощью FFmpeg)
                        // fragment.Duration = GetVideoDuration(sortedFiles[i]);

                        project.Fragments.Add(fragment);
                    }
                }

                return project;
            }
        }

        public void DeleteProject(Guid id)
        {
            var projectFile = Path.Combine(_storagePath, $"{id}.xml");
            if (File.Exists(projectFile))
                File.Delete(projectFile);

            foreach (var fragmentFile in Directory.GetFiles(_fragmentsPath, $"{id}_*"))
            {
                File.Delete(fragmentFile);
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        List<Project> IProjectManager.GetAllProjects()
        {
            List<Project?> deserializeFiles = new List<Project?>();

            foreach (var file in Directory.GetFiles(_storagePath).Where(x => x.EndsWith(".xml")))
            { 
                var serializer = new XmlSerializer(typeof(Project));

                
                using (var reader = new StreamReader(file))
                {
                    deserializeFiles.Add((Project)serializer.Deserialize(reader));
                }
            }
            return deserializeFiles;
        }
    }
}
