local drawableSprite = require("structs.drawable_sprite")
local directory = require("mods").requireFromPlugin("utils.directory")

local marker = {}

marker.name = "CorreWithCare/VerticalProgressBarDefiner"

marker.placements = {
    name = "marker",
    data = {
        isCounter = true,
        sessionName = "default",
        fromLeft = true,
        moveDuration = 1,
        targetOffsetX = 192,
        sessionBorder1 = 0,
        sessionBorder2 = 100,
        sizeY = 864,
        fontSizeX = 1,
        fontSizeY = 1,
        barSize = 10,
        fontColor = "ffffffff",
        barColor = "ffffffff",
        gapX = 10,
        gapY = 10,
        shadowShiftX = 0,
        shadowShiftY = 8,
        titleName = "",
        titleScaleX = 1,
        titleScaleY = 1,
        titleColor = "ffffffff",
        flag = "",
        titleOnBottom = true,
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
    titleColor = {
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
