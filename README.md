# MixWinUI3UwpMsix

See https://github.com/Gaoyifei1011/MSIXIncludeWinUI2AndWinUI3

1. KernelBase 里的俩函数 win11 22000 才引入
2. 至少要给 WinUI 或 Uwp 其中一个的 App.xaml 更名避免冲突（改UWP的应该也可以没尝试过）

```xaml
<ApplicationDefinition Include="WinUIApp.xaml" />
<Page Remove="WinUIApp.xaml" />
```

3. 不知为啥 `<DisableRuntimeMarshalling>true</DisableRuntimeMarshalling>` 用不了得 `[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]`
