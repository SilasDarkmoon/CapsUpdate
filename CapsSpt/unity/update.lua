local UnityEngine = clr.UnityEngine
local Application = UnityEngine.Application
local PlatDependant = clr.Capstones.UnityEngineEx.PlatDependant

local update = {}

function update.update(funcComplete, funcReport)
    if not ___CONFIG__UPDATE_ERR_CODES then
        ___CONFIG__UPDATE_ERR_CODES = {}
    end
    if not ___CONFIG__UPDATE_ERR_CODES[404] then
        ___CONFIG__UPDATE_ERR_CODES[404] = true
    end

    local ignoreUpdate = false
    if Application.isEditor then
        ignoreUpdate = not ___CONFIG__TEST_UPDATE_IN_EDITOR
    else
        ignoreUpdate = ___CONFIG__IGNORE_UPDATE
    end

    if not ___CONFIG__UPDATE_FILE_EXT_HANDLERS then
        ___CONFIG__UPDATE_FILE_EXT_HANDLERS = {}
    end
    if not ___CONFIG__UPDATE_FILE_EXT_HANDLERS[".obb"] then
        ___CONFIG__UPDATE_FILE_EXT_HANDLERS[".obb"] = {
            download = true,
            checkfunc = "zip",
            applyfunc = function(key, zippath, v)
                local obbname = key
                if string.sub(key, 1, 4) == "obb-" then
                    obbname = string.sub(key, 5, -1)
                end

                local obbpath = clr.updatepath.."/obb/"..obbname.."."..clr.Capstones.UnityEngineEx.ResManager.AppVer..".obb"
                clr.Capstones.UnityEngineEx.PlatDependant.MoveFile(zippath, obbpath)
                return true
            end,
        }
    end

    -- if ignoreUpdate and req.reportVersion then
    --     -- when we ignore update, we should use another request to report our versions to server in order to do version check.
    --     req.reportVersion(nil, { [404] = true })
    -- end
    if not ignoreUpdate and req.checkVersion then
        local resp = req.checkVersion(nil, ___CONFIG__UPDATE_ERR_CODES)
        if api.success(resp) then
            if ___CONFIG__UPDATE_EX_HANDLERS and resp.val.ex then
                for k, v in pairs(resp.val.ex) do
                    if ___CONFIG__UPDATE_EX_HANDLERS[k] then
                        ___CONFIG__UPDATE_EX_HANDLERS[k](v)
                    end
                end
            end

            local version = resp.val.update
            if type(version) == "table" then
                local cvtable = _G["___resver"]
                if type(cvtable) ~= "table" then
                    cvtable = {}
                    _G["___resver"] = cvtable
                end
                local newres = {}
                for i, v in ipairs(version) do
                    if type(v) == "table" then
                        local key = v.key
                        local ver = v.ver
                        local url = v.url
                        if type(url) == "string" and type(key) == "string" and type(ver) == "number" then
                            if ver < 0 or tonumber(cvtable[key]) < ver then
                                if not funcReport or not funcReport("filter", key) then
                                    newres[#newres + 1] = v
                                end
                            end
                        elseif type(ver) == "string" then
                            newres[#newres + 1] = v
                        end
                    end
                end
                if #newres > 0 then
                    local totallen = 0
                    local quiet = true
                    for i, v in ipairs(newres) do
                        local len = tonumber(v.len)
                        totallen = totallen + len
                        if not v.quiet then
                            quiet = false
                        end
                    end
                    if funcReport then
                        local waitHandle = funcReport("cnt", #newres, totallen, quiet)
                        if type(waitHandle) == "table" then
                            while waitHandle.waiting do
                                unity.waitForNextEndOfFrame()
                            end
                        end
                    end
                    for i, v in ipairs(newres) do
                        local key = v.key
                        local ver = v.ver
                        local url = v.url
                        local len = tonumber(v.len)
                        local itemsuccess = false
                        local retry_wait = 450

                        local updateFileIndex = 0
                        while not itemsuccess do
                            if funcReport then
                                funcReport("prog", i)
                                funcReport("key", key)
                                funcReport("ver", ver)
                            end

                            while retry_wait < 450 do
                                retry_wait = retry_wait + 1
                                unity.waitForNextEndOfFrame()
                            end
                            retry_wait = 0

                            if type(ver) == "string" then
                                if ___CONFIG__UPDATE_TYPED_ITEM_HANDLERS and ___CONFIG__UPDATE_TYPED_ITEM_HANDLERS[ver] then
                                    ___CONFIG__UPDATE_TYPED_ITEM_HANDLERS[ver](key, v)
                                end
                                itemsuccess = true
                            else
                                local extindex, ext
                                while true do
                                    local index = string.find(url, ".", (extindex or 0) + 1, true)
                                    if index then
                                        extindex = index
                                    else
                                        break
                                    end
                                end
                                if extindex then
                                    ext = string.sub(url, extindex)
                                end
                                ext = ext or ""
                                if ext == ".zip" or type(___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext]) == "table" and ___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext].download then
                                    dump(v)
                                    local zippath = Application.temporaryCachePath.."/download/update"..updateFileIndex..".zip"
                                    local enablerange = false
                                    local rangefile = zippath..'.url'
                                    local rangestream = PlatDependant.OpenReadText(rangefile)
                                    if rangestream and rangestream ~= clr.null then
                                        local ourl = rangestream:ReadLine()
                                        rangestream:Dispose()
                                        if ourl == url then
                                            if PlatDependant.IsFileExist(zippath) then
                                                enablerange = true
                                                dump('range enabled.')
                                            end
                                        end
                                    end
                                    if not enablerange then
                                        PlatDependant.DeleteFile(zippath)
                                        local rangestream = PlatDependant.OpenWriteText(rangefile)
                                        if rangestream and rangestream ~= clr.null then
                                            rangestream:Write(url)
                                            rangestream:Dispose()
                                        end
                                    end
                                    local stream = PlatDependant.OpenAppend(zippath)
                                    if stream and stream ~= clr.null then
                                        dump("success OpenAppend update zip file: "..zippath)
                                        local req = clr.Capstones.Net.HttpRequestBase.Create(url, nil, nil, nil)
                                        req.DestStream = stream
                                        req.RangeEnabled = enablerange
                                        req:StartRequest()
                                        local reqTotal
                                        local reqReceivedLength = 0
                                        local reqReceiveLastTick = clr.System.Environment.TickCount
                                        local rlen = tonumber(v.rlen)
                                        if rlen <= 0 then
                                            rlen = len
                                        end
                                        while not req.IsDone do
                                            if req.Total > 0 and (not reqTotal or req.Total > reqTotal) then
                                                reqTotal = req.Total
                                                rlen = reqTotal
                                            end
                                            if req.Length > reqReceivedLength then
                                                reqReceivedLength = req.Length
                                                reqReceiveLastTick = clr.System.Environment.TickCount
                                                if funcReport then
                                                    if rlen > 0 then
                                                        funcReport("percent", math.clamp(reqReceivedLength / rlen, 0, 1))
                                                    else
                                                        funcReport("streamlength", reqReceivedLength)
                                                    end
                                                end
                                            elseif clr.System.Environment.TickCount - reqReceiveLastTick > 15000 then
                                                req:StopRequest()
                                                break
                                            end
                                            retry_wait = retry_wait + 1
                                            unity.waitForNextEndOfFrame()
                                        end
                                        stream:Dispose()
                                        if req.Error and req.Error ~= "" then
                                            local msg = req.Error
                                            if msg == "timedout" or msg == "cancelled" then
                                                msg = clr.transstr("timedOut")
                                            else
                                                msg = clr.transstr("networkError")
                                            end
                                            if funcReport then
                                                funcReport("error", msg)
                                            end
                                            dump("update error - download error")
                                            dump(msg)
                                        else
                                            local checkpass = true
                                            if ext == ".zip" or type(___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext]) == "table" and ___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext].checkfunc == "zip" then
                                                checkpass = clr.Capstones.UnityEngineEx.CapsUpdateUtils.CheckZipFile(zippath)
                                            elseif type(___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext]) == "table" and type(___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext].checkfunc) == "function" then
                                                checkpass = ___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext].checkfunc(zippath)
                                            end
                                            if checkpass then
                                                if ext == ".zip" then
                                                    if funcReport then
                                                        funcReport("unzip")
                                                    end
                                                    dump("unzip...")

                                                    local prog = PlatDependant.UnzipAsync(zippath, clr.updatepath.."/pending")
                                                    while not prog.Done do
                                                        if funcReport then
                                                            funcReport("unzipprog")
                                                        end
                                                        retry_wait = retry_wait + 1
                                                        unity.waitForNextEndOfFrame()
                                                    end
                                                    PlatDependant.DeleteFile(zippath)
                                                    dump("deleted "..zippath)

                                                    if prog.Error and prog.Error ~= "" then
                                                        if funcReport then
                                                            funcReport("error", prog.Error)
                                                        end
                                                        dump("update error - zip file error")
                                                    else
                                                        itemsuccess = true
                                                        dump("success "..url)
                                                    end
                                                elseif type(___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext]) == "table" and type(___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext].applyfunc) == "function" then
                                                    itemsuccess = ___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext].applyfunc(key, zippath, v)
                                                end
                                            else
                                                if funcReport then
                                                    funcReport("error", "zip file is not correct")
                                                end
                                                dump("update error - zip error")
                                                PlatDependant.DeleteFile(zippath)
                                                dump("deleted "..zippath)
                                            end
                                        end
                                    else
                                        updateFileIndex = updateFileIndex + 1
                                        if funcReport then
                                            funcReport("error", "downloading file is in using.")
                                        end
                                        dump("cannot write update zip file: "..zippath)
                                    end
                                elseif ___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext] then
                                    local handler = ___CONFIG__UPDATE_FILE_EXT_HANDLERS[ext]
                                    if type(handler) == "function" then
                                        itemsuccess = handler(key, url, v)
                                    elseif type(handler) == "table" and type(handler.handler) == "function" then
                                        itemsuccess = handler.handler(key, url, v)
                                    else
                                        funcReport("error", "cannot process file of type "..ext)
                                        dump("error, cannot process file of type "..ext)
                                        itemsuccess = true
                                    end
                                else
                                    funcReport("error", "cannot process file of type "..ext)
                                    dump("error, cannot process file of type "..ext)
                                    itemsuccess = true
                                end
                            end
                        end
                    end
                    return funcComplete(true)
                end
            end
            funcComplete(false)
        elseif resp.failed == 404 then
            funcComplete(false)
        end
    else
        funcComplete(false)
    end
end

local function deleteFilesInFolder(folder)
    local files = clr.table(PlatDependant.GetAllFiles(folder))
    for i, v in ipairs(files) do
        PlatDependant.DeleteFile(v)
    end
end

function update.clear()
    res.Cleanup()
    deleteFilesInFolder(clr.updatepath.."/res")
    deleteFilesInFolder(clr.updatepath.."/spt")
    deleteFilesInFolder(clr.updatepath.."/pending")
    deleteFilesInFolder(clr.updatepath.."/obb")
    deleteFilesInFolder(Application.temporaryCachePath.."/download")
    clr.UnityEngine.PlayerPrefs.DeleteKey("___TEMP__HOTFIX_RECORDED_VERSION")
    unity.restart()
end

_G['update'] = update

return update