using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploadToChannelViewModel : BaseViewModel
    {
        internal UploadToChannelViewModel(ApplicationViewModel application, Func<Stream> streamCreator)
        {
            Application = application;
            StreamCreator = streamCreator;
            if (application.Channels.Any())
            {
                _Channels = new ObservableCollection<ChannelViewModel>(application.Channels.Where(r => !r.IsArchived));
            }
        }

        public ApplicationViewModel Application { get; }

        internal Func<Stream> StreamCreator { get; }

        private ImageSource _Image;

        public ImageSource Image
            => _Image ?? (_Image = ImageSource.FromStream(StreamCreator));

        private ObservableCollection<ChannelViewModel> _Channels;

        public ObservableCollection<ChannelViewModel> Channels
        {
            get
            {
                InitChannels();
                return _Channels;
            }
        }

        private async void InitChannels()
        {
            if (_Channels == null)
            {
                _Channels = new ObservableCollection<ChannelViewModel>();

                var memberships = await Application.GetMembershipsAsync(archived: false).ConfigureAwait(false);

                foreach (var m in memberships)
                {
                    var r = m.Channel;
                    var rvm = Application.Channels.FirstOrDefault(rm => rm.Id == r.Id);
                    if (rvm != null)
                    {
                        _Channels.Add(rvm);
                    }
                }
            }
        }
    }
}