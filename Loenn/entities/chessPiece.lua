local chess_piece = {}

chess_piece.name = "CelesteChess/ChessPiece"
chess_piece.depth = -10

local function getTexture(entity)
    return entity.piece or "Pawn"
end

chess_piece.fieldInformation = {
    piece = {
        options = {"Pawn", "Knight", "Bishop", "Rook", "Queen", "King"},
        editable = false,
    },
    color = {
        options = {"White", "Black"},
        editable = false,
    },
}

chess_piece.placements = {
    name = "chessPiece",
    data = {
        _width = 24,
        _height = 24,
        piece = "Pawn",
        color = "White",
    }
}

chess_piece.texture = function(entity)
    local textureRaw = getTexture(entity)
    local texture = "CelesteChess/pieces/" .. textureRaw
    return texture
end

return chess_piece