using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Parser.DataModel
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected static readonly Char[] ws = new Char[] { ' ', '\n', '\t' };

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] String name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
