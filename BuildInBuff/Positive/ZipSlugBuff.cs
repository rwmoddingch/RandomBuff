using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using RWCustom;
using MoreSlugcats;
using System.Threading.Tasks;
using System;

class ZipSlugBuff : Buff<ZipSlugBuff, ZipSlugBuffData>
{
    public override BuffID ID => ZipSlugBuffEntry.ZipSlugID;
}

class ZipSlugBuffData : BuffData
{
    public override BuffID ID => ZipSlugBuffEntry.ZipSlugID;
}

class ZipSlugBuffEntry : IBuffEntry
{
    public static BuffID ZipSlugID = new BuffID("ZipSlugID", true);

    public void OnEnable()
    {
        BuffRegister.RegisterBuff<ZipSlugBuff, ZipSlugBuffData, ZipSlugBuffEntry>(ZipSlugID);
    }

    public static void HookOn()
    {
        On.Player.Update += ZipUpdate;
    }

    private static void ZipUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        var data = self.GetZipData();
        data.Update();
    }
}

public class PathPointSprite : CosmeticSprite
{
    private Vector2 targetPos;
    private Color color;
    private const float SIZE = 20f;  // 调整大小为6像素
    private float alpha = 0.7f;
    private float pulseTimer = 0f;

    public PathPointSprite(Room room, Vector2 pos)
    {
        this.room = room;
        this.targetPos = pos;
        this.pos = pos;
        // 使用红色，更容易看到贴墙的路径点位
        this.color = new Color(1f, 0.3f, 0.3f, alpha);
        lastPos = pos;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (room == null)
        {
            Destroy();
            return;
        }
        pos = room.MiddleOfTile(targetPos);

        // 添加脉冲效果
        pulseTimer += 0.05f;
        alpha = 0.5f + 0.3f * Mathf.Sin(pulseTimer);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("pixel");
        sLeaser.sprites[0].scale = SIZE;
        sLeaser.sprites[0].color = color;
        sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Hologram"];
        AddToContainer(sLeaser, rCam, null);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        sLeaser.sprites[0].SetPosition(pos - camPos);
        // 更新颜色透明度
        sLeaser.sprites[0].color = new Color(color.r, color.g, color.b, alpha);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        if (newContatiner == null)
        {
            // 使用HUD容器，确保在最上层显示
            newContatiner = rCam.ReturnFContainer("HUD");
        }
        foreach (FSprite fsprite in sLeaser.sprites)
        {
            fsprite.RemoveFromContainer();
            newContatiner.AddChild(fsprite);
        }
    }
}

public class ZipSlugPlayerData
{
    private Player player;
    public Room lastRoom;
    public List<Vector2> overseerPathPoints;
    private bool isCalculatingPath;
    private const float GRID_SIZE = 20f;  // 减小网格大小为20像素，获得更精确的路径
    // 存储路径点位的sprite
    private List<PathPointSprite> pathSprites;

    // 用于分块计算的变量
    private int currentGridX = 0;
    private int currentGridY = 0;
    private int totalGridX = 0;
    private int totalGridY = 0;
    private bool isUpdatingGrid = false;
    private List<Vector2> tempValidPoints = new List<Vector2>();

    public ZipSlugPlayerData(Player player)
    {
        this.player = player;
        overseerPathPoints = new List<Vector2>();
        isCalculatingPath = false;
        pathSprites = new List<PathPointSprite>();
    }

    internal void Update()
    {
        // 检查房间是否改变
        if (player.room != lastRoom)
        {
            RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"房间改变: 从 {(lastRoom != null ? lastRoom.abstractRoom.name : "null")} 到 {(player.room != null ? player.room.abstractRoom.name : "null")}");
            
            // 清除旧的路径点位sprite
            ClearPathSprites();
            
            lastRoom = player.room;
            if (player.room != null)
            {
                overseerPathPoints.Clear();
                // 重置分析状态
                isCalculatingPath = false;
                isUpdatingGrid = false;
                currentGridX = 0;
                currentGridY = 0;
                // 初始化网格
                InitializeGrids();
                
                RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"初始化网格: 房间大小 {player.room.PixelWidth}x{player.room.PixelHeight}, 网格数量 {totalGridX}x{totalGridY}");
            }
        }

        // 如果有待分析的区块则进行分析
        if (isCalculatingPath && !isUpdatingGrid)
        {
            UpdateValidPathPoints();
        }
        
        // 显示计算进度
        if (isCalculatingPath)
        {
            ShowCalculationProgress();
        }
    }

    private void InitializeGrids()
    {
        if (player.room == null) return;

        // 计算需要多少个500*500的区块
        totalGridX = (int)Math.Ceiling(player.room.PixelWidth / 500f);
        totalGridY = (int)Math.Ceiling(player.room.PixelHeight / 500f);

        tempValidPoints.Clear();
        isCalculatingPath = true;
        currentGridX = 0;
        currentGridY = 0;
        
        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"开始计算路径点位: 房间 {player.room.abstractRoom.name}, 区块数量 {totalGridX}x{totalGridY}");
    }

    private void UpdateValidPathPoints()
    {
        if (player.room == null) return;
        
        isUpdatingGrid = true;
        
        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"处理区块 [{currentGridX},{currentGridY}]/{totalGridX}x{totalGridY}");
        
        // 计算当前区块的边界
        float startX = currentGridX * 500f;
        float startY = currentGridY * 500f;
        float endX = Math.Min(startX + 500f, player.room.PixelWidth);
        float endY = Math.Min(startY + 500f, player.room.PixelHeight);
        
        int pointsFoundInBlock = 0;
        
        // 在当前区块中寻找有效的路径点位
        for (float x = startX; x < endX; x += GRID_SIZE)
        {
            for (float y = startY; y < endY; y += GRID_SIZE)
            {
                Vector2 testPos = new Vector2(x + GRID_SIZE / 2f, y + GRID_SIZE / 2f);
                
                // 检查该位置是否适合Overseer移动（贴墙）
                if (IsValidOverseerPosition(player.room, testPos))
                {
                    tempValidPoints.Add(testPos);
                    pointsFoundInBlock++;
                    
                    // 立即为每个有效点位创建一个临时的可视标记
                    PathPointSprite sprite = new PathPointSprite(player.room, testPos);
                    pathSprites.Add(sprite);
                    player.room.AddObject(sprite);
                }
            }
        }
        
        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"区块 [{currentGridX},{currentGridY}] 找到 {pointsFoundInBlock} 个路径点位");
        
        // 移动到下一个区块
        currentGridY++;
        if (currentGridY >= totalGridY)
        {
            currentGridY = 0;
            currentGridX++;
        }
        
        // 检查是否所有区块都已处理完毕
        if (currentGridX >= totalGridX)
        {
            // 所有区块都已处理完毕
            isCalculatingPath = false;
            
            // 更新主要的路径点位列表
            lock (overseerPathPoints)
            {
                overseerPathPoints.Clear();
                overseerPathPoints.AddRange(tempValidPoints);
            }
            
            RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"计算完成: 房间 {player.room.abstractRoom.name} 中找到 {overseerPathPoints.Count} 个贴墙路径点位");
        }
        
        isUpdatingGrid = false;
    }

    private void ClearPathSprites()
    {
        foreach (var sprite in pathSprites)
        {
            sprite.Destroy();
        }
        pathSprites.Clear();
    }

    private bool IsValidOverseerPosition(Room room, Vector2 position)
    {
        // 获取位置对应的瓦片坐标
        IntVector2 tilePos = room.GetTilePosition(position);

        // 检查该位置是否在房间范围内
        if (tilePos.x < 0 || tilePos.y < 0 || tilePos.x >= room.Width || tilePos.y >= room.Height)
            return false;

        // 检查该位置是否是固体瓦片，如果是固体则不可通行
        if (room.GetTile(tilePos).Solid)
            return false;

        // 检查是否贴着墙面（至少有一个相邻瓦片是固体）
        bool adjacentToWall = false;
        
        // 检查四个方向的相邻瓦片
        IntVector2[] adjacentTiles = new IntVector2[]
        {
            new IntVector2(tilePos.x + 1, tilePos.y), // 右
            new IntVector2(tilePos.x - 1, tilePos.y), // 左
            new IntVector2(tilePos.x, tilePos.y + 1), // 上
            new IntVector2(tilePos.x, tilePos.y - 1)  // 下
        };
        
        foreach (IntVector2 adjPos in adjacentTiles)
        {
            // 确保相邻位置在房间范围内
            if (adjPos.x >= 0 && adjPos.y >= 0 && adjPos.x < room.Width && adjPos.y < room.Height)
            {
                // 如果相邻瓦片是固体，则该位置贴着墙面
                if (room.GetTile(adjPos).Solid)
                {
                    adjacentToWall = true;
                    break;
                }
            }
        }
        
        return adjacentToWall;
    }

    // 显示计算进度
    private void ShowCalculationProgress()
    {
        if (player.room == null || totalGridX <= 0 || totalGridY <= 0) return;
        
        // 计算总进度
        int totalBlocks = totalGridX * totalGridY;
        int completedBlocks = currentGridX * totalGridY + currentGridY;
        float progressPercentage = (float)completedBlocks / totalBlocks * 100f;
        
        // 每秒更新一次进度日志
        if (UnityEngine.Time.frameCount % 40 == 0) // 假设游戏运行在40FPS
        {
            RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"计算进度: {progressPercentage:F1}% ({completedBlocks}/{totalBlocks} 区块), 已找到 {tempValidPoints.Count} 个路径点位");
        }
    }
}

public static class ZipSlugPlayerDataExtensions
{
    private static ConditionalWeakTable<Player, ZipSlugPlayerData> zipDataTable = new ConditionalWeakTable<Player, ZipSlugPlayerData>();

    public static ZipSlugPlayerData GetZipData(this Player player)
    {
        return zipDataTable.GetValue(player, (p) => new ZipSlugPlayerData(p));
    }
}
