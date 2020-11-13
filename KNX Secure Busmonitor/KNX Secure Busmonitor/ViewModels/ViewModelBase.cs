using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Busmonitor.ViewModels
{
  public abstract class ViewModelBase : INotifyPropertyChanged
  {
    public virtual Task Initialize(object parameter) => Task.CompletedTask;
    public virtual Task Initialize() => Initialize(null);

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
