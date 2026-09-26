local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local directory = require("mods").requireFromPlugin("utils.directory")

local defaultTexture = "CorreWithCare/HoldableJumpthru/"

-- 内外层默认染色
local defaultInnerColor = "ffffff99"
local defaultOuterColor = "ffffffff"

-- 取实体的染色，字段留空时回退到默认值；补足 alpha 使六位写法也带完整透明度
local barrierTint = function(entity, attribute, fallback)
    local value = entity[attribute]
    if value == nil or value == "" then
        value = fallback
    end

    local color = utils.getColor(value) or utils.getColor(fallback)

    if color and #color < 4 then
        color[4] = 1
    end

    return color
end

local barrierFieldOrder = function(axis)
    local size = ""
    if axis == "horizontal" then
        size = "width"
    elseif axis == "vertical" then
        size = "height"
    end
    return {
        "x",
        "y",
        size,
        "texture",
        "innerColor",
        "outerColor",
        "depth"
    }
end

-- 取实体的贴图目录，留空时用默认目录；统一补上末尾斜杠以匹配拼接用法
local barrierTexture = function(entity)
    local texture = entity.texture
    if texture == nil or texture == "" then
        texture = defaultTexture
    end

    if texture:sub(-1) ~= "/" then
        texture = texture .. "/"
    end

    return texture
end

local barrierSize = function(entity, axis)
    if axis == "horizontal" then
        return entity.width or 8
    end
    return entity.height or 8
end

local cellSize = 8

-- 素材沿 X 方向切成 A / B / C 三段
local QUAD_INDEX = {A = 0, B = 1, C = 2}

-- 四向排列与单格变换
-- order 为沿排布方向的字母序列：上下朝向沿 X 排布，左右朝向沿 Y 排布
local barrierLayout = {
    up = {order = {"A", "B", "C"}, vertical = false, rotation = 0, flipY = false},
    down = {order = {"A", "B", "C"}, vertical = false, rotation = 0, flipY = true},
    left = {order = {"C", "B", "A"}, vertical = true, rotation = -math.pi / 2, flipY = false},
    right = {order = {"A", "B", "C"}, vertical = true, rotation = math.pi / 2, flipY = false}
}

-- 取排列中第 index 格的字母：首尾取 A / C，中间一律取 B
local barrierLetter = function(layout, index, cells)
    if index == 0 then
        return layout.order[1]
    elseif index == cells - 1 then
        return layout.order[3]
    end
    return layout.order[2]
end

-- 按邻居轴选取纵向两行之一：上下朝向检查左右邻居，左右朝向检查上下邻居
local barrierRow = function(room, entity, direction)
    local horizontal = direction == "up" or direction == "down"
    local size = barrierSize(entity, horizontal and "horizontal" or "vertical")
    local cells = math.max(1, math.floor(size / cellSize))

    local startX = math.floor(entity.x / cellSize) + 1
    local startY = math.floor(entity.y / cellSize) + 1
    local stopX = startX + (horizontal and cells - 1 or 0)
    local stopY = startY + (horizontal and 0 or cells - 1)

    local hasNeighbor
    if horizontal then
        hasNeighbor = room.tilesFg.matrix:get(startX - 1, startY, "0") ~= "0"
            or room.tilesFg.matrix:get(stopX + 1, stopY, "0") ~= "0"
    else
        hasNeighbor = room.tilesFg.matrix:get(startX, startY - 1, "0") ~= "0"
            or room.tilesFg.matrix:get(stopX, stopY + 1, "0") ~= "0"
    end

    return hasNeighbor and 0 or cellSize
end

-- 单格绘制：以格子中心为旋转轴心，使旋转与翻转后内容落回同一格
local barrierCellSprite = function(texture, color, entity, position, letter, row, layout)
    local sprite = drawableSprite.fromTexture(texture, entity)
    sprite:setJustification(0, 0)
    sprite:useRelativeQuad(QUAD_INDEX[letter] * cellSize, row, cellSize, cellSize)
    sprite:setColor(color)

    -- 轴心取内容中心，绘制点取格子中心
    sprite:setOffset(cellSize / 2, cellSize / 2)
    sprite:setPosition(position.x + cellSize / 2, position.y + cellSize / 2)

    sprite.rotation = layout.rotation
    if layout.flipY then
        sprite.scaleY = -1
    end

    return sprite
end

local barrierSprite = function(room, entity, direction)
    local texture = barrierTexture(entity)
    local layout = barrierLayout[direction]
    local horizontal = not layout.vertical

    local size = barrierSize(entity, horizontal and "horizontal" or "vertical")
    local cells = math.max(1, math.floor(size / cellSize))
    local row = barrierRow(room, entity, direction)
    local innerTint = barrierTint(entity, "innerColor", defaultInnerColor)
    local outerTint = barrierTint(entity, "outerColor", defaultOuterColor)

    local sprites = {}

    for index = 0, cells - 1 do
        local letter = barrierLetter(layout, index, cells)

        -- 起始格左上角对齐实体位置，其余格沿排布轴依次偏移
        local position = {x = entity.x, y = entity.y}
        if horizontal then
            position.x = entity.x + cellSize * index
        else
            position.y = entity.y + cellSize * index
        end

        table.insert(sprites, barrierCellSprite(texture .. "inner00", innerTint, entity, position, letter, row, layout))
        table.insert(sprites, barrierCellSprite(texture .. "outer00", outerTint, entity, position, letter, row, layout))
    end

    return sprites
end

local barrierSelection = function(entity, axis)
    if axis == "horizontal" then
        return utils.rectangle(entity.x, entity.y, entity.width or 8, 8)
    end
    return utils.rectangle(entity.x, entity.y, 8, entity.height or 8)
end

-- 贴图目录字段：选择目录内的贴图文件，自动取所属文件夹并保留末尾斜杠，
-- 以匹配实体侧按目录名拼接材质文件名的用法
local barrierTextureField = directory.gameplayPath(true, false)
local barrierBaseProcessor = barrierTextureField.filenameProcessor

barrierTextureField.filenameProcessor = function(filename, rawFilename, prefix)
    local path = barrierBaseProcessor(filename, rawFilename, prefix)

    if path == nil or path == "" then
        return path
    end

    -- 只保留所属文件夹部分，与实体侧 texture .. "inner00" 的拼接方式对应
    local folder = path:match("^(.*)/[^/]+$") or path

    return folder .. "/"
end

local barrierFieldInformation = {
    -- 贴图目录，留空时使用默认目录
    texture = barrierTextureField,
    -- 内层与外层材质染色
    innerColor = {
        fieldType = "color",
        useAlpha = true
    },
    outerColor = {
        fieldType = "color",
        useAlpha = true
    },
    depth = require("mods").requireFromPlugin("utils.setups").depths,
}

-- 朝向索引表：轮转顺序为上、右、下、左，数值即顺时针步进
local barrierOrder = {
    "up",
    "right",
    "down",
    "left",

    up = 1,
    right = 2,
    down = 3,
    left = 4
}

local barrierNames = {
    up = "CorreWithCare/HoldableJumpthruUp",
    right = "CorreWithCare/HoldableJumpthruRight",
    down = "CorreWithCare/HoldableJumpthruDown",
    left = "CorreWithCare/HoldableJumpthruLeft"
}

-- 按朝向取在索引表中的序号，结果恒为 1 至 4
local barrierIndex = function(direction)
    local index = barrierOrder[direction] or 1
    local m = index % 4
    return m == 0 and 4 or m
end

local makeBarrier = function(direction)
    local horizontal = direction == "up" or direction == "down"
    local axis = horizontal and "horizontal" or "vertical"

    local obstacle = {
        name = barrierNames[direction],
        placements = {
            name = direction,
            data = horizontal
                and {width = 8, innerColor = defaultInnerColor, outerColor = defaultOuterColor, depth = 0,}
                or {height = 8, innerColor = defaultInnerColor, outerColor = defaultOuterColor, depth = 0,}
        },
        fieldInformation = barrierFieldInformation,
        fieldOrder = barrierFieldOrder(axis),
        sprite = function(room, entity)
            return barrierSprite(room, entity, direction)
        end,
        selection = function(room, entity)
            return barrierSelection(entity, axis)
        end
    }

    -- 旋转时在四个朝向间切换；换轴时宽高互换
    obstacle.rotate = function(room, entity, rotationDirection)
        local sideIndex = barrierIndex(direction)
        local targetIndex = barrierIndex(barrierOrder[((sideIndex - 1 + (rotationDirection or 1)) % 4) + 1])

        if sideIndex == targetIndex then
            return false
        end

        entity._name = barrierNames[barrierOrder[targetIndex]]

        if sideIndex % 2 ~= targetIndex % 2 then
            entity.width, entity.height = entity.height, entity.width
        end

        return true
    end

    if horizontal then
        obstacle.canResize = {true, false}
    else
        obstacle.canResize = {false, true}
    end

    return obstacle
end

local barriers = {
    makeBarrier("up"),
    makeBarrier("right"),
    makeBarrier("down"),
    makeBarrier("left")
}

return barriers
