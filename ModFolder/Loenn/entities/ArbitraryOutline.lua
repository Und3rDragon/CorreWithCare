local dR = require("structs.drawable_rectangle")
local dF = require("structs.drawable_function")
local dL = require("structs.drawable_line")
local drawing = require("utils.drawing")
local utils = require("utils")
local vh = require("viewport_handler")

local DEG_TO_RAD = 0.0174532925

local aseEnt = {
    name = "CorreWithCare/ArbitraryOutline",
    nodeLimits = {0, 999},
    depth = -math.huge + 6
}

aseEnt.placements = {
    name = "Arbitrary Color Fill",
    data = {
        color = "FFFFFF",
        depth = 0,
        effect = "",
        markerEffectPixels = 0.4,
        markerInterval = 0.6,
        windingOrder = "Auto"
    }
}

aseEnt.fieldInformation = {
    color = {
        fieldType = "color"
    },
    effect = {
        fieldType = "string",
        options = {
            "",
            "Marker"
        }
    },
    windingOrder = {
        fieldType = "string",
        options = {
            "Auto",
            "Clockwise",
            "Counter Clockwise"
        }
    }
}

function drawFilledPolygon(pt, fillColor)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(fillColor)
        local ok, triangles = pcall(love.math.triangulate, pt)
        if not ok then return end

        for _,triangle in ipairs(triangles) do
            love.graphics.polygon("fill", triangle)
        end
    end)
end

function getFilledRectangleSprite(rectangle, fillColor)
    return dR.fromRectangle("fill", rectangle.x, rectangle.y, rectangle.width, rectangle.height, fillColor or {1,1,1,1}):getDrawableSprite()
end

function point(position, color)
    return getFilledRectangleSprite(utils.rectangle(position.x - 1, position.y - 1, 3, 3), color)
end

function aseEnt.sprite(room, entity)
    local sprites = {}

    local nodeColor = {1,1,1,1}
    local success, r, g, b = utils.parseHexColor(entity.color)
    local lineColor = {r,g,b,1}
    local fillColor = {lineColor[1], lineColor[2], lineColor[3], lineColor[4]/2.5}

    local points = {entity.x+0.5, entity.y+0.5}
    local nodeSprites = { point(entity, {1, 0, 0, 1})}

    if entity.nodes then
        for _,value in ipairs(entity.nodes) do
            table.insert(points, value.x+0.5)
            table.insert(points, value.y+0.5)
    
            table.insert(nodeSprites, point(value, nodeColor))
        end
    end

    local sprites = {}

    if #entity.nodes>=2 then
        table.insert(points, entity.x+0.5)
        table.insert(points, entity.y+0.5)
        table.insert(sprites, dF.fromFunction(drawFilledPolygon, points, fillColor))
    end

    if #entity.nodes>=1 then
        table.insert(sprites, dL.fromPoints(points, lineColor, 0.5))
    end

    for _,v in ipairs(nodeSprites) do
        table.insert(sprites, v)
    end

    return sprites
end

function rotateVector(vectorTab, delta)
    local x = vectorTab.x
    local y = vectorTab.y

    return {
        ["x"] = x * math.cos(delta) - y * math.sin(delta),
        ["y"] = x * math.sin(delta) + y * math.cos(delta)
    }
end

function aseEnt.rotate(room,entity,direction)
    if entity.nodes then
        for i=1,#entity.nodes do
            local node = entity.nodes[i]

            local x = node.x
            local y = node.y

            local dX = node.x - entity.x
            local dY = node.y - entity.y

            local offsetdir = rotateVector({["x"] = dX, ["y"] = dY}, 5/57.2958)

            node.x = entity.x + offsetdir.x
            node.y = entity.y + offsetdir.y
        end
    end

    return true
end

function aseEnt.nodeSprite() end

function aseEnt.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, 1, 1)

    if entity.nodes then
        local nodeSelections = {}
        for _,node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x, node.y, 1, 1))
        end
        return main, nodeSelections
    end

    return main, {}
end

function aseEnt.nodeAdded(room, entity, node)
    local mx, my = love.mouse.getPosition()
    local nodeX, nodeY = vh.getRoomCoordinates(room, mx, my)

    local nodes = entity.nodes

    if node==0 then
        table.insert(nodes, 1, {x = nodeX, y = nodeY})
    else
        table.insert(nodes, node + 1, {x = nodeX, y = nodeY})
    end

    return true
end

return aseEnt