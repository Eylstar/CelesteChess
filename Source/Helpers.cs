using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;


using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;

using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using On.Celeste.Editor;

namespace Celeste.Mod.CelesteChess;

public static class ChessHelpers
{
    public static bool TryGetPlayer(out Player p)
    {
        p = Engine.Scene?.Tracker?.GetEntity<Player>();
        return p != null;
    }
    
    public static void MoveSmoothlyEntity(Entity entity, Vector2 targetPosition, float duration)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMove(entity, targetPosition, duration)));
    }
    
    public static void MoveSmoothlyEntity(Entity entity, Vector2 targetPosition, float duration, Action onComplete)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMove(entity, targetPosition, duration, onComplete)));
    }
    
    static IEnumerator EntityMove(Entity entity, Vector2 targetPosition, float duration, Action onComplete = null)
    {
        if (entity == null) yield break;
        Vector2 startPosition = entity.Position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Engine.DeltaTime;
            float t = MathHelper.Clamp(elapsedTime / duration, 0f, 1f);
            float ease = Ease.SineOut(t);
            entity.Position = Vector2.Lerp(startPosition, targetPosition, ease);
            yield return null;
        }
        entity.Position = targetPosition;
        onComplete?.Invoke();
    }
}

public class PlutoniumText : Component
{
    public readonly Dictionary<char, int> Charset;
    public readonly List<MTexture> CharTextures;
    public Vector2 FontSize;
    public readonly string CharList;
    
    public PlutoniumText(string fontPath, string charList, Vector2 fontSize) : base(true, true)
    {
        FontSize = fontSize;
        Charset = new Dictionary<char, int>();
        CharTextures = new List<MTexture>();
        string characters = CharList = charList;

        for (int i = 0; i < characters.Length; i++)
        {
            Charset.Add(characters[i], i);
            CharTextures.Add(GFX.Game[fontPath].GetSubtexture(i * (int)fontSize.X, 0, (int)fontSize.X, (int)fontSize.Y)); // "PlutoniumHelper/PlutoniumText/font"
        }
    }
    
    public void PrintCentered(Vector2 pos, string str, bool shadow, int spacing, Color mainColor, Color outlineColor, float scale = 1, int id = 0)
    {
        float stringlen = spacing * str.Length * scale;
        Print(pos - new Vector2((float)Math.Floor(stringlen / 2f), (float)Math.Floor(FontSize.Y / 2f)), str, shadow, spacing, mainColor, outlineColor, scale, id);
    }

    public void Print(Vector2 pos, string str, bool shadow, int spacing, Color mainColor, Color outlineColor, float scale = 1, int id = 0)
    {
        SpriteEffects flip = SaveData.Instance.Assists.MirrorMode ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (SaveData.Instance.Assists.MirrorMode) str = new string(str.Reverse().ToArray());
        
        pos = pos.Floor();

        int index = 0;

        if (outlineColor != Color.Transparent)
        {
            foreach (char c in str) //draw shadows
            {
                float offset = index * spacing * scale;
                Vector2 charpos = pos + Vector2.UnitX * offset;
                charpos = new Vector2((float)Math.Floor(charpos.X), (float)Math.Floor(charpos.Y));

                int chr;
                if (!Charset.ContainsKey(c))
                    chr = 0;
                else
                    chr = Charset[c];
                
                if (shadow)
                    CharTextures[chr].Draw(charpos + Vector2.One * scale, Vector2.Zero, outlineColor, scale, 0, flip);

                index++;
                
            }
        }

        if (mainColor == Color.Transparent) return;

        index = 0;

        foreach (char c in str) //draw all characters
        {
            float offset = index * spacing * scale;
            Vector2 charpos = pos + Vector2.UnitX * offset;
            charpos = new Vector2((float)Math.Floor(charpos.X), (float)Math.Floor(charpos.Y));
            int chr;
            if (!Charset.TryGetValue(c, out int value))
                chr = 0;
            else
                chr = value;

            CharTextures[chr].Draw(charpos, Vector2.Zero, mainColor, scale, 0, flip);
            index++;
        }

    }
}



// OLD MANUAL NETWORKING CODE - REPLACED BY COOPHELPER

/*public class BoardNetData : DataType<BoardNetData>
{
    static BoardNetData()
    {
        DataID = "CelesteChess/Data";
    }
    
    public DataPlayerInfo Player;
    public string Info;
    public uint targetID;

    public int OriginCoordinate;
    public int TargetCoordinate;
    public int PieceType;

    public BoardNetData() { }

    public override bool FilterHandle(DataContext ctx) => Player != null;
    
    public override MetaType[] GenerateMeta(DataContext ctx) => new MetaType[]
    {
        new MetaPlayerUpdate(Player),
        new MetaOrderedUpdate(Player?.ID ?? uint.MaxValue)
        
    };

    public override void FixupMeta(DataContext ctx)
    {
        MetaPlayerUpdate upd = Get<MetaPlayerUpdate>(ctx);
        MetaOrderedUpdate order = Get<MetaOrderedUpdate>(ctx);
        order.ID = upd;
        Player = upd.Player;
    }

    protected override void Read(CelesteNetBinaryReader reader)
    {
        string data = reader.ReadNetString();
        Info = data;
        string[] arr = Info.Split(',', StringSplitOptions.TrimEntries);
        if (arr.Length == 2)
        {
            int.TryParse(arr[0], out OriginCoordinate);
            int.TryParse(arr[1], out TargetCoordinate);
            BoardManagerEntity.Instance.MoveReceivedByNetwork(OriginCoordinate, TargetCoordinate);
        }
        else
            Logger.Log(LogLevel.Error, "CelesteChess", $"Invalid data format: {Info}");
    }
    
    protected override void Write(CelesteNetBinaryWriter writer)
    {
        writer.WriteNetString(Info);
    }
}

public class InformationComponent : CelesteNetGameComponent
{
    static CelesteNetClientContext ctx;
    public static Dictionary<uint, int[]> Information = new();
    
    BoardManagerEntity boardManager;
    
    public InformationComponent(CelesteNetClientContext context, Game game) : base(context, game)
    {
        ctx = context;
    }

    public override void Start()
    {
        base.Start();
        if (Client == null)
            return;
        if (BoardManagerEntity.Instance == null)
            return;
        boardManager = BoardManagerEntity.Instance;
        boardManager.onMove += SendGameData;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        if (BoardManagerEntity.Instance == null) return;

        if (boardManager == null)
        {
            boardManager = BoardManagerEntity.Instance;
            boardManager.onMove += SendGameData;
        }

        if(BoardManagerEntity.Instance != boardManager)
        {
            boardManager.onMove -= SendGameData;
            boardManager = BoardManagerEntity.Instance;
            boardManager.onMove += SendGameData;
        }
    }

    public override void Disconnect(bool forceDispose = false)
    {
        base.Disconnect(forceDispose);
        if (Client == null)
            return;
        if(boardManager != null)
            boardManager.onMove -= SendGameData;
    }

    public void SendGameData(int orig, int target)
    {
        BoardManagerEntity manager = BoardManagerEntity.Instance;
        if (manager == null)
        {
            Logger.Log(LogLevel.Error, "CelesteChess", "BoardManagerEntity is null");
            return;
        }
        Logger.Log(LogLevel.Warn, "CelesteChess", $"SEND GAME DATA: {orig}, {target}");
        string dataToSend = $"{orig},{target}";
        Send(dataToSend);
    }
    
    public void Send(string info)
    {
        Client?.SendAndHandle(new BoardNetData()
        {
            Info = info,
            Player = Client.PlayerInfo, 
        });
    }

    public void Handle(CelesteNetConnection con, BoardNetData data)
    {
        if (data.Player != null)
            Information[data.Player.ID] = new int[]
            {
                data.OriginCoordinate,
                data.TargetCoordinate,
                data.PieceType
            };
    }
}*/