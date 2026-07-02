# MixWinUI3UwpMsix

See https://github.com/Gaoyifei1011/MSIXIncludeWinUI2AndWinUI3

1. KernelBase 里的俩函数 win11 22000 才引入
2. 至少要给 WinUI 或 Uwp 其中一个的 App.xaml 更名避免冲突（改UWP的应该也可以没尝试过）

```xaml
<ApplicationDefinition Include="WinUIApp.xaml" />
<Page Remove="WinUIApp.xaml" />
```

3. 不知为啥 `<DisableRuntimeMarshalling>true</DisableRuntimeMarshalling>` 用不了得 `[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]`

---

UWP / WinUI Package 模版关键对比 （2026.7.2）

``` xml
<!--  UWP  -->
<DebuggerFlavor>AppHostLocalDebugger</DebuggerFlavor>
<RemoteDebugEnabled>False</RemoteDebugEnabled>
<DebuggerType>CoreClr</DebuggerType>
```

``` xml
<!--  WinUI  -->
<WapProjPath Condition="'$(WapProjPath)'==''">$(MSBuildExtensionsPath)\Microsoft\DesktopBridge\</WapProjPath>
<PathToXAMLWinRTImplementations>App1\</PathToXAMLWinRTImplementations>
<WinUISDKReferences>false</WinUISDKReferences>

<AssetTargetFallback>net8.0-windows$(TargetPlatformVersion);$(AssetTargetFallback)</AssetTargetFallback>
```

如果不引用 microsoft ui xaml 2.8.7，winui 和 uwp 混合 msix 似乎相互协议调用也非常简单

给所有引用的项目上 `SkipGetTargetFrameworkProperties` 就可以消除 NU1702 警告

warning NU1702: 已使用“.NETCoreApp,Version=v11.0”而不是项目目标框架“.NETFramework,Version=v4.5.1”解析 ProjectReference“C:\Users\user\source\repos\TestMsixUwp\App1\App1\App1.csproj”。此项目可能与你的项目不完全兼容。

UWP 一旦安装 MUXC 2.8.7，启动 winui 时就会提示

Cannot create instance of type 'Microsoft.UI.Xaml.Controls.XamlControlsResources' [Line: 10 Position: 40]

看上去只要不在 App xaml 中初始化 MUXC 就行了？实则不然，要知道现在 uwp winui 混合架构的包图中，winui 已经优先读取了 muxc 2.8.7 的包

即使不使用 muxc，即使不使用任何 winui 控件，但是如果你使用 xaml，背后生成的 XamlTypeInfo.g.cs 也会尝试去读取所有的 winui3 控件，此时会出现 CastInvalidException

> 一个好玩的事实是，如果把 winui WindowsAppSDKSelfContained,全打包进 msix 中，此时启动 uwp 主项目，会提示找不到 MUXC 了，看样子是优先去读 winui3 的包了。

最终结论是，为了正常让 winui 项目启动，在初始化时（App xaml 的构造函数即可），重新改变包图依赖顺序是有必要的
```csharp
private const string WindowsAppRuntimeFamilyName = "Microsoft.WindowsAppRuntime.2_8wekyb3d8bbwe";

public WinUIApp()
{
    // 把 Microsoft.WindowsAppRuntime.2 prepend 到当前进程 package graph”
    OsPlatformApi.TryRegisterDependency(WindowsAppRuntimeFamilyName, GetCurrentArchitecture());

    InitializeComponent();
}
```

下面这一串输出似乎是现在的 winui3 separate wapproj 模版启动就会输出的一串，默认就会有，uwp 创建的 wapproj 没这问题，可能与上面的一些 winui wapproj msbuild 属性有关

```
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936338190) ReturnHr(1) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\directxdatabasehelper.cpp(2315)\directxdatabasehelper.dll!00007FF936337040: (caller: 00007FF939A89D40) ReturnHr(2) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936317CA0) ReturnHr(3) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936338190) ReturnHr(4) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936317CA0) ReturnHr(5) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936338190) ReturnHr(6) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936338190) ReturnHr(7) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936317CA0) ReturnHr(8) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936317CA0) ReturnHr(9) tid(388c) 80070002 系统找不到指定的文件。
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936338190) ReturnHr(10) tid(37b4) 80070002 系统找不到指定的文件。
onecore\internal\sdk\inc\wil/resource.h(967)\dwmcorei.dll!00007FF855840658: (caller: 00007FF855848CAC) ReturnNt(1) tid(37b4) C0000022 onecoreuap\windows\frameworkudk\warppal.cpp(783)\Microsoft.Internal.WarpPal.dll!00007FF9377AE77C: (caller: 00007FF90A33F980) ReturnHr(1) tid(37b4) 80004002 不支持此接口
onecore\windows\directx\database\helperlibrary\lib\perappusersettingsqueryimpl.cpp(135)\directxdatabasehelper.dll!00007FF936317DF4: (caller: 00007FF936338190) ReturnHr(11) tid(1374) 80070002 系统找不到指定的文件。
线程 'ShellHandwriting Delegate Thread' (18144) 已退出，返回值为 0 (0x0)。
onecoreuap\windows\frameworkudk\warppal.cpp(783)\Microsoft.Internal.WarpPal.dll!00007FF9377AE77C: (caller: 00007FF90A33F980) ReturnHr(2) tid(1374) 80004002 不支持此接口
onecoreuap\windows\frameworkudk\warppal.cpp(783)\Microsoft.Internal.WarpPal.dll!00007FF9377AE77C: (caller: 00007FF90A33F980) ReturnHr(3) tid(1374) 80004002 不支持此接口
onecoreuap\windows\frameworkudk\warppal.cpp(783)\Microsoft.Internal.WarpPal.dll!00007FF9377AE77C: (caller: 00007FF938B7F614) ReturnHr(4) tid(37b4) 80004002 不支持此接口
Microsoft.UI.Xaml.dll!00007FF817DFB350: 80070057 - E_INVALIDARG
Microsoft.UI.Xaml.dll!00007FF817DFB350: 80070057 - E_INVALIDARG
Microsoft.UI.Xaml.dll!00007FF817DFB350: 80070057 - E_INVALIDARG
```
