local drawableSprite = require("structs.drawable_sprite")
local directory = require("mods").requireFromPlugin("utils.directory")

local coin = {}

coin.name = "CorreWithCare/CollectibleCoin"
coin.nodeLineRenderType = "line"
coin.nodeLimits = { 0, 2 }
coin.texture = "CorreWithCare/entities/collectibleCoin/idle00"

local defaultSprite = "CorreWithCare/entities/collectibleCoin/idle"
local defaultSfx = "event:/gddcoin/key_get"

coin.placements = {
    {
        name = "coin",
        data = {
            tag = "default",
            value = 1,
            sprite = defaultSprite,
            sfx = defaultSfx,
            persist = false
        }
    },
    {
        name = "coin_with_return",
        placementType = "point",
        data = {
            tag = "default",
            value = 1,
            sprite = defaultSprite,
            sfx = defaultSfx,
            persist = false,
            nodes = {
                { x = 0, y = 0 },
                { x = 0, y = 0 }
            }
        }
    }
}

coin.fieldInformation = {
    value = {
        fieldType = "integer",
        minimumValue = 0
    },
    -- 金币贴图目录，选择文件后自动转换为模组内的 Gameplay 贴图路径
    sprite = directory.gameplayPath(false, true)
}

function coin.nodeLimits(room, entity)
    local nodes = entity.nodes or {}

    if #nodes > 0 then
        return 2, 2
    end

    return 0, 0
end

function coin.sprite(room, entity)
    local sprite = {}
    local iconSprite = drawableSprite.fromTexture(defaultSprite .. "00", entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return coin
