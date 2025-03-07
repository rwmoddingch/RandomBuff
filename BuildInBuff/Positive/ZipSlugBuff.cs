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
        On.PlayerGraphics.DrawSprites += HideAndShowSprites;
    }

    private static void HideAndShowSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.player == null) return;

        // 检查玩家是否在Zip模式下
        if (self.player.GetZipData().IsInZipMode())
        {
            // 隐藏玩家所有精灵
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].isVisible = false;
            }

            // 隐藏其他可能的附加精灵
            if (self.player.room != null && self.player.room.game != null)
            {
                for (int i = 0; i < self.player.room.game.cameras.Length; i++)
                {
                    if (self.player.room.game.cameras[i] != null && self.player.room.game.cameras[i].spriteLeasers != null)
                    {
                        foreach (var leaser in self.player.room.game.cameras[i].spriteLeasers)
                        {
                            if (leaser.drawableObject != null && leaser.drawableObject is PlayerGraphics)
                            {
                                for (int j = 0; j < leaser.sprites.Length; j++)
                                {
                                    leaser.sprites[j].isVisible = false;
                                }
                            }
                        }
                    }
                }
            }
        }
        // 检查玩家是否刚刚退出Zip模式
        else if (self.player.GetZipData().JustExitedZipMode())
        {
            // 恢复所有精灵的可见性
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].isVisible = true;
            }

            // 恢复其他可能的附加精灵
            if (self.player.room != null && self.player.room.game != null)
            {
                for (int i = 0; i < self.player.room.game.cameras.Length; i++)
                {
                    if (self.player.room.game.cameras[i] != null && self.player.room.game.cameras[i].spriteLeasers != null)
                    {
                        foreach (var leaser in self.player.room.game.cameras[i].spriteLeasers)
                        {
                            if (leaser.drawableObject != null && leaser.drawableObject is PlayerGraphics)
                            {
                                for (int j = 0; j < leaser.sprites.Length; j++)
                                {
                                    leaser.sprites[j].isVisible = true;
                                }
                            }
                        }
                    }
                }
            }

            // 标记已恢复显示
            self.player.GetZipData().SetJustExitedZipMode(false);

            // 记录日志
            RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", "恢复玩家显示");
        }
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
    private const float SIZE = 1f;
    private float alpha = 0.6f;
    private float pulseTimer = 0f;
    private int life = 10;

    // 添加对ZipSlugPlayerData的引用和位置信息
    private ZipSlugPlayerData zipData;
    private Vector2 pointPosition;

    public PathPointSprite(Room room, Vector2 pos)
    {
        this.room = room;
        this.targetPos = pos;
        this.pos = pos;
        this.pointPosition = pos; // 保存位置信息
        // 使用红色，更容易看到贴墙的路径点位
        this.color = new Color(1f, 0.3f, 0.3f, alpha);
        lastPos = pos;
    }

    // 设置ZipSlugPlayerData引用
    public void SetZipData(ZipSlugPlayerData data)
    {
        this.zipData = data;
    }

    // 设置为Zip模式精灵
    public void SetAsZipModeSprite()
    {
        // 设置初始颜色为白色
        this.color = new Color(1f, 1f, 1f, alpha);
    }

    // 设置为普通精灵，让其自然消散
    public void SetAsNormalSprite()
    {
        // 设置一个较小的生命值，让其很快消散
        life = 5;
    }

    // 设置颜色的方法
    public void SetColor(Color newColor)
    {
        this.color = new Color(newColor.r, newColor.g, newColor.b, alpha);
    }

    // 重置生命值
    public void ResetLife(int newLife)
    {
        life = newLife;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (room == null)
        {
            Destroy();
            return;
        }

        // 只有非Zip模式的精灵才会自动销毁
        if (life <= 0)
        {
            Destroy();
            return;
        }

        pos = room.MiddleOfTile(targetPos);

        // 添加脉冲效果
        pulseTimer += 0.05f;
        alpha = 0.7f + 0.1f * Mathf.Sin(pulseTimer);

        if (life > 0)
        {
            life--;
        }
    }

    // 重写Destroy方法，确保从集合中移除
    public override void Destroy()
    {
        // 如果有ZipData引用，从集合中移除自己
        if (zipData != null)
        {
            zipData.RemovePathPointSprite(this, pointPosition);
        }

        base.Destroy();
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White");
        sLeaser.sprites[0].scale = SIZE;
        sLeaser.sprites[0].color = color;
        sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Hologram"];
        AddToContainer(sLeaser, rCam, null);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        sLeaser.sprites[0].SetPosition(pos - camPos);
        // 更新颜色透明度
        // 计算到当前路径点的距离
        float distance = Vector2.Distance(pos, zipData.player.mainBodyChunk.pos);
        // 根据距离计算透明度衰减
        float distanceRatio = distance / zipData.visiblePathRange;
        // 在边界处完全透明,中心处保持原透明度
        float fadeAlpha = Mathf.Lerp(alpha, 0f, distanceRatio);
        sLeaser.sprites[0].color = new Color(color.r, color.g, color.b, alpha);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        if (newContatiner == null)
        {
            // 使用HUD容器，确保在最上层显示
            newContatiner = rCam.ReturnFContainer("Items");
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
    public Player player;
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

    // Zip模式相关变量
    private bool isInZipMode = false;
    private int zipModeCounter = 0;
    private const int ZIP_MODE_DURATION = 200; // 10秒 * 40帧
    private Vector2 currentZipPosition;
    private int currentPathPointIndex = -1;
    private Dictionary<Vector2, int> pathPointActivation = new Dictionary<Vector2, int>();
    private Dictionary<Vector2, PathPointSprite> pathPointSprites = new Dictionary<Vector2, PathPointSprite>();
    private bool justExitedZipMode = false; // 标记是否刚刚退出Zip模式
    public float visiblePathRange = 0f; // 可见路径范围

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

        // 检测玩家是否进入bellyslide动画状态
        if (!isInZipMode && player.animation == Player.AnimationIndex.BellySlide)
        {
            EnterZipMode();
        }

        // 更新Zip模式
        if (isInZipMode)
        {
            UpdateZipMode();
            // 更新可见路径范围
            UpdateVisiblePathRange();
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

    // 进入Zip模式
    private void EnterZipMode()
    {
        if (overseerPathPoints.Count == 0) return;

        isInZipMode = true;
        zipModeCounter = ZIP_MODE_DURATION;
        pathSprites.Clear();
        // 找到最近的路径点
        FindNearestPathPoint();

        // 初始化路径点激活状态
        InitializePathPointActivation();

        // 更新可见路径范围和显示
        UpdateVisiblePathRange();

        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", "进入Zip模式");
    }

    // 更新Zip模式
    private void UpdateZipMode()
    {
        // 倒计时
        zipModeCounter--;

        if (zipModeCounter <= 0)
        {
            ExitZipMode();
            return;
        }

        // 检查玩家是否按下跳跃键
        if (player.input[0].jmp && player.input[0].y >= 0)
        {
            ExitZipMode();
            // 给予玩家一个向上的速度，模拟从Zip模式弹出
            player.mainBodyChunk.vel.y = 10f;
            player.bodyChunks[1].vel.y = 10f;
            return;
        }
        //防止进入管道出现问题
        player.shortcutDelay = 5;
        // 隐藏玩家
        HidePlayer();

        // 同步位置到当前路径点
        SyncPositionToPathPoint();

        // 处理玩家输入，移动到其他路径点
        HandleZipModeInput();

        // 不需要在这里更新路径点激活状态和颜色，因为这个功能已经在UpdatePathPointsVisibility中处理了
    }

    // 退出Zip模式
    private void ExitZipMode()
    {
        isInZipMode = false;
        justExitedZipMode = true; // 标记刚刚退出Zip模式

        // 恢复玩家碰撞
        RestorePlayer();

        // 不清除路径点显示，让它们自然消散
        // ClearPathPointSprites();

        // 将所有路径点设置为非Zip模式，让它们自然消散
        foreach (var sprite in pathSprites)
        {
            if (sprite != null)
            {
                // 确保保留ZipData引用
                sprite.SetAsNormalSprite();
            }
        }

        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", "退出Zip模式，设置justExitedZipMode = true，路径点数量: " + pathSprites.Count);
    }

    // 隐藏玩家
    private void HidePlayer()
    {
        // 禁用玩家碰撞
        foreach (var chunk in player.bodyChunks)
        {
            chunk.collideWithTerrain = false;
        }
    }

    // 恢复玩家
    private void RestorePlayer()
    {
        // 恢复玩家碰撞
        foreach (var chunk in player.bodyChunks)
        {
            chunk.collideWithTerrain = true;
        }
    }

    // 找到最近的路径点
    private void FindNearestPathPoint()
    {
        if (overseerPathPoints.Count == 0) return;

        float minDist = float.MaxValue;
        currentPathPointIndex = 0;

        for (int i = 0; i < overseerPathPoints.Count; i++)
        {
            float dist = Vector2.Distance(player.mainBodyChunk.pos, overseerPathPoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                currentPathPointIndex = i;
            }
        }

        currentZipPosition = overseerPathPoints[currentPathPointIndex];
    }

    // 同步位置到当前路径点
    private void SyncPositionToPathPoint()
    {
        if (currentPathPointIndex < 0 || currentPathPointIndex >= overseerPathPoints.Count) return;

        pathPointActivation[currentZipPosition] = 10;
        // 同步玩家位置到当前路径点
        player.mainBodyChunk.pos = currentZipPosition;
        player.bodyChunks[1].pos = currentZipPosition;

        // 重置速度
        player.mainBodyChunk.vel = Vector2.zero;
        player.bodyChunks[1].vel = Vector2.zero;
    }

    // 处理Zip模式下的输入
    private void HandleZipModeInput()
    {
        if (overseerPathPoints.Count == 0) return;
        
        // 获取玩家输入
        int inputX = player.input[0].x;
        int inputY = player.input[0].y;
        
        if (inputX == 0 && inputY == 0) return;
        
        // 寻找最近的符合方向的路径点
        Vector2 currentPoint = overseerPathPoints[currentPathPointIndex];
        int newIndex = -1;
        float minDist = 45; // 减少判定范围至35格
        float bestDotProduct = 0.5f; // 最佳方向匹配度
        
        // 创建输入方向向量
        Vector2 inputDir = new Vector2(inputX, inputY).normalized;
        
        // 记录调试信息
        if (UnityEngine.Time.frameCount % 20 == 0)
        {
            RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"处理移动输入: ({inputX},{inputY}), 当前位置: {currentPoint}");
        }
        
        for (int i = 0; i < overseerPathPoints.Count; i++)
        {
            if (i == currentPathPointIndex) continue;
            
            Vector2 targetPoint = overseerPathPoints[i];
            Vector2 direction = targetPoint - currentPoint;
            float dist = Vector2.Distance(currentPoint, targetPoint);
            
            // 只考虑35格以内的路径点
            if (dist > 35f) continue;
            
            // 计算方向向量与输入向量的点积，判断方向是否相似
            Vector2 targetDir = direction.normalized;
            float dotProduct = Vector2.Dot(inputDir, targetDir);
            
            // 如果点积大于阈值，认为方向匹配
            if (dotProduct > 0.3f) // 降低阈值到0.3（约70度以内）
            {
                // 优先选择方向最匹配的点
                if (dotProduct > bestDotProduct || (Mathf.Approximately(dotProduct, bestDotProduct) && dist < minDist))
                {
                    bestDotProduct = dotProduct;
                    minDist = dist;
                    newIndex = i;
                }
            }
        }
        
        // 如果找到了新的路径点，则移动到该点
        if (newIndex >= 0)
        {
            Vector2 oldPosition = currentZipPosition;
            currentPathPointIndex = newIndex;
            currentZipPosition = overseerPathPoints[currentPathPointIndex];
            
            // 激活当前路径点
            pathPointActivation[currentZipPosition] = 10;
            
            // 如果有这个点的精灵，重置其生命值
            if (pathPointSprites.ContainsKey(currentZipPosition))
            {
                pathPointSprites[currentZipPosition].ResetLife(3);
            }
            
            // 计算移动方向与输入方向的匹配度
            Vector2 moveDir = (currentZipPosition - oldPosition).normalized;
            float moveDotProduct = Vector2.Dot(inputDir, moveDir);
            
            // 记录移动日志
            RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"移动到新路径点: 从 {oldPosition} 到 {currentZipPosition}, 距离: {Vector2.Distance(oldPosition, currentZipPosition)}, 输入: ({inputX},{inputY}), 匹配度: {moveDotProduct:F2}");
        }
        else
        {
            // 如果没找到合适的点，记录日志
            if (UnityEngine.Time.frameCount % 20 == 0)
            {
                RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"未找到合适的移动目标点，输入: ({inputX},{inputY})");
            }
        }
    }

    // 初始化路径点激活状态
    private void InitializePathPointActivation()
    {
        pathPointActivation.Clear();
        foreach (var point in overseerPathPoints)
        {
            pathPointActivation[point] = 0;
        }

        // 激活当前路径点
        if (currentPathPointIndex >= 0 && currentPathPointIndex < overseerPathPoints.Count)
        {
            pathPointActivation[overseerPathPoints[currentPathPointIndex]] = 10;
        }
    }

    // 清除路径点显示
    private void ClearPathPointSprites()
    {
        // 创建一个副本以避免在迭代过程中修改集合
        List<PathPointSprite> spritesToDestroy = new List<PathPointSprite>(pathSprites);

        foreach (var sprite in spritesToDestroy)
        {
            if (sprite != null)
            {
                // 在销毁前移除ZipData引用，避免循环调用
                sprite.SetZipData(null);
                sprite.Destroy();
            }
        }

        // 清空集合
        pathSprites.Clear();
        pathPointSprites.Clear();

        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", "清除所有路径点显示");
    }

    // 添加一个公共方法来检查是否在Zip模式下
    public bool IsInZipMode()
    {
        return isInZipMode;
    }

    // 添加一个公共方法来检查是否刚刚退出Zip模式
    public bool JustExitedZipMode()
    {
        return justExitedZipMode;
    }

    // 添加一个公共方法来设置是否刚刚退出Zip模式
    public void SetJustExitedZipMode(bool value)
    {
        justExitedZipMode = value;
    }

    // 更新可见路径范围
    private void UpdateVisiblePathRange()
    {
        // 根据剩余时间计算可见范围
        float timeRatio = (float)zipModeCounter / ZIP_MODE_DURATION;
        visiblePathRange = 100f + 200f * timeRatio; // 范围从100到300不等

        // 更新路径点显示
        UpdatePathPointsVisibility();
    }

    // 更新路径点可见性
    private void UpdatePathPointsVisibility()
    {
        if (currentPathPointIndex < 0 || currentPathPointIndex >= overseerPathPoints.Count) return;

        Vector2 currentPoint = overseerPathPoints[currentPathPointIndex];

        // 更新所有路径点的激活状态
        List<Vector2> keys = new List<Vector2>(pathPointActivation.Keys);
        foreach (var point in keys)
        {
            if (pathPointActivation[point] > 0)
            {
                pathPointActivation[point]--;
            }
        }

        // 不清除路径点显示，让它们自然消散
        // ClearPathPointSprites();

        // 显示在范围内的路径点
        foreach (var point in overseerPathPoints)
        {
            float distance = Vector2.Distance(currentPoint, point);
            if (distance <= visiblePathRange)
            {
                // 检查是否已经有这个点的精灵
                bool spriteExists = pathPointSprites.ContainsKey(point);
                PathPointSprite sprite;

                if (spriteExists)
                {
                    // 更新现有精灵
                    sprite = pathPointSprites[point];
                    // 重置生命值为10，防止自然消散
                    sprite.ResetLife(3);
                }
                else
                {
                    // 创建新精灵
                    sprite = new PathPointSprite(player.room, point);
                    sprite.SetAsZipModeSprite();
                    sprite.SetZipData(this); // 设置ZipData引用
                    pathSprites.Add(sprite);
                    pathPointSprites[point] = sprite;
                    player.room.AddObject(sprite);
                }

                // 设置颜色
                int activation = pathPointActivation.ContainsKey(point) ? pathPointActivation[point] : 0;
                Color blueColor = new Color(0.2f, 0.4f, 1f);
                Color whiteColor = player.ShortCutColor();
                float t = 1f - (activation / 10f);
                Color initialColor = Color.Lerp(blueColor, whiteColor, t);
                sprite.SetColor(initialColor);

                // 记录日志
                if (activation > 0 && UnityEngine.Time.frameCount % 10 == 0)
                {
                    RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"路径点激活状态: {activation}/10, 颜色: {initialColor}, 距离: {distance}/{visiblePathRange}");
                }
            }
        }
    }

    // 添加一个方法来移除PathPointSprite
    public void RemovePathPointSprite(PathPointSprite sprite, Vector2 position)
    {
        // 从pathSprites列表中移除
        if (pathSprites.Contains(sprite))
        {
            pathSprites.Remove(sprite);
        }

        // 从pathPointSprites字典中移除
        if (pathPointSprites.ContainsKey(position) && pathPointSprites[position] == sprite)
        {
            pathPointSprites.Remove(position);
        }

        RandomBuffUtils.BuffUtils.Log("ZipSlugBuff", $"移除路径点精灵: 位置 {position}");
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
