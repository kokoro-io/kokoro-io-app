namespace KokoroIO.XamarinForms.ViewModels
{
    internal interface IUploaderInfoHost
    {
        ApplicationViewModel Application { get; }
        UploaderInfo SelectedUploader { get; set; }
    }
}