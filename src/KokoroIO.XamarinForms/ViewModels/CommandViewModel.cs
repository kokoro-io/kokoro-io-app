using System.Windows.Input;

namespace KokoroIO.XamarinForms.ViewModels
{
    public class CommandViewModel : ObservableObject
    {
        private string _Title;

        public CommandViewModel(string title, ICommand command)
        {
            _Title = title;
            Command = command;
        }

        public string Title
        {
            get => _Title;
            set => SetProperty(ref _Title, value);
        }

        public ICommand Command { get; }
    }
}