using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public abstract class EntityCandicates<T> : ObservableObject
    {
        protected readonly MessagesViewModel Page;

        internal EntityCandicates(MessagesViewModel page)
        {
            Page = page;
        }

        internal abstract ObservableRangeCollection<T> Source { get; }
        internal abstract char PrefixChar { get; }

        internal abstract bool IsValidChar(char c);

        internal abstract IEnumerable<T> FilterByPrefix(string prefix);

        internal abstract string GetValue(T item);

        #region HasResult

        private bool _HasResult;

        public bool HasResult
        {
            get => _HasResult;
            private set => SetProperty(ref _HasResult, value);
        }

        #endregion HasResult

        #region Result

        private ObservableRangeCollection<T> _Result;

        public ObservableRangeCollection<T> Result
            => _Result ?? (_Result = new ObservableRangeCollection<T>());

        #endregion Result

        internal void UpdateResult()
        {
            var t = Page.NewMessage;
            var s = Page.SelectionStart;
            var l = Page.SelectionLength;

            if (t != null && 0 < s && l >= 0 && Source.Count > 0)
            {
                var i = s + l;
                if (i == t.Length || (i < t.Length && char.IsWhiteSpace(t[i])))
                {
                    for (i--; i >= 0; i--)
                    {
                        var c = t[i];
                        if (c == PrefixChar)
                        {
                            var pref = t.Substring(i + 1, s + l - i - 1);

                            Result.ReplaceRange(FilterByPrefix(pref));
                            HasResult = Result.Any();
                            return;
                        }
                        else if (IsValidChar(c))
                        {
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            HasResult = false;
            Result.Clear();
        }

        private Command _SelectCommand;

        public Command SelectCommand
            => _SelectCommand ?? (_SelectCommand = new Command(OnSelect));

        private void OnSelect(object parameter)
        {
            if (!(parameter is T p))
            {
                return;
            }
            var t = Page.NewMessage;
            var s = Page.SelectionStart;
            var l = Page.SelectionLength;

            if (t != null && 0 < s && l >= 0)
            {
                var i = s + l;
                if (i == t.Length || (i < t.Length && char.IsWhiteSpace(t[i])))
                {
                    for (i--; i >= 0; i--)
                    {
                        var c = t[i];
                        if (c == PrefixChar)
                        {
                            var sn = GetValue(p);
                            Page.NewMessage = t.Substring(0, i) + PrefixChar + sn + (s + l == t.Length ? " " : (" " + t.Substring(s + l + 1)));
                            Page.SelectionStart = i + sn.Length + 2;
                            Page.SelectionLength = 0;
                            Page.NewMessageFocused = true;
                            Page.CandicateClicked = DateTime.Now;
                            return;
                        }
                        else if (IsValidChar(c))
                        {
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}