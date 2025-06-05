using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLecturer.Core.Models;

namespace VideoLecturer.Core.Services
{
    public interface IProjectManager
    {
        Project CreateNewProject();
        void SaveProject(Project project);
        Project LoadProject(Guid id);
        void DeleteProject(Guid id);

        List<Project> GetAllProjects();
    }
}
