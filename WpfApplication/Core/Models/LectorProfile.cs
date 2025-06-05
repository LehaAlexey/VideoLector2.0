using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace VideoLecturer.Core.Models
{
    [XmlRoot("LectorProfile")]
    public class LectorProfile : INotifyPropertyChanged
    {
        private Guid _id;
        private string _name;
        private string _imagePath;
        private string _voicePath;

        [XmlAttribute("Id")]
        public Guid Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlElement("Name")]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlElement("ImagePath")]
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlElement("VoicePath")]
        public string VoicePath
        {
            get => _voicePath;
            set
            {
                if (_voicePath != value)
                {
                    _voicePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Конструктор по умолчанию для XML-сериализации
        public LectorProfile() { }

        public LectorProfile(string name, string imagePath, string voicePath)
        {
            Name = name;
            ImagePath = imagePath;
            VoicePath = voicePath;
        }
    }
}