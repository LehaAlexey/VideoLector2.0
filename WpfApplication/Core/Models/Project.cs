using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using WMPLib;
using WpfApplication.Core.Models;

namespace VideoLecturer.Core.Models
{
    [Serializable]
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string TextFilePath { get; set; }
        public string PdfFilePath { get; set; }
        public Guid LectorId { get; set; }
        [XmlElement("VideoPosition")]
        public VideoPosition Position { get; set; } = VideoPosition.TopRight;
        public ObservableCollection<VideoFragment> Fragments { get; set; } = new();
        public DateTime CreationDate { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
