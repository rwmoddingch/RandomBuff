<h1>BuffPoolManager</h1>

单轮回内负责管理增益数据。为单例模式（调用请使用BuffPoolManager.Instance.xxxx）

<h2>方法</h2>

```csharp
//获取ID对应的Buff
//不存在则返回null
public IBuff GetBuff(BuffID id);
```

<h2>属性</h2>

```csharp
//获取BuffPoolManager的实例
//在游戏为开始前获取行为未定义
public static BuffPoolManager Instance { get; };
```