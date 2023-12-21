using AtlusScriptLibrary.MessageScriptLanguage;

namespace PersonaEventMsgEditor.Models.Event;
public class Voice
{
    private int _id;
    public int Id { get => _id; }
    public int EventMajor { get; }
    public int EventMinor { get; }

    public Voice(int id, int eventMajor, int eventMinor)
    {
        _id = id;
        EventMajor = eventMajor;
        EventMinor = eventMinor;
    }

    public FunctionToken GetToken()
    {
        return new FunctionToken(2, 8, (ushort)Id);
    }
}
