local drawableSprite = require("structs.drawable_sprite")
local directory = require("mods").requireFromPlugin("utils.directory")

local marker = {}

marker.name = "CorreWithCare/ProgressBarDefiner"

marker.placements = {
    name = "marker",
    data = {
        isCounter = true,
        sessionName = "default",
        fromTop = true,
        moveDuration = 1,
        targetOffsetY = 108,
        sessionBorder1 = 0,
        sessionBorder2 = 100,
        sizeX = 1536,
        fontSizeX = 1,
        fontSizeY = 1,
        barSize = 10,
        fontColor = "ffffffff",
        barColor = "ffffffff",
        gapX = 10,
        gapY = 10,
    }
}

marker.fieldInformation = {
    fontColor = {
        fieldType = "color",
        useAlpha = true,
    },
    barColor = {
        fieldType = "color",
        useAlpha = true,
    },
}

function marker.sprite(room, entity)
    local sprite = {}
    local iconSprite = drawableSprite.fromTexture("CorreWithCare/LoennIcons/Marker", entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return marker
