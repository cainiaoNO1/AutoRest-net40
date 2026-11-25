using System.Collections.Generic;
using System.ComponentModel;

namespace AutoRest.ViewModels
{
    /// <summary>
    /// net40 兼容：不使用 .NET 4.5 才有的 [CallerMemberName]，
    /// 所有属性变更显式传入 nameof(属性名)。
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
