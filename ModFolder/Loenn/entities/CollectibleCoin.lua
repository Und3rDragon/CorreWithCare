local drawableSprite = require("structs.drawable_sprite")
local directory = require("mods").requireFromPlugin("utils.directory")

local coin = {}

coin.name = "CorreWithCare/CollectibleCoin"
coin.nodeLineRenderType = "line"
coin.nodeLimits = { 0, 2 }
coin.texture = "CorreWithCare/CollectibleCoin/idle00"

local defaultSprite = "corre_CollectibleCoin"
local defaultSpriteImage = "CorreWithCare/CollectibleCoin/idle"
local defaultSfx = "event:/gddcoin/key_get"

coin.placements = {
    {
        name = "coin",
        data = {
            tag = "default",
            value = 1,
            sprite = defaultSprite,
            sfx = defaultSfx,
            
            collectOffsetY = -12,
            collectOffsetDuration = 1.25,
            shrinkX = true,
            existence = 1,
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
            nodes = {
                { x = 0, y = 0 },
                { x = 0, y = 0 }
            },

            collectOffsetY = -12,
            collectOffsetDuration = 1.25,
            shrinkX = true,
            existence = 1,
        }
    }
}

coin.fieldInformation = {
    value = {
        fieldType = "integer",
        minimumValue = 0
    },
    -- 金币贴图目录，选择文件后自动转换为模组内的 Gameplay 贴图路径
    sprite = directory.gameplayPath(false, true),
    existence = {
        options = {
            ["Persistent"] = 0,
            ["Normal"] = 1,
            ["Once Per Map"] = 2,
        },
        fieldType = "integer",
    },
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
    local iconSprite = drawableSprite.fromTexture(defaultSpriteImage .. "00", entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return coin
