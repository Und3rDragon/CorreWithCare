local drawableSprite = require("structs.drawable_sprite")

local controller = {}

controller.name = "CorreWithCare/LightningRendererController"
--controller.fillColor = { 0.55, 0.97, 0.96, 0.4 }
--controller.borderColor = { 0.99, 0.96, 0.47, 1.0 }

controller.placements = {
    {
        name = "map",
        data = {
            mode = 0,
            targetIDs = "",
            colorA = "fcf579ff",
            colorB = "8cf7e2ff",
            scaleB = 0.35,
            edgeThickness = 1.0,
            fillColor = "f7b2621a",
            seedInterval = 0.1,
            flashStrength = 1.0,
            glitchStrength = 0.15,
            bloomStrength = 0.5,
            ambience = "event:/new_content/env/10_electricity",
            ambienceVolume = 1.0,
            depth = -1000100,
            drawEdges = true,
            drawBody = true
        }
    },
    {
        name = "room",
        data = {
            mode = 1,
            targetIDs = "",
            colorA = "fcf579ff",
            colorB = "8cf7e2ff",
            scaleB = 0.35,
            edgeThickness = 1.0,
            fillColor = "f7b2621a",
            seedInterval = 0.1,
            flashStrength = 1.0,
            glitchStrength = 0.15,
            bloomStrength = 0.5,
            ambience = "event:/new_content/env/10_electricity",
            ambienceVolume = 1.0,
            depth = -1000100,
            drawEdges = true,
            drawBody = true
        }
    },
    {
        name = "place",
        data = {
            mode = 2,
            targetIDs = "",
            colorA = "fcf579ff",
            colorB = "8cf7e2ff",
            scaleB = 0.35,
            edgeThickness = 1.0,
            fillColor = "f7b2621a",
            seedInterval = 0.1,
            flashStrength = 1.0,
            glitchStrength = 0.15,
            bloomStrength = 0.5,
            ambience = "event:/new_content/env/10_electricity",
            ambienceVolume = 1.0,
            depth = -1000100,
            drawEdges = true,
            drawBody = true
        }
    },
    {
        name = "ids",
        data = {
            mode = 3,
            targetIDs = "",
            colorA = "fcf579ff",
            colorB = "8cf7e2ff",
            scaleB = 0.35,
            edgeThickness = 1.0,
            fillColor = "f7b2621a",
            seedInterval = 0.1,
            flashStrength = 1.0,
            glitchStrength = 0.15,
            bloomStrength = 0.5,
            ambience = "event:/new_content/env/10_electricity",
            ambienceVolume = 1.0,
            depth = -1000100,
            drawEdges = true,
            drawBody = true
        }
    },
}

controller.fieldInformation = {
    colorA = {
        fieldType = "color",
        useAlpha = true
    },
    colorB = {
        fieldType = "color",
        useAlpha = true
    },
    fillColor = {
        fieldType = "color",
        useAlpha = true
    },
    depth = require("mods").requireFromPlugin("utils.setups").depths,
    mode = {
        fieldType = "integer",
        options = {
            ["On Map"] = 0,
            ["Per Room"] = 1,
            ["Placed"] = 2,
            ["Target IDs"] = 3,
        },
        editable = false,
    },
}

controller.fieldOrder = {
    
}

function controller.sprite(room, entity)
    local sprite = {}
    local texture
    if tonumber(entity.mode) == 2 then
        texture = "CorreWithCare/LoennIcons/LightningB"
    else
        texture = "CorreWithCare/LoennIcons/LightningA"
    end
    local iconSprite = drawableSprite.fromTexture(texture, entity)

    table.insert(sprite, iconSprite)
    return sprite
end

return controller
