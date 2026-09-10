local drawableSprite = require("structs.drawable_sprite")
local directory = require("mods").requireFromPlugin("utils.directory")

local marker = {}

marker.name = "CorreWithCare/CollectibleCoinMarker"

local defaultIcon = "CorreWithCare/coin"

marker.placements = {
    name = "marker",
    data = {
        tag = "default",
        icon = defaultIcon
    }
}

marker.fieldInformation = {
    -- 计数器图标，选择文件后自动转换为模组内的 Gui 贴图路径
    icon = directory.guiPath(false)
}

function marker.sprite(room, entity)
    local sprite = {}
    local iconSprite = drawableSprite.fromTexture("CorreWithCare/LoennIcons/Marker", entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return marker
