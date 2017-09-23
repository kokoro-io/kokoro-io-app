using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class MembersLabel : EntitiesLabel
    {
        internal override string GetText(object item)
            => "@" + ((ProfileViewModel)item).ScreenName;
    }
}