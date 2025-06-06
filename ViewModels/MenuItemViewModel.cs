using ReactiveUI;
using System.Collections.ObjectModel;

public class MenuItemViewModel : ReactiveObject
{
    public string Title { get; set; }
    public ObservableCollection<string> SubItems { get; set; } = new();

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}
