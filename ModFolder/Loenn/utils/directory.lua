local directory = {}

--- 统一路径处理字段：把编辑器选择的完整贴图路径转成模组内的相对路径。
-- 支持多级前缀匹配降级，能容忍用户选到任意层级的文件。
-- @param trimPath      string: 需要移除的路径前缀，如 "Graphics/Atlases/Gameplay/"
-- @param ignoreNumbers boolean: 是否移除文件名结尾的数字（xxx00 -> xxx）
-- @param ignoreSuffix  boolean: 是否移除扩展名（xxx00.png -> xxx00）
-- @param empty         boolean: 是否允许空字符串
function directory.versatilePath(trimPath, ignoreNumbers, ignoreSuffix, empty)
    return {
        fieldType = "path",
        allowEmpty = empty == true,
        allowFiles = true,
        allowFolders = true,
        filenameProcessor = function(filename, rawFilename, prefix)
            -- 1. 基础清理
            local str = (filename or "")
            str = str:gsub("^%s+", ""):gsub("%s+$", "")

            if str == "" then
                return empty and "" or nil
            end

            -- 2. 移除路径前缀
            local path = str

            if trimPath and trimPath ~= "" then
                if trimPath == "/" or trimPath == "\\" then
                    if str:sub(1, 1) == trimPath then
                        path = str:sub(2)
                    end
                else
                    local trimmed = trimPath
                    if trimmed:sub(-1) ~= "/" then
                        trimmed = trimmed .. "/"
                    end

                    local prefixLen = #trimmed

                    -- 方法1：直接匹配完整前缀
                    if #str >= prefixLen and str:sub(1, prefixLen) == trimmed then
                        path = str:sub(prefixLen + 1)
                    else
                        -- 方法2：从后往前匹配最后一段路径
                        local lastPart = trimPath:match("([^/]+)/?$")

                        if lastPart then
                            local searchStr = lastPart .. "/"
                            local pos = str:find(searchStr, 1, true)

                            if pos and (pos == 1 or str:sub(pos - 1, pos - 1) == "/") then
                                path = str:sub(pos + #searchStr)
                            end
                        end

                        -- 方法3：逐段向前回退匹配
                        if path == str and trimPath:match("^.+//") then
                            local segments = {}

                            for segment in (trimPath:gmatch("([^/]+)/")) do
                                table.insert(segments, segment)
                            end

                            for i = #segments, 1, -1 do
                                local searchStr = segments[i] .. "/"
                                local pos = str:find(searchStr, 1, true)

                                if pos and (pos == 1 or str:sub(pos - 1, pos - 1) == "/") then
                                    path = str:sub(pos + #searchStr)
                                    break
                                end
                            end
                        end
                    end
                end
            end

            if path == "" and not empty then
                return str
            end

            if path == "" then
                return ""
            end

            -- 3. 目录路径直接返回（去掉末尾斜杠）
            if path:sub(-1) == "/" then
                return path:sub(1, -2)
            end

            -- 4. 拆分文件名与扩展名
            local hasExtension = path:match("%.[^/%.]+$") ~= nil
            local name = path
            local ext = ""

            if hasExtension then
                ext = path:match("%.([^%.]+)$") or ""
                name = path:match("(.+)%.[^%.]+$") or path
            end

            -- 5. 移除文件名结尾的数字（不影响目录部分）
            if ignoreNumbers then
                local dirPart = name:match("^(.*)/")

                if dirPart then
                    local fileName = name:sub(#dirPart + 2)
                    name = dirPart .. "/" .. fileName:gsub("%d+$", "")
                else
                    name = name:gsub("%d+$", "")
                end
            end

            -- 6. 按需保留或去掉扩展名
            if ignoreSuffix or ext == "" then
                return name
            end

            return name .. "." .. ext
        end
    }
end

--- Gameplay 图集贴图路径字段（实体自身贴图等）。
-- 例：选中 .../Gameplay/CorreWithCare/entities/coin/idle00.png → "CorreWithCare/entities/coin/idle"
function directory.gameplayPath(empty, ignoreNumbers)
    return directory.versatilePath("Graphics/Atlases/Gameplay/", ignoreNumbers == true, true, empty)
end

--- Gui 图集贴图路径字段（HUD 图标等）。
-- 例：选中 .../Gui/CorreWithCare/coin.png → "CorreWithCare/coin"
function directory.guiPath(empty)
    return directory.versatilePath("Graphics/Atlases/Gui/", false, true, empty)
end

return directory
