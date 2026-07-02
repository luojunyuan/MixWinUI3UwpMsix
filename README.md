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
