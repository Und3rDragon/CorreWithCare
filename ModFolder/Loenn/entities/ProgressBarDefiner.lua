local drawableSprite = require("structs.drawable_sprite")

local marker = {}

marker.name = "CorreWithCare/ProgressBarDefiner"

marker.placements = {
    name = "marker",
    data = {
        isCounter = true,
        sessionName = "default",
        direction = 0,
        moveDuration = 1,
        targetOffset = 108,
        sessionBorder1 = 0,
        sessionBorder2 = 100,
        size = 1536,
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
    -- 0/1 为水平进度条（从上/下方进出），2/3 为垂直进度条（从左/右侧进出）
    direction = {
        fieldType = "integer",
        options = {
            ["From Top"] = 0,
            ["From Bottom"] = 1,
            ["From Left"] = 2,
            ["From Right"] = 3,
        },
        editable = false,
    },
}

function marker.sprite(room, entity)
    local sprite = {}
    local iconSprite = drawableSprite.fromTexture("CorreWithCare/LoennIcons/Marker", entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return marker
