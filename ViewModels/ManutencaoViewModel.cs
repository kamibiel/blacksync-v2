using ReactiveUI;
using System;

public class ManutencaoViewModel : ReactiveObject
{
    private DateTime _de;

    public DateTime De
    {
        get => _de;
        set => this.RaiseAndSetIfChanged(ref _de, value);
    }

    public string DeFormatted => _de.ToString("dd/MM/yyyy");
}
