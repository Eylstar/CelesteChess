using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;

using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;

namespace Celeste.Mod.CelesteChess;

public static class MoveHelper
{
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
    
    public static void MoveSmoothlyEntityHorizontal(Entity entity, float targetX, float duration)
    {
        if (entity == null) return;
        entity.Add(new Coroutine(EntityMoveHorizontal(entity, targetX, duration)));
    }
    
    static IEnumerator EntityMoveHorizontal(Entity entity, float targetX, float duration)
    {
        if (entity == null) yield break;
        Vector2 startPosition = entity.Position;
        float elapsedTime = 0f;
        Vector2 targetPosition = new Vector2(targetX, entity.Position.Y);

        while (elapsedTime < duration)
        {
            targetPosition = new Vector2(targetX, entity.Position.Y);
            elapsedTime += Engine.DeltaTime;
            float t = MathHelper.Clamp(elapsedTime / duration, 0f, 1f);
            entity.Position = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        entity.Position = targetPosition;
    }
}

public class BoardNetData : DataType<BoardNetData>
{
    static BoardNetData()
    {
        DataID = "CelesteChess/Data";
    }
    
    public DataPlayerInfo Player;
    public string Info;

    public int OriginCoordinate;
    public int TargetCoordinate;
    public int PieceType;

    public BoardNetData() { }

    public override bool FilterHandle(DataContext ctx) 
        => Player != null;
    
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
        Logger.Log(LogLevel.Warn, "OnlineTest", $"Reading data: {data}");
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

    public override void Start()
    {
        base.Start();
        if (Client == null)
            return;
        BoardManagerEntity.Instance.onMove += SendGameData;
    }

    public InformationComponent(CelesteNetClientContext context, Game game) : base(context, game)
    {
        ctx = context;
    }

    public void SendGameData(int orig, int target)
    {
        BoardManagerEntity manager = BoardManagerEntity.Instance;
        if (manager == null)
        {
            Logger.Log(LogLevel.Error, "CelesteChess", "BoardManagerEntity is null");
            return;
        }
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
}
public static class ChessNetHelper
{
    public static int[] TryGetData(BoardManagerEntity manager, out int[] data)
    {
        data = null;
        if (manager == null)
            return null;

        ChessPieceEntity piece = manager.GetPlayer()?.Holding?.Entity as ChessPieceEntity;
        if (piece == null)
            return null;

        int[] arr = new int[3];
        arr[0] = (int)piece.tile.gridCoordinates.X;
        arr[1] = (int)piece.tile.gridCoordinates.Y;
        arr[2] = (int)piece.pieceType;
        
        return arr;
    }
}