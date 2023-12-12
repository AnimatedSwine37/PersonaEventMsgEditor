using PersonaEventMsgEditor.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.ViewModels;
public class PreviewViewModel : ViewModelBase
{
    private Event? _event;
    public Event? Event
    {
        get => _event;
        set => this.RaiseAndSetIfChanged(ref _event, value);
    }
}
