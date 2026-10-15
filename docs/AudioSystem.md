# 轻量游戏音频系统

适用版本：Unity 2022.3.62f3c1。使用 Unity 原生 AudioSource、AudioMixer、ScriptableObject、PlayerPrefs，无新增 Package。

## 已实现与接入范围

- 全局管理器、四类音频分组、五路音量保存、BGM / 环境音循环与交叉淡化。
- 固定音效池、随机音频变体及音高、2D / 3D 播放、跟随物体、停止句柄。
- 根据当前主场景路径自动选择配置。场景切换仍使用项目原有流程。
- 独立试听场景及合成占位音频。现有交互、脚步、UI 按钮没有自动绑定音效；可通过公开接口或 AudioEmitter 的 UnityEvent 按需接入。

## 文件与职责

```text
Assets/
  Scripts/Core/Audio/
    AudioManager.cs          全局入口、场景事件、音量与生命周期
    AudioRuntime.cs          持久化宿主、固定声源池与淡化
    AudioCategory.cs         BGM / SFX / UI / Ambience
    SoundDefinition.cs       音效配置
    SceneAudioProfile.cs     一套场景的 BGM 与环境音
    AudioSettings.cs         Mixer、声源数量、场景路径映射
    AudioEmitter.cs          可选 Inspector / UnityEvent 入口
  Scripts/Debug/AudioSystemDemo.cs
  Art/Audio/
    BGM/                     奶茶店、洞穴占位循环
    SFX/                     三个石质脚步占位变体
    UI/                      点击占位音
    Ambience/                店内低鸣、洞穴风声占位循环
  Configs/Audio/
    Sounds/                  六个 SoundDefinition
    Profiles/                milk_tea_audio、goblin_cave_audio
    Resources/Audio/audio_settings.asset
  Settings/Audio/game_audio.mixer
  Scenes/Audio/AudioSystemDemo.unity
```

只有全局入口配置放在嵌套 Resources 下，其他配置和音频由资产引用带入。保持资源键 `Audio/audio_settings` 唯一。这里资源规模很小，采用同步加载；正式长音乐或大量资源的异步加载可在出现需求后接入现有 Addressables。

## 启动与生命周期

AudioManager 是沿用 `ManagerBase<AudioManager>` 的纯 C# 服务。`BeforeSceneLoad` 自动初始化，Bootstrap.Awake 也调用幂等的 `EnsureInitialized()`，因此直接打开试听或关卡场景无需额外挂管理器。

AudioRuntime 是管理器创建的唯一 MonoBehaviour 宿主，使用 DontDestroyOnLoad。它拥有全部 AudioSource；不要手工往场景或 Prefab 添加 AudioRuntime。重复初始化复用管理器，不增加声源。SubsystemRegistration 清理静态服务、事件及旧宿主，兼容关闭 Domain Reload 的 Play Mode。销毁宿主或 Dispose 时成对退订场景事件。

系统不创建或移动 AudioListener。第一人称应使用玩家相机上的 Listener；第三人称默认以相机 Listener 的位置和朝向计算 3D 音效。每个运行场景保留一个启用的 Listener；需要以角色为听觉中心时，由相机/角色模块明确设置 Listener。

## Mixer 与音量

```text
Master  [MasterVolume]
  BGM       [BGMVolume]
  SFX       [SFXVolume]
  UI        [UIVolume]
  Ambience  [AmbienceVolume]
```

Mixer 已创建分组、Attenuation 和五个暴露参数，配置已引用对应分组。可直接打开 Mixer 调整。不要重命名暴露参数，代码使用这些固定名称。

所有音量接口接收 0～1：`0` 映射到 -80 dB，其他值使用 `20 * log10(value)`，0.5 约为 -6.02 dB。总音量与分类音量在 Mixer 中叠加。默认全部为 1，素材自身响度由 SoundDefinition.Volume 控制。

音量缓存在管理器并写入 PlayerPrefs，键前缀为 `Untitled.Audio.`。确认设置时调用 `SaveVolumes()`；退出或应用进入后台也保存。Mixer 参数在宿主 Start 后应用，避免在 Awake / OnEnable 中设置 Mixer 的初始化时序问题，见 [Unity 2022.3 SetFloat 文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html)。

```csharp
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetVolume(AudioCategory.BGM, 0.5f);
AudioManager.Instance.SetVolume(AudioCategory.SFX, 0.9f);
float uiVolume = AudioManager.Instance.GetVolume(AudioCategory.UI);
AudioManager.Instance.SaveVolumes();
```

## 配置音效与场景

在 Project 中选择 `Create > Untitled > Audio > Sound`：

| 字段 | 用途 |
| --- | --- |
| Category | 决定 Mixer 路由；UI 始终为 2D |
| Clips | 每次播放均匀随机选择一个有效片段，允许连续抽到相同片段；空引用忽略 |
| Volume | 单次播放响度，0～1 |
| Pitch Range | 每次随机音高，运行时限制在 0.1～3；音乐建议 1～1 |
| Spatial Blend | Play / AudioEmitter 使用的 2D～3D 混合比例 |
| Min / Max Distance | 3D 对数距离衰减参数；Max Distance 是衰减曲线参数，不能视为硬静音半径 |

示例 `stone_step.asset` 配置三个变体，音高 0.92～1.08、空间混合 1、距离 1～20。`ui_click.asset` 为 UI 分类。BGM 和场景 Ambience 无论资源的 Spatial Blend 如何配置，均使用 2D 循环。

选择 `Create > Untitled > Audio > Scene Profile`，设置 BGM、Ambience、Fade Seconds。BGM 槽只接受 BGM 分类，Ambience 槽只接受 Ambience 分类。任一槽留空会淡出对应循环。已有两套配置默认淡化 1.5 秒。

在 `audio_settings.asset` 的 Scenes 数组中登记完整项目相对路径：

| 路径 | 配置 |
| --- | --- |
| Assets/Scenes/Gameplay/Bussiness.unity | 奶茶店 |
| Assets/Scenes/BussinessWorkstation.unity | 奶茶店 |
| Assets/Scenes/MilkTeaShop.unity | 奶茶店 |
| Assets/Scenes/MilkTeaShopReference.unity | 奶茶店 |
| Assets/Scenes/Gameplay/Adventure.unity | 洞穴 |
| Assets/Scenes/Gameplay/Cave.unity | 洞穴 |
| Assets/Scenes/GoblinCave200.unity | 洞穴 |
| Assets/Scenes/Audio/AudioSystemDemo.unity | 奶茶店 |

直接进入以上场景或用已有流程加载，会自动播放对应音乐与环境音。相同 Profile 连续切换场景会延续播放；未登记场景默认用 1.5 秒淡出循环。其他洞穴预览场景按需显式登记，不根据名称模糊推断。重命名或移动场景后需要更新路径。

Single 加载监听 sceneLoaded；Additive 加载保持当前主场景音乐，调用 `SceneManager.SetActiveScene` 后再切换，见 [Unity activeSceneChanged](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SceneManagement.SceneManager-activeSceneChanged.html)。没有新增场景跳转、修改 Build Settings 或假定预览场景已加入构建。

## 播放接口

在调用处用 Inspector 引用 SoundDefinition，不按字符串每次查找资源：

```csharp
using UnityEngine;
using Untitled;

public sealed class AudioUsageExample : MonoBehaviour
{
    [SerializeField] private SoundDefinition click;
    [SerializeField] private SoundDefinition step;
    [SerializeField] private SoundDefinition music;

    public void Click() => AudioManager.Instance.Play2D(click);

    public void Step()
    {
        // 根据配置的 Spatial Blend 播放，并跟随当前物体。
        AudioManager.Instance.Play(step, transform.position, transform);
    }

    public void ImpactAt(Vector3 point)
    {
        // 强制 3D；Play2D 则强制 2D。UI 分类始终保持 2D。
        int handle = AudioManager.Instance.Play3D(step, point);
        // 需要提前中止时：AudioManager.Instance.Stop(handle);
    }

    public void ChangeMusic() => AudioManager.Instance.PlayBgm(music, 2f);
    public void FadeOutMusic() => AudioManager.Instance.StopBgm(1f);
}
```

完整场景音频可调用 `PlayProfile(profile)`。PlayBgm / StopBgm 只影响 BGM，下次场景事件会恢复场景配置。Play / Play2D / Play3D 播放一次，不能用来播放 BGM；Ambience 分类的一次性声音可用这些接口按 Ambience 分组播放。

不写代码的接入方式：给交互物体添加 AudioEmitter，拖入 Sound，现有 UnityEvent 绑定 `AudioEmitter.Play()`。Play On Enable 可选；Follow Transform 默认开启，Stop On Disable 默认开启。单个 Emitter 重播会先停止自身上一次声音。跟随目标销毁时释放其声道，静态坐标播放则自然播完。

播放返回 0 表示未播放（例如未配置资源）；其他值是停止句柄。池满替换后，旧句柄不再控制新声音。不要直接修改管理器内部 AudioSource，也不要用 AudioSource.Pause 代替统一暂停，否则无法区分暂停和自然结束。

## 声源上限、淡化与暂停

默认总数固定为 **24**：BGM 2、Ambience 2、SFX 16、UI 4。设置资产可调整 SFX / UI 数量，在下次初始化时生效。一次性 Ambience 与 SFX 共用 16 个声道，但 Mixer 路由独立。自然结束会清理 clip 引用，声源供下次复用。

池满时覆盖同池中最早播放的声音，UI 独立池不受 SFX 洪峰影响。不会使用 PlayClipAtPoint 或无上限 PlayOneShot 叠加。高频调用的响度、触发频率仍应由具体交互控制。

BGM / 环境音各保留两个声源，按当前音量线性交叉淡化。淡化期间再次切换时保留较响的一侧、复用较轻的一侧，总数不增长；极快速连切可能截断较轻的一侧。循环选择一个随机片段后持续循环，不会每轮重新抽取变体。

淡化采用 unscaledDeltaTime，因此 `Time.timeScale = 0` 时仍可切换音乐。需要暂停世界音频时使用 `AudioListener.pause`；UI 声源设为 ignoreListenerPause，仍可播放。暂停时池不会错误回收世界音效，见 [Unity AudioListener.pause](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AudioListener-pause.html)。

## 占位素材与人工试听

本次八个 WAV 是数学合成的短旋律、低鸣、风声、噪声脚步与点击，无第三方素材；用于功能演示，尚未进行正式作曲、录音或响度母带制作。可将正式资源导入对应目录，再替换 SoundDefinition.Clips 引用。长 BGM 可按设备目标调整 Streaming，短 SFX 通常保留 Decompress On Load。

1. 打开 AudioSystemDemo，Play 后试听两套 Profile、淡出与随机脚步；检查左右 3D 位置及五路音量，保存后重新 Play 确认恢复。
2. 点击 Pool limit，Hierarchy 的 AudioManager 下应始终为 24 个 AudioSource，UI 点击仍能播放。
3. 分别直接运行已映射的奶茶店与洞穴场景，确认自动选曲及当前相机的听觉位置；通过现有场景切换入口检查过渡。最终淡化时间、响度和第一/第三人称距离感由人工试听确认。

## 本次验证

已在 Unity 2022.3 Editor 中完成编译及一次针对性 Play Mode 检查：重复初始化单例、24 声源上限、16 个 SFX 满载时 UI 独立播放、三个随机变体、2D / 3D 参数与位置、停止旧句柄、自然结束回收、Mixer 音量换算、双声源交叉淡化结束后释放旧片段、未登记主场景淡出，以及 Single 重载已映射场景后保留同一宿主并自动恢复 BGM / Ambience。检查脚本未加入项目。

没有执行完整构建；没有对现有奶茶店 / 洞穴的最终听感作人工验收。
