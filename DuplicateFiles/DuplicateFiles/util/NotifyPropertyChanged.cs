using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfUtils
{
    /// <summary>
    /// 
    /// This implementation of <see cref="INotifyPropertyChanged"/> is not needed if attribute
    /// <see cref="[ImplementPropertyChanged]"/> is used on a class, but can be combined with it
    /// to allow attaching event listeners without reflection.
    /// 
    /// If only [ImplementPropertyChanged] is used, it will weave this implementation in by itself.
    /// 
    /// </summary>
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
