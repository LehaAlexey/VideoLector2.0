using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLecturer.Core.Models;

namespace WpfApplication.Core.Services
{
    public interface ILectorManager
    {
        IEnumerable<LectorProfile> AllLectors { get; }
        LectorProfile GetLectorById(Guid id);
        void AddLector(LectorProfile lector);
        void RemoveLector(Guid id);
        void UpdateLector(LectorProfile lector);
        string GetPath();
    }
}
