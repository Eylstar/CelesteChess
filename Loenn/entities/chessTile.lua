local chess_tile = {}

chess_tile.name = "CelesteChess/ChessTile"
chess_tile.depth = 100000

chess_tile.fieldInformation = {
    color = {
        options = {"Black", "White"},
        editable = false,
    },
}

chess_tile.placements = {
    name = "chessTile",
    data = { 
        _width = 32,
        _height = 32,
        color = "Black",
    },
}

chess_tile.texture = "CelesteChess/BlackTile"
chess_tile.justification = {0.5, 0.5}

function chess_tile.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, 8, 8)
end

return chess_tile