using System;
using System.IO;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.UWP;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Xamarin.Forms;
using XLabs.Platform.Services.Media;

[assembly: Dependency(typeof(MediaPicker))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class MediaPicker : IMediaPicker
    {
        public bool IsCameraAvailable => true;

        public bool IsPhotosSupported => true;
        public bool IsVideosSupported => false;

        public EventHandler<MediaPickerArgs> OnMediaSelected { get; set; }
        public EventHandler<MediaPickerErrorArgs> OnError { get; set; }

        public async Task<MediaFile> SelectPhotoAsync(CameraMediaStorageOptions options)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".jpe");
            picker.FileTypeFilter.Add(".jfif");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var tmp = Path.GetTempFileName();
                var tf = await StorageFile.GetFileFromPathAsync(tmp);
                await file.CopyAsync(await tf.GetParentAsync(), Path.GetFileName(tmp), NameCollisionOption.ReplaceExisting);
                var mf = new MediaFile(file.Path, () => new FileStream(tmp, FileMode.Open));

                OnMediaSelected?.Invoke(this, new MediaPickerArgs(mf));

                return mf;
            }
            else
            {
                return null;
            }
        }

        public Task<MediaFile> SelectVideoAsync(VideoMediaStorageOptions options)
        {
            throw new NotSupportedException();
        }

        public async Task<MediaFile> TakePhotoAsync(CameraMediaStorageOptions options)
        {
            var camera = new CameraCaptureUI();

            var file = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (file != null)
            {
                var tmp = Path.GetTempFileName();
                var tf = await StorageFile.GetFileFromPathAsync(tmp);
                await file.CopyAsync(await tf.GetParentAsync(), Path.GetFileName(tmp), NameCollisionOption.ReplaceExisting);
                var mf = new MediaFile(file.Path, () => new FileStream(tmp, FileMode.Open));

                OnMediaSelected?.Invoke(this, new MediaPickerArgs(mf));

                return mf;
            }
            else
            {
                return null;
            }
        }

        public Task<MediaFile> TakeVideoAsync(VideoMediaStorageOptions options)
        {
            throw new NotSupportedException();
        }
    }
}