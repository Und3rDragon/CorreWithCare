local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local controller = {}

controller.name = "CorreWithCare/CustomCredits"

controller.placements = {
    name = "credits",
    data = {
        xPosition = 0.5,
        alignment = 0.5,
        scale = 1,
        spacing = 10,
        scrollTime = 60,
        scrollOffScreen = true,
        allowInput = true,
        headingColor = "ffffffff",
        subtitleColor = "808080ff",
        textColor = "ffffffff",
        outlineColor = "000000ff",
        edgeColor = "483d88ff",
        headingScale = 2.5,
        subtitleScale = 0.9,
        textScale = 1.4,
        dialogKey = "",
        inlineText = "",
        depth = -2000000,
    }
}

controller.ignoredFields = {
    "_x", "_y", "x", "y"
}

controller.fieldInformation = 
{
    headingColor = {
        fieldType = "color",
        useAlpha = true,
    },
    subtitleColor = {
        fieldType = "color",
        useAlpha = true,
    },
    textColor = {
        fieldType = "color",
        useAlpha = true,
    },
    outlineColor = {
        fieldType = "color",
        useAlpha = true,
    },
    edgeColor = {
        fieldType = "color",
        useAlpha = true,
    },
    depth = require("mods").requireFromPlugin("utils.setups").depths
}

function controller.sprite(room, entity)
    local sprite = {}
    local rect = drawableRectangle.fromRectangle("fill", entity.x, entity.y, 16, 16, {0.0, 0.0, 0.0})
    local iconSprite = drawableSprite.fromTexture("CorreWithCare/LoennIcons/Credits", entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return controller