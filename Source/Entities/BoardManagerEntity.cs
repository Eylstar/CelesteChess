using System;
using System.Collections.Generic;
using Celeste.Mod.CoopHelper;
using Celeste.Mod.CoopHelper.IO;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

[CustomEntity("CelesteChess/BoardOutline")]
public class BoardOutlineEntity : Entity
{
    Image sprite;
    public BoardOutlineEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        Depth = 100000000;
        sprite = new Image(GFX.Game["CelesteChess/outline"]);
        sprite.Origin = new Vector2(sprite.Width / 2f, sprite.Height / 2f);
        Add(sprite);
    }
}

[Tracked]
[CustomEntity("CelesteChess/BoardManager")]
public class BoardManagerEntity : Entity
{
    public static BoardManagerEntity Instance;
    public GhostPieceEntity ghostPiece;
    
    ChessPieceEntity lastMovedPiece = null;
    
    Player player;
    public bool whiteTurn = true;
    
    List<ChessTileEntity> tiles = new();
    List<ChessTileEntity> validMoves = new();
    public ChessTileEntity hoveredTile;
    
    public BoardManagerEntity(EntityData data, Vector2 offset) : base(data.Position + offset) { }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        
        //Gets all the tiles in the scene and creates an Instance of the BoardManager
        foreach (Entity entity in scene.Tracker.GetEntities<ChessTileEntity>())
        {
            if (entity is ChessTileEntity tile)
            {
                tiles.Add(tile);
            }
        }
        Instance = this;
        
        AssignTilesValue();
        SpawnChessPieces();
        
        ghostPiece = new GhostPieceEntity(new EntityData(), new Vector2(50, 50));
        Scene.Add(ghostPiece);
        ghostPiece.enabled = false;
    }

    void AssignTilesValue()
    {
        //Sort the tiles in the List by their position on the Scene
        tiles.Sort((a, b) =>
        {
            if (a.Position.Y == b.Position.Y)
                return a.Position.X.CompareTo(b.Position.X);

            return a.Position.Y.CompareTo(b.Position.Y);
        });
        //Assign the grid position and coordinates to each tile
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].gridPosition = i;
            tiles[i].gridCoordinates = new Vector2(i % 8, i / 8);
        }
    }
    
    //Spawn the chess pieces on the board
    void SpawnChessPieces()
    {
        Dictionary<int, PieceInfo> piecesDataToSpawn = new();
        Piece[] row = { Piece.Rook, Piece.Knight, Piece.Bishop, Piece.Queen, Piece.King, Piece.Bishop, Piece.Knight, Piece.Rook };
        
        for (int i = 0; i < 8; i++)
        {
            //Add the pieces to the dictionary with their grid position
            piecesDataToSpawn.Add(i, new PieceInfo(row[i], ChessPieceColor.Black));
            piecesDataToSpawn.Add(i + 8, new PieceInfo(Piece.Pawn, ChessPieceColor.Black));
            piecesDataToSpawn.Add(i + 48, new PieceInfo(Piece.Pawn, ChessPieceColor.White));
            piecesDataToSpawn.Add(i + 56, new PieceInfo(row[i], ChessPieceColor.White));
        }

        int index = 0;
        foreach (ChessTileEntity tile in tiles)
        {
            if (piecesDataToSpawn.TryGetValue(tile.gridPosition, out PieceInfo pieceInfo))
            {
                //Create a new chess piece and assign it a unique ID
                ChessPieceEntity chessPiece = new ChessPieceEntity(new EntityData(), tile.Position);
                chessPiece.constructorIndex = index;
                
                //Set the piece values and add it to the scene
                chessPiece.pieceType = pieceInfo.pieceType;
                chessPiece.ChessPieceColor = pieceInfo.ChessPieceColor;
                chessPiece.Position = tile.Position;
                chessPiece.tile = tile;
                tile.piece = chessPiece; 
                Scene.Add(chessPiece);
                index++;
            }
        }
    }
    
    public override void Update()
    {
        base.Update();
        
        if (ChessHelpers.TryGetPlayer(out player))
        {
            player.carryOffset = new Vector2(0, -20);

            //Teleport the player to the center if he presses Dash
            if (Input.DashPressed) 
                player.Position = tiles[27].Position + new Vector2(Math.Abs(tiles[27].Width / 2), 6);
        }
    }
    
    
    //Calculate the valid moves of a piece
    public void CalculateValidMoves(ChessTileEntity tile, ChessPieceEntity piece)
    {
        if (piece == null || tile == null) return;
        
        //Get all the valid moves of the piece
        validMoves.Clear();
        validMoves.AddRange(ChessEngine.CalculateValidMoves(piece, tile));
        
        if (validMoves == null || validMoves.Count == 0)
            return;
        
        //Create a list for the safe valid moves, that don't put the king in check
        List<ChessTileEntity> safe = new();
        foreach (ChessTileEntity vm in validMoves)
        {
            if (vm == null) continue;
            
            //For each valid move, create a temporary fake board
            BoardManagerEntity tempBoard = CloneBoard();
            ChessPieceEntity tempPiece = tempBoard.GetTileByGridCoordinates(piece.tile.gridCoordinates).piece;
            ChessTileEntity tempTile = tempBoard.GetTileByGridCoordinates(vm.gridCoordinates);
            
            //Simulate the move and check if it puts the king in check, if not, add it to the safe list
            if (tempBoard.SimulateMove(tempPiece, tempTile))
                safe.Add(vm);
            
            tempBoard.ReleaseClone();
        }
        validMoves = safe;
        
        foreach (ChessTileEntity validMove in validMoves)
        {
            //Set every safe tile state and color to valid
            if (validMove != null)
                validMove.SetValidMove();
        }
    }
    
    //Function used to compute the move of a piece made by the player or the network
    public void ComputeMove(ChessPieceEntity piece, ChessTileEntity moveTile, bool valid, bool sendToNetwork = true, bool switchTurns = true)
    {
        //Reset the color and state of all the tiles
        foreach (ChessTileEntity t in tiles)
        {
            t.colorTarget = ColorTarget.None;
            t.isValidForActualMove = false;
            validMoves.Clear();
        }
        
        //Return of the move is not valid
        if (moveTile == null || piece == null || moveTile == piece.tile || valid == false) return;


        if (moveTile.isCastleDestination)
        {
            //If the move is a castling, we need to move the rook too
            ChessTileEntity rookDestination = moveTile.rookDestinationTile;
            ChessPieceEntity rook = moveTile.castlePiece;
            
            if (rook != null && rookDestination != null)
            {
                SendMoveToNetwork(rook, rookDestination, true);
                MoveInducedByAction(rook.tile.gridPosition, rookDestination.gridPosition, false);
            }
        }
        
        //If the move is made by the player, send the move to the network
        if (sendToNetwork)
        {
            foreach (ChessTileEntity t in tiles)
                if (t.piece != null) t.piece.pawnJustMovedTwice = false;
            SendMoveToNetwork(piece, moveTile);
        }
        
        //Eating
        Scene.Remove(moveTile.enPassantCapturePiece ?? moveTile.piece);
            
        
        //Move the piece to the tile and set the tile as the new tile of the piece
        SwapValuesForMove(piece, moveTile);

        //Promotion
        if (piece.pieceType == Piece.Pawn && ((piece.IsWhite() && moveTile.gridCoordinates.Y == 0) || (!piece.IsWhite() && moveTile.gridCoordinates.Y == 7)))
            piece.PromoteToQueen();

        if (switchTurns)
        {
            whiteTurn = !whiteTurn;
            lastMovedPiece = piece;
        }

        foreach (ChessTileEntity tile in tiles)
        {
            tile.isCastleDestination = false;
            tile.castlePiece = null;
            tile.rookDestinationTile = null;
            tile.isDoubleMoveDestination = false;
            tile.enPassantCapturePiece = null;
        }
    }

    //Called to send the move to the network, we only send the origin and destination of the move
    public void SendMoveToNetwork(ChessPieceEntity piece, ChessTileEntity tile, bool rookCastling = false)
    {
        piece.originToNetwork = piece.tile.gridPosition;
        piece.destinationToNetwork = tile.gridPosition;
        piece.castleMove = rookCastling;
        piece.pawnJustMovedTwice = tile.isDoubleMoveDestination;
        piece.AteWithEnPassant = tile.enPassantCapturePiece != null;
        
        ModInterop.PostUpdate(piece.id);
    }
    
    //Called by the network (or castling) to move the piece on the local scene, we only receive the origin and destination of the move
    public void MoveInducedByAction(int origin, int target, bool switchActions = true, bool pawnJustMovedTwice = false, bool ateWtihEnPassant = false)
    {
        //Determine the origin tile, the target tile and the piece based on the received positions
        ChessTileEntity originTile = GetTileByGridPosition(origin);
        ChessTileEntity targetTile = GetTileByGridPosition(target);
        ChessPieceEntity piece = originTile.piece;
        
        if (piece == null || targetTile == null) return;
        
        piece.pawnJustMovedTwice = pawnJustMovedTwice;
        
        //Like in the hook called by local moves, disable the holdable, move the piece and compute it 
        Holdable holdable = piece.Get<Holdable>();
        holdable.Active = false;
            
        ChessHelpers.MoveSmoothlyEntity(piece, targetTile.Position + new Vector2(0, 6), 0.25f, () => { 
            holdable.Active = true;
        });

        if (ateWtihEnPassant)
            Scene.Remove(lastMovedPiece);
        
        ComputeMove(piece, targetTile, true, false, switchActions);
    }
    
    
    //Function used to create a clone of the board, to check if the king will be in check
    public BoardManagerEntity CloneBoard()
    {
        BoardManagerEntity clone = new(new EntityData(), Vector2.Zero);
        
        //Switch the turn for the clone
        clone.whiteTurn = !whiteTurn;
        
        //Create new list of tiles
        clone.tiles = new List<ChessTileEntity>();
        foreach (ChessTileEntity tile in tiles)
        {
            //Copy the instance tile and add it to the new list
            ChessTileEntity newTile = new ChessTileEntity(tile.gridCoordinates);
            clone.tiles.Add(newTile);

            if (tile.piece == null) continue;
            
            //Copy the instance piece and add it to the new tile
            ChessPieceEntity newPiece = tile.piece.Clone();
            newTile.piece = newPiece;
            newPiece.tile = newTile;
        }
        return clone;
    }
    
    //Function used to simulate a move on the clone board only, to check if the king will be in check, called once by valid move to test
    public bool SimulateMove(ChessPieceEntity piece, ChessTileEntity destination)
    {
        SwapValuesForMove(piece, destination);
        
        //If the simulated move puts the king in check, return false to mark the move as not safe / valid
        return !IsInCheck(true);
    }

    //Called by the clone, used to check if the king is in check ~uwu~
    public bool IsInCheck(bool simulation)
    {
        //Create a list of every attacked tile by the pieces of the player
        List<ChessTileEntity> attackedTiles = new();
        foreach (ChessTileEntity tile in tiles)
        {
            if (tile.piece != null && tile.piece.IsWhite() == whiteTurn)
                attackedTiles.AddRange(ChessEngine.CalculateValidMoves(tile.piece, tile, this));
        }
        
        //If one of the attacked tiles has a king, return true
        foreach (ChessTileEntity tile in attackedTiles)
        {
            if (tile.piece?.pieceType == Piece.King)
                return true;
        }
        
        return false;
    }
    
    //Free the lists and links of the clone and reset the values to let them be deleted
    public void ReleaseClone()
    {
        foreach (ChessTileEntity tile in tiles)
        {
            if (tile.piece != null)
                tile.piece.tile = null;
        }
        tiles.Clear();
        validMoves.Clear();
    }
    
    //Swap the tile and piece values for the move
    public void SwapValuesForMove(ChessPieceEntity piece, ChessTileEntity destination)
    {
        piece.tile.piece = null;
        piece.tile = destination;
        destination.piece = piece;
        piece.movedOnce = true;

        int role = CoopHelperModule.Session.SessionRole;
    }
    
    //API to get a tile by its grid position
    public ChessTileEntity GetTileByGridPosition(int gridPosition)
    {
        if (gridPosition < 0 || gridPosition >= tiles.Count)
            return null;
        
        return tiles[gridPosition];
    }
    
    //API to get a tile by its grid coordinates
    public ChessTileEntity GetTileByGridCoordinates(Vector2 gridCoordinates)
    {
        int gridPosition = (int)gridCoordinates.Y * 8 + (int)gridCoordinates.X;
        return GetTileByGridPosition(gridPosition);
    }
    
    public Player GetPlayer() => player;
    
    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (Instance == this) Instance = null;
    }
    
    //Struct used only when creating the clone, to store the piece type and color of the cloned piece
    struct PieceInfo
    {
        public Piece pieceType;
        public ChessPieceColor ChessPieceColor;

        public PieceInfo(Piece pieceType, ChessPieceColor chessPieceColor)
        {
            this.pieceType = pieceType;
            ChessPieceColor = chessPieceColor;
        }
    }
}


public enum Piece
{
    Pawn = 0,
    Rook = 1, 
    Knight = 2,
    Bishop = 3,
    Queen = 4,
    King = 5
}

public enum ChessPieceColor
{
    White,
    Black
}