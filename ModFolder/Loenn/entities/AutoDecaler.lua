local dR = require("structs.drawable_rectangle")
local dF = require("structs.drawable_function")
local dL = require("structs.drawable_line")
local drawing = require("utils.drawing")
local utils = require("utils")
local vh = require("viewport_handler")
local directory = require("mods").requireFromPlugin("utils.directory")

local decaler = {}

decaler.name = "CorreWithCare/AutoDecaler"
decaler.nodeLimits = { 2, 999 }
decaler.depth = -10500

-- 单条 Decal 路径的选择器：选中文件后转换为模组内的 Gameplay 贴图路径
local decalPath = directory.gameplayPath(true, true)

decaler.placements = {
    name = "autoDecaler",
    data = {
        paths = "",
        density = 1,
        scatterness = 0.7,
        gridX = -1,
        gridY = -1,
        randomizeFrame = false,
        depth = -10500,
    }
}

decaler.fieldInformation = {
    paths = {
        fieldType = "list_scroll",
        minimumElements = 0,
        elementOptions = decalPath,
    },
    density = {
        fieldType = "number",
        allowEmpty = false,
        minimumValue = 0,
    },
    scatterness = {
        fieldType = "number",
        allowEmpty = false,
        minimumValue = 0,
        maximumValue = 1,
    },
    depth = require("mods").requireFromPlugin("utils.setups").depths,
}

decaler.fieldOrder = {
    "x",
    "y",
    "paths",
    "density",
    "scatterness",
    "gridX",
    "gridY",
    "randomizeFrame",
    "depth",
}

local FILTER_COLOR = { 0.63, 0.86, 0.78, 1 }

local getFilledRectangleSprite = function(rectangle, fillColor)
    return dR.fromRectangle("fill", rectangle.x, rectangle.y, rectangle.width, rectangle.height, fillColor or {1,1,1,1}):getDrawableSprite()
end

local point = function(position, color)
    return getFilledRectangleSprite(utils.rectangle(position.x - 1, position.y - 1, 3, 3), color)
end

local drawFilledPolygon = function(points, fillColor)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(fillColor)
        local ok, triangles = pcall(love.math.triangulate, points)
        if not ok then return end

        for _, triangle in ipairs(triangles) do
            love.graphics.polygon("fill", triangle)
        end
    end)
end

function decaler.sprite(room, entity)
    local sprites = {}

    local nodeColor = {1,1,1,1}
    local fillColor = {FILTER_COLOR[1], FILTER_COLOR[2], FILTER_COLOR[3], FILTER_COLOR[4] / 2.5}

    local points = {entity.x+0.5, entity.y+0.5}
    local nodeSprites = { point(entity, {1, 0, 0, 1})}

    if entity.nodes then
        for _,value in ipairs(entity.nodes) do
            table.insert(points, value.x+0.5)
            table.insert(points, value.y+0.5)

            table.insert(nodeSprites, point(value, nodeColor))
        end
    end

    if #entity.nodes>=2 then
        table.insert(points, entity.x+0.5)
        table.insert(points, entity.y+0.5)
        table.insert(sprites, dF.fromFunction(drawFilledPolygon, points, fillColor))
    end

    if #entity.nodes>=1 then
        table.insert(sprites, dL.fromPoints(points, FILTER_COLOR, 0.5))
    end

    for _,v in ipairs(nodeSprites) do
        table.insert(sprites, v)
    end

    return sprites
end

function decaler.selection(room, entity)
    local main = utils.rectangle(entity.x - 4, entity.y - 4, 8, 8)

    if entity.nodes then
        local nodeSelections = {}
        for _,node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x - 4, node.y - 4, 8, 8))
        end
        return main, nodeSelections
    end

    return main, {}
end

function decaler.nodeAdded(room, entity, node)
    local mx, my = love.mouse.getPosition()
    local nodeX, nodeY = vh.getRoomCoordinates(room, mx, my)

    local nodes = entity.nodes

    if node == 0 then
        table.insert(nodes, 1, {x = nodeX, y = nodeY})
    else
        table.insert(nodes, node + 1, {x = nodeX, y = nodeY})
    end

    return true
end

function decaler.nodeSprite() end

return decaler
