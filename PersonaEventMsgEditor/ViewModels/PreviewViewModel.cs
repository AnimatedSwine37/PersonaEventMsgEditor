using PersonaEventMsgEditor.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonaEventMsgEditor.ViewModels;
public class PreviewViewModel : ViewModelBase
{
    private EventMessage? _eventMsg;
    public EventMessage? Message
    {
        get => _eventMsg;
        set => this.RaiseAndSetIfChanged(ref _eventMsg, value);
    }

    public PreviewViewModel()
    {
    }

}