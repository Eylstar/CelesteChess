namespace Celeste.Mod.CelesteChess;

public class CelesteChessModule : EverestModule {
    public static CelesteChessModule Instance { get; private set; }
    
    public CelesteChessModule() {
        Instance = this;
        #if DEBUG
            Logger.SetLogLevel(nameof(CelesteChessModule), LogLevel.Verbose);
        #else
            Logger.SetLogLevel(nameof(CelesteChessModule), LogLevel.Info);
        #endif
    }

    public override void Load()
    {
        On.Celeste.Player.Drop += ChessHooks.OnDrop;
        On.Celeste.TheoCrystal.OnRelease += ChessHooks.OnRelease;
        On.Celeste.TheoCrystal.OnPickup += ChessHooks.OnPickup;
        IL.Celeste.Player.NormalUpdate += ChessHooks.IL_DropStopping;
    }

    public override void Unload()
    {
        On.Celeste.Player.Drop -= ChessHooks.OnDrop;
        On.Celeste.TheoCrystal.OnRelease -= ChessHooks.OnRelease;
        On.Celeste.TheoCrystal.OnPickup -= ChessHooks.OnPickup;
        IL.Celeste.Player.NormalUpdate -= ChessHooks.IL_DropStopping;
    }
}