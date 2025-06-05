using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoLecturer.Core.Models
{
    public class VideoFragment
    {
        public VideoFragment()
        {
                
        }
        public VideoFragment(int slideNumber, string fragmentPath)
        {
            SlideNumber= slideNumber;
            VideoPath= fragmentPath;
        }
        public int SlideNumber { get; set; }
        public string VideoPath { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
