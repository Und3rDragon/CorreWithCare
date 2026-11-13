local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local entities = require("entities")

local moreCustomNpc = {}

moreCustomNpc.name = "CorreWithCare/GossipNPC"
moreCustomNpc.depth = 100
moreCustomNpc.justification = { 0.5, 1.0 }
moreCustomNpc.nodeLimits = { 0, 2 }
moreCustomNpc.nodeLineRenderType = "line"
moreCustomNpc.fieldInformation = {
    spriteRate = {
        fieldType = "integer",
    },
    approachDistance = {
        fieldType = "integer",
    },
    indicatorOffsetX = {
        fieldType = "integer",
    },
    indicatorOffsetY = {
        fieldType = "integer",
    }
}

moreCustomNpc.placements = {
    name = "npc",
    data = {
        spriteName = "theo/idle",
        mainDialogIds = "",
        gossipDialogIds = "",
        --onlyOnce = true,
        endLevel = false,
        flipX = false,
        flipY = false,
        approachWhenTalking = false,
        approachDistance = 16,
        indicatorOffsetX = 0,
        indicatorOffsetY = 0,
        onlyIfFlag = "",
        setFlag = "",
        onlyIfFlagInverted = false,
        setFlagInverted = false,
        customFont = ""
    }
}

local borderColor = { 1.0, 1.0, 1.0, 1.0 }
local fillColor = { 1.0, 1.0, 1.0, 0.8 }

function moreCustomNpc.nodeSprite(room, entity, node, index)
    local rectangle = utils.rectangle(node.x, node.y, 8, 8)
    return drawableRectangle.fromRectangle("bordered", rectangle, fillColor, borderColor):getDrawableSprite()
end

function moreCustomNpc.scale(room, entity)
    local scaleX = entity.flipX and -1 or 1
    local scaleY = entity.flipY and -1 or 1

    return scaleX, scaleY
end

function moreCustomNpc.texture(room, entity)
    local spriteName = entity.spriteName or ""

    if spriteName == "oshiro/oshiro" then
        return "characters/oshiro/oshiro31"
    end

    return "characters/player/idle00"
end

return moreCustomNpc
