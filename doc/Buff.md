<h1>Buff&lt;TData&gt;</h1>
单轮回内创建的实例，负责实际的运行逻辑。
会在单轮回结束时销毁。

<h2>方法</h2>

```csharp
//当点击（或按键）触发时调用
//仅对可触发的增益有效（Triggerable == true）
public abstract bool Trigger(RainWorldGame game);
```

```csharp
//增益的更新方法，与RainWorldGame.Update同步
public abstract void Update(RainWorldGame game);
```

```csharp
// 增益的销毁方法，当该增益实例被移除的时候会调用
// 注意：当前轮回结束时会清除全部的Buff物体
public abstract void Destroy();
```

<h2>属性</h2>

```csharp
//增益数据对应的BuffID
public abstract BuffID ID { get; }
```

```csharp
//增益对应的数据
public TData Data { get; }
```

```csharp
// 如果为true，则在周期结束时自动移除本增益
public bool NeedDeletion { get; set; }
```
