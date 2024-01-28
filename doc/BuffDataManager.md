<h1>BuffDataManager</h1>

负责管理增益数据。为单例模式（调用请使用BuffDataManager.Instance.xxxx）

<h2>方法</h2>

```csharp
//获取BuffData
//如果不存在则返回 null
public BuffData GetBuffData(BuffID id);
```

```csharp
//获取全部启用的Buff ID
public List<BuffID> GetAllBuffIds();
```

<h2>属性</h2>

```csharp
//获取BuffDataManager的实例
public static BuffDataManager Instance { get; };
```