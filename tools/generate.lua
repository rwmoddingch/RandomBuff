-- 设置控制台输出编码为UTF-8
os.execute("chcp 65001 > nul")  -- Windows下设置UTF-8编码

local json = require("json")
local colorpicker = require("colorpicker")

-- 简化的类型映射
local type_map = {
    p = "Positive",
    d = "Duality",
    n = "Negative"
}

local property_map = {
    n = "Normal",
    s = "Special"
}

-- 预定义的颜色选项
local colors = {
    ["1"] = {name = "红色", value = "#FF0000"},
    ["2"] = {name = "绿色", value = "#00FF00"},
    ["3"] = {name = "蓝色", value = "#0000FF"},
    ["4"] = {name = "黄色", value = "#FFFF00"},
    ["5"] = {name = "紫色", value = "#FF00FF"},
    ["6"] = {name = "青色", value = "#00FFFF"},
    ["7"] = {name = "白色", value = "#FFFFFF"},
    ["8"] = {name = "黑色", value = "#000000"},
    ["9"] = {name = "橙色", value = "#FFA500"},
    ["0"] = {name = "灰色", value = "#808080"}
}

-- 检查目录是否存在
local function directory_exists(path)
    local cmd = string.format('if exist "%s\\" (exit 0) else (exit 1)', path)
    local result = os.execute(cmd)
    return result == true or result == 0
end

-- 创建目录函数
local function create_directory(path)
    local cmd = string.format('mkdir "%s" 2>nul', path)
    local result = os.execute(cmd)
    return result == true or result == 0
end

-- 显示颜色选择器
local function show_color_picker()
    print("\n=== 颜色选择 ===")
    print("请选择颜色 (输入数字)：")
    for key, color in pairs(colors) do
        print(string.format("%s: %s (%s)", key, color.name, color.value))
    end
    print("c: 自定义颜色")
    
    local choice = io.read()
    if choice == "c" then
        print("请输入自定义颜色代码 (例如: #FF0000)：")
        return io.read()
    else
        return colors[choice] and colors[choice].value or "#FFFFFF"
    end
end

-- 模板配置
local templates = {
    -- JSON模板
    json = [[
{
    "FaceName": "${name}",
    "ID": "${id}",
    "BuffType": "${buffType}",
    "BuffProperty": "${buffProperty}",
    "Triggerable": ${triggerable},
    "Stackable": ${stackable},
    "Color": "${color}",
    "Chinese": {
        "BuffName": "${chineseName}",
        "Description": "${description}"
    }
}]],

    -- C#类模板
    cs = [[
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;

class ${name}Buff : Buff<${name}Buff, ${name}BuffData>
{
    public override BuffID ID => ${name}BuffEntry.${name}ID;
}

class ${name}BuffData : BuffData
{
    public override BuffID ID => ${name}BuffEntry.${name}ID;
}

class ${name}BuffEntry : IBuffEntry
{
    public static BuffID ${name}ID = new BuffID("${id}", true);

    public void OnEnable()
    {
        BuffRegister.RegisterBuff<${name}Buff, ${name}BuffData, ${name}BuffEntry>(${name}ID);
    }

    public static void HookOn()
    {
        // TODO: 实现具体逻辑
    }
}]]
}

-- 获取buff类型对应的目录名
local function get_type_directory(short_type)
    return type_map[short_type:lower()]
end

-- 获取属性全名
local function get_property_name(short_prop)
    return property_map[short_prop:lower()]
end

-- 简单的模板渲染函数
local function render_template(template, data)
    return template:gsub("%${(.-)}", function(key)
        return data[key] or ""
    end)
end

-- 写入文件函数
local function write_file(path, content)
    print("尝试写入文件: " .. path)
    local file = io.open(path, "w")
    if not file then
        print("错误: 无法创建文件: " .. path)
        return false
    end
    
    local success = pcall(function()
        file:write(content)
        file:close()
    end)
    
    if not success then
        print("错误: 写入文件失败: " .. path)
        return false
    end
    
    print("成功写入文件: " .. path)
    return true
end

-- 用VS Code打开文件
local function open_in_editor(path)
    -- 尝试使用VS Code打开
    local cmd = string.format('code "%s"', path)
    local result = os.execute(cmd)
    
    if not result then
        print("警告: 无法使用VS Code打开文件，请确保VS Code已安装且'code'命令可用")
        -- 尝试使用系统默认程序打开
        os.execute(string.format('start "" "%s"', path))
    end
end

-- 在现有代码中添加这个函数
local function create_buff_image(path, face_name, buff_id, bg_color)
    local ps_script = string.format([[
Add-Type -AssemblyName System.Drawing

# 创建图片
$image = New-Object System.Drawing.Bitmap 300, 500
$graphics = [System.Drawing.Graphics]::FromImage($image)

# 设置背景色
$backgroundColor = [System.Drawing.ColorTranslator]::FromHtml('%s')
$graphics.Clear($backgroundColor)

# 创建淡化背景色的随机图案
$rnd = New-Object System.Random
$lighterColor = [System.Drawing.Color]::FromArgb(
    [Math]::Min(255, $backgroundColor.R + 40),
    [Math]::Min(255, $backgroundColor.G + 40),
    [Math]::Min(255, $backgroundColor.B + 40)
)
$brush = New-Object System.Drawing.SolidBrush($lighterColor)

# 随机绘制5个大圆形作为背景装饰
for ($i = 0; $i -lt 5; $i++) {
    $x = $rnd.Next(-100, 300)
    $y = $rnd.Next(-100, 500)
    $size = $rnd.Next(100, 200)
    $graphics.FillEllipse($brush, $x, $y, $size, $size)
}

# 计算文字颜色（如果背景色深就用白色，浅就用黑色）
$r = [convert]::ToInt32($backgroundColor.R)
$g = [convert]::ToInt32($backgroundColor.G)
$b = [convert]::ToInt32($backgroundColor.B)
$brightness = ($r * 0.299 + $g * 0.587 + $b * 0.114) / 255
$textColor = if ($brightness -gt 0.5) { [System.Drawing.Color]::Black } else { [System.Drawing.Color]::White }

# 设置文字格式
$font = New-Object System.Drawing.Font('Arial', 32, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush($textColor)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

# 保存当前图形状态
$state = $graphics.Save()

# 设置45度旋转
$graphics.TranslateTransform(150, 250)  # 移动到中心点
$graphics.RotateTransform(45)           # 旋转45度
$graphics.TranslateTransform(-150, -250) # 移回原位

# 绘制文字
$rect = New-Object System.Drawing.RectangleF(0, 0, 300, 500)
$graphics.DrawString('%s', $font, $brush, $rect, $format)

# 恢复图形状态
$graphics.Restore($state)

# 保存图片
$image.Save('%s', [System.Drawing.Imaging.ImageFormat]::Png)

# 清理资源
$graphics.Dispose()
$image.Dispose()
]], bg_color, buff_id, path:gsub("/", "\\"))

    -- 保存PowerShell脚本到临时文件
    local temp_file = os.getenv("TEMP") .. "\\create_image.ps1"
    local file = io.open(temp_file, "w")
    file:write(ps_script)
    file:close()

    -- 执行PowerShell脚本
    os.execute('powershell -ExecutionPolicy Bypass -File "' .. temp_file .. '"')

    -- 清理临时文件
    os.remove(temp_file)
end

-- 主函数
local function generate()
    -- 检查必要的目录结构
    local root_dir = "..\\"  -- 项目根目录
    
    print("当前工作目录:")
    os.execute("cd")
    
    local required_dirs = {
        root_dir .. "buffinfos",
        root_dir .. "buffinfos\\BuiltinBuffs",
        root_dir .. "buffinfos\\BuiltinBuffs\\cardinfos",
        root_dir .. "BuildInBuff"
    }
    
    -- 检查每个必需的目录
    local all_dirs_exist = true
    for _, dir in ipairs(required_dirs) do
        if not directory_exists(dir) then
            print("错误: 找不到目录: " .. dir)
            all_dirs_exist = false
        else
            print("找到目录: " .. dir)
        end
    end
    
    if not all_dirs_exist then
        print("\n目录结构应该是：")
        print("RandomBuff/")
        print("├── tools/")
        print("│   └── buffinfos/")
        print("│       └── BuiltinBuffs/")
        print("│           └── cardinfos/")
        print("└── BuildInBuff/")
        print("\n请确保在正确的项目目录中运行此脚本")
        return
    end

    print("=== Buff生成器 ===")
    
    print("请输入Buff名称：")
    local name = io.read()
    
    print("请输入Buff类型 (p=正面/d=中立/n=负面)：")
    local short_type = io.read()
    while not type_map[short_type:lower()] do
        print("无效类型，请重新输入 (p/d/n)：")
        short_type = io.read()
    end
    local buffType = type_map[short_type:lower()]
    
    print("请输入Buff属性 (n=普通/s=特殊)：")
    local short_prop = io.read()
    while not property_map[short_prop:lower()] do
        print("无效属性，请重新输入 (n/s)：")
        short_prop = io.read()
    end
    local buffProperty = property_map[short_prop:lower()]
    
    print("是否可触发 (t/f)：")
    local trig = io.read()
    local triggerable = trig:lower() == "t" and "true" or "false"
    
    print("是否可堆叠 (t/f)：")
    local stack = io.read()
    local stackable = stack:lower() == "t" and "true" or "false"
    
    -- 使用颜色选择器
    local color = colorpicker.pick_color()
    
    print("请输入中文名称：")
    local chineseName = io.read()
    
    print("请输入描述：")
    local description = io.read()

    -- 准备数据
    local data = {
        name = name,
        id = name .. "ID",
        buffType = buffType,
        buffProperty = buffProperty,
        triggerable = triggerable,
        stackable = stackable,
        color = color,
        chineseName = chineseName,
        description = description
    }

    -- 检查并使用目标目录
    local json_base_dir = root_dir .. "buffinfos\\BuiltinBuffs\\cardinfos\\" .. buffType
    local json_buff_dir = json_base_dir .. "\\" .. name  -- 添加以buff名字命名的文件夹
    local cs_dir = root_dir .. "BuildInBuff\\" .. buffType

    -- 确保目录存在
    if not directory_exists(json_base_dir) then
        print("错误: 找不到目录: " .. json_base_dir)
        return
    end

    -- 创建buff专属目录
    if not directory_exists(json_buff_dir) then
        print("创建目录: " .. json_buff_dir)
        if not create_directory(json_buff_dir) then
            print("错误: 无法创建目录: " .. json_buff_dir)
            return
        end
    end

    if not directory_exists(cs_dir) then
        print("错误: 找不到目录: " .. cs_dir)
        return
    end

    -- 生成文件
    local json_content = render_template(templates.json, data)
    local cs_content = render_template(templates.cs, data)

    -- 修改JSON文件路径，放在buff名字的文件夹下
    local json_path = json_buff_dir .. "\\" .. name .. ".json"
    local cs_path = cs_dir .. "\\" .. name .. "Buff.cs"

    local files_generated = true

    if write_file(json_path, json_content) then
        print("JSON文件已生成：" .. json_path)
        
        -- 创建图片
        local image_path = json_buff_dir .. "\\" .. name .. ".png"
        create_buff_image(image_path, name, data.id, data.color)
        print("图片已生成：" .. image_path)
    else
        files_generated = false
    end

    if write_file(cs_path, cs_content) then
        print("C#文件已生成：" .. cs_path)
    else
        files_generated = false
    end

    -- 如果文件生成成功，则打开它们
    if files_generated then
        print("\n正在打开生成的文件...")
        open_in_editor(json_path)
        open_in_editor(cs_path)
    end
end

-- 错误处理
local success, error = pcall(generate)
if not success then
    print("发生错误：" .. tostring(error))
end

print("\n按回车键退出...")
io.read() 