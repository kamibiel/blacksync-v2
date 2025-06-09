namespace BlackSync.ViewModels
{
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Material.Dialog;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ManutencaoViewModel : ReactiveObject
    {
        public List<string> BancoDeDados { get; } = new List<string>
        {
            "Ambos",
            "Firebird",
            "MySQL"
        };

        public List<string> Comparacoes { get; } = new List<string>
        {
            ">",
            "<",
            "<=",
            ">=",
            "=",
            "between"
        };
    }
}
