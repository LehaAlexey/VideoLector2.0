using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLecturer.Core.Models;

namespace VideoLecturer.Core.Services
{
    public interface IPythonApiService
    {
        Task<string> GenerateVideoFragment(
            string generatedPath,
    string faceImagePath,
    string voicePath,
    string text,
    Guid projectId);
        Task CancelGeneration();
        Task<bool> TryToPing();
    }
}
