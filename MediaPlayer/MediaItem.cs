using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaInfo;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using NReco;

namespace MediaPlayer
{
    class MediaItem : INotifyPropertyChanged
    {
        public string Path { get; set; }
        
        public MediaItem(string path)
        {
            Path = path;
            Playing = 0;
            GenerateThumbnail();
        }
        public string Name
        {
            get
            {
                var info = new FileInfo(Path);
                var name = System.IO.Path.GetFileNameWithoutExtension(info.Name);
                return name;
            }
        }
        public string Thumbnail
        {
            get
            {
                if (System.IO.Path.GetExtension(Path) != ".mp4") return "image/mp3Thumb.jpg";
                return $"thumbnails/{Name}.jpg";
            }
        }
        public int Playing { get; set; }
        public string Duration
        {
            get
            {
                using (var shell = ShellObject.FromParsingName(Path))
                {
                    IShellProperty prop = shell.Properties.System.Media.Duration;
                    var t = (ulong)prop.ValueAsObject;
                    TimeSpan time = TimeSpan.FromTicks((long)t);

                    int hours = time.Hours;
                    int minutes = time.Minutes;
                    int seconds = time.Seconds;
                    return hours == 0 ? $"{minutes:00}:{seconds:00}" : $"{hours}:{minutes:00}:{seconds:00}";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void GenerateThumbnail()
        {
            if (System.IO.Path.GetExtension(Path) != ".mp4") return;
            if (!Directory.Exists("thumbnails"))
            {
                Directory.CreateDirectory("thumbnails");
            };
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            try
            {
                ffMpeg.GetVideoThumbnail(Path, $"thumbnails/{Name}.jpg");
            } catch (Exception exp)
            {
                return;
            }
        }
        
    }
}
