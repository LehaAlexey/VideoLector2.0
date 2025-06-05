using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VideoLecturer.Core.Models;

namespace WpfApplication.Core.ViewModels
{
    public class VideoFragmentViewModel : BaseViewModel
    {
        private readonly VideoFragment _fragment;

        public int SlideNumber => _fragment.SlideNumber;
        public string VideoPath => _fragment.VideoPath;
        public TimeSpan Duration => _fragment.Duration;

        public VideoFragmentViewModel(VideoFragment fragment)
        {
            _fragment = fragment;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
