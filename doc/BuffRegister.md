<h1>BuffRegister</h1>

负责注册增益。是静态类（真的感觉没的介绍的东西了）

<h2>方法</h2>

```csharp
//注册新的增益
//HookType 需继承自 IBuffHook
public static void RegisterBuff<BuffType, DataType, HookType>(BuffID id);
```

```csharp
//注册新的增益
public static void RegisterBuff<BuffType, DataType>(BuffID id);
```

<h2>接口</h2>

```csharp
public interface IBuffHook
{
    //相当于OnModsInit()
    //请将增益需要的Hook在这里应用
    public void HookOn();
}
```
