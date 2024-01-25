
<h1>BuffData</h1>

增益的数据类型，保存存档内数据，也可以获取配置属性。
不会随着单局开始结束而重复创建，仅在更换存档和启动游戏时候创建。

<h2>方法</h2>


```csharp
//轮回结束时触发
public virtual void CycleEnd(); 
```


```csharp
//重复选取时会被调用
public virtual void Stack(); 
```

```csharp
//当存档数据读取后调用
//可以用于数据初始化
public abstract void DataLoaded(bool newData); 
```

```csharp
//获取配置属性（可以用下文数据的方式获取）
public T GetConfig<T>(string name);
```

<h2>属性</h2>

```csharp
//增益数据对应的BuffID
public abstract BuffID ID { get; }
```

存档数据
```csharp
[JsonProperty]
public AnyType saveInWorld; //创建一个想要保存到猫的游戏存档的数据
```

配置属性
```csharp
[CustomStaticConfig]
public AnyType saveInSlot { get; } //创建一个配置属性
//这个属性不跟随猫存档变化而变化
//会有一个单独的界面(类似mod界面)来进行配置

///注意 ： 必须使用 {get;} 的形式！！！
```

