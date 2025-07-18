using AvalonInjectLib.Interfaces;

[AutoDetectScript(true)]
public class PlayerMovementScript : IAvalonScript
{
    IAvalonEngine _engine;

    public string Name => "OrbWalk";

    public string Version => "1.0.0";

    public string Description => "Uan descripcion";

    public void Initialize(IAvalonEngine engine)
    {
        _engine = engine;
        engine.Log("Mi Script a cargado correctamente.");
    }

    public void Draw()
    {

    }

    public void Update()
    {

    }
}

