-- 设置控制台输出编码为UTF-8
os.execute("chcp 65001 > nul")

-- 创建并执行PowerShell脚本来显示颜色选择器
local function show_color_picker_dialog()
    local ps_script = [[
Add-Type -AssemblyName System.Windows.Forms
$dialog = New-Object System.Windows.Forms.ColorDialog
$dialog.AllowFullOpen = $true
$dialog.AnyColor = $true
$dialog.ShowDialog()
if ($dialog.Color) {
    $hex = "#" + [System.Drawing.ColorTranslator]::ToHtml($dialog.Color).Substring(1)
    Write-Output $hex
}
]]

    -- 将PowerShell脚本保存到临时文件
    local temp_file = os.getenv("TEMP") .. "\\color_picker.ps1"
    local file = io.open(temp_file, "w")
    file:write(ps_script)
    file:close()

    -- 执行PowerShell脚本并获取结果
    local handle = io.popen('powershell -ExecutionPolicy Bypass -File "' .. temp_file .. '"')
    local result = handle:read("*a")
    handle:close()

    -- 清理临时文件
    os.remove(temp_file)

    -- 返回颜色代码
    return result:match("#%x+")
end

-- 主选色函数
local function pick_color()
    print("正在打开颜色选择器...")
    local color = show_color_picker_dialog()
    return color or "#FFFFFF" -- 如果用户取消，返回白色
end

-- 导出函数
return {
    pick_color = pick_color
} 