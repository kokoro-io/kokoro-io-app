using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XLabs.Forms.Controls;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CommandListPopup : PopupLayout
    {
        public CommandListPopup()
        {
            InitializeComponent();
        }
    }
}