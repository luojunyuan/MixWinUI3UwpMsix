using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.System;
using WinRT;
using PkgEnum = WinUI3App.WindowsAPI.PInvoke.KernelBase;

namespace MixWinUI3UwpMsix
{
    internal class Program
    {
        [STAThread]
        [SupportedOSPlatform("windows10.0.22000.0")]
        public static void Main()
        {
            ComWrappersSupport.InitializeComWrappers();

            // 使用 MSIX 动态依赖包 API，强行修改静态包图的依赖顺序，解决 WinUI 3 桌面应用程序加载时错误加载成 WinUI 2 程序集，导致程序启动失败的问题
            IReadOnlyList<Package> dependencyPackageList = Package.Current.Dependencies;
            PackageDependencyProcessorArchitectures packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.None;

            switch (Package.Current.Id.Architecture)
            {
                case ProcessorArchitecture.X86:
                    {
                        packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.X86;
                        break;
                    }
                case ProcessorArchitecture.X64:
                    {
                        packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.X64;
                        break;
                    }
                case ProcessorArchitecture.Arm64:
                    {
                        packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.Arm64;
                        break;
                    }
                case ProcessorArchitecture.X86OnArm64:
                    {
                        packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.X86OnArm64;
                        break;
                    }
                case ProcessorArchitecture.Neutral:
                    {
                        packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.Neutral;
                        break;
                    }
                case ProcessorArchitecture.Unknown:
                    {
                        packageDependencyProcessorArchitectures = PackageDependencyProcessorArchitectures.None;
                        break;
                    }
            }

            foreach (Package dependencyPacakge in dependencyPackageList)
            {
                if (dependencyPacakge.DisplayName.Contains("WindowsAppRuntime") && 
                    KernelBaseLibrary.TryCreatePackageDependency(
                        IntPtr.Zero, 
                        dependencyPacakge.Id.FamilyName, 
                        new Windows.ApplicationModel.PackageVersion(), 
                        packageDependencyProcessorArchitectures, 
                        PackageDependencyLifetimeArtifactKind.Process, 
                        string.Empty,
                        PkgEnum.CreatePackageDependencyOptions.CreatePackageDependencyOptions_None, 
                        out string packageDependencyId) is 0)
                {
                    if (KernelBaseLibrary.AddPackageDependency(
                        packageDependencyId, 
                        0,
                        PkgEnum.AddPackageDependencyOptions.AddPackageDependencyOptions_PrependIfRankCollision, 
                        out _, 
                        out _) is 0)
                    {
                        break;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // 启动桌面程序
            Application.Start((param) =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()));
                new WinUIApp();
            });
        }
    }

    public static partial class KernelBaseLibrary
    {
        private const string KernelBase = "kernelBase.dll";

        /// <summary>
        /// 使用 TryCreatePackageDependency 方法添加前面创建的框架包依赖项的运行时引用和指定选项。 此方法成功返回后，应用可以激活类型并使用框架包中的内容。
        /// </summary>
        /// <param name="packageDependencyId">要解析并添加到调用进程的包图的包依赖项的 ID。 此参数必须与通过使用 TryCreatePackageDependency 函数通过) CreatePackageDependencyOptions_ScopeIsSystem 选项对调用用户或系统 (定义的 包 依赖项匹配，否则返回错误。</param>
        /// <param name="rank">用于将解析的包添加到调用方包图的排名。</param>
        /// <param name="options">添加包依赖项时要应用的选项。</param>
        /// <param name="packageDependencyContext">添加的包依赖项的句柄。 此句柄在传递到 RemovePackageDependency 之前有效。</param>
        /// <param name="packageFullName">此方法返回时，包含指向以 null 结尾的 Unicode 字符串的指针的地址，该字符串指定已解析依赖项的包的全名。 调用 HeapFree 不再需要此资源后，调用方负责释放此资源。</param>
        /// <returns>如果函数成功，则返回 ERROR_SUCCESS。 否则，函数将返回错误代码。</returns>
        [LibraryImport(KernelBase, EntryPoint = "AddPackageDependency", SetLastError = false, StringMarshalling = StringMarshalling.Utf16), PreserveSig]
        public static partial int AddPackageDependency([MarshalAs(UnmanagedType.LPWStr)] string packageDependencyId, int rank, PkgEnum.AddPackageDependencyOptions options, out PackageDependencyContextId packageDependencyContext, [MarshalAs(UnmanagedType.LPWStr)] out string packageFullName);

        /// <summary>
        /// 使用指定的包系列名称、最低版本和其他条件，为当前应用的框架包依赖项创建安装时引用。
        /// </summary>
        /// <param name="user">包依赖项的用户范围。 如果为 NULL，则使用调用方的用户上下文。 如果指定 了CreatePackageDependencyOptions_ScopeIsSystem ，则必须为 NULL。</param>
        /// <param name="packageFamilyName">要依赖的框架包的包系列名称。</param>
        /// <param name="minVersion">要对其具有依赖项的框架包的最低版本。</param>
        /// <param name="packageDependencyProcessorArchitectures">包依赖项的处理器体系结构。</param>
        /// <param name="lifetimeKind">用于定义包依赖项生存期的项目类型。</param>
        /// <param name="lifetimeArtifact">用于定义包依赖项生存期的项目的名称。 如果PackageDependencyLifetimeKind_Process lifetimeKind 参数，则必须为 NULL。 有关详细信息，请参阅备注。</param>
        /// <param name="options">创建包依赖项时要应用的选项。</param>
        /// <param name="packageDependencyId">此方法返回时，包含指向以 null 结尾的 Unicode 字符串的指针的地址，该字符串指定新包依赖项的 ID。 调用 HeapFree 不再需要此资源后，调用方负责释放此资源。</param>
        /// <returns>如果该函数成功，则返回 ERROR_SUCCESS。 否则，该函数将返回错误代码。</returns>
        [LibraryImport(KernelBase, EntryPoint = "TryCreatePackageDependency", SetLastError = true, StringMarshalling = StringMarshalling.Utf16), PreserveSig]
        public static partial int TryCreatePackageDependency(nint user, [MarshalAs(UnmanagedType.LPWStr)] string packageFamilyName, Windows.ApplicationModel.PackageVersion minVersion, PackageDependencyProcessorArchitectures packageDependencyProcessorArchitectures, PackageDependencyLifetimeArtifactKind lifetimeKind, [MarshalAs(UnmanagedType.LPWStr)] string lifetimeArtifact, PkgEnum.CreatePackageDependencyOptions options, [MarshalAs(UnmanagedType.LPWStr)] out string packageDependencyId);
    }
}

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace WinUI3App.WindowsAPI.PInvoke.KernelBase
{
    /// <summary>
    /// 定义在使用 AddPackageDependency 函数添加对框架包的运行时引用时可以应用的选项。
    /// </summary>
    [Flags]
    public enum AddPackageDependencyOptions
    {
        /// <summary>
        /// 未应用任何选项。
        /// </summary>
        AddPackageDependencyOptions_None = 0x00000000,

        /// <summary>
        /// 如果包图中存在多个包，其排名与调用 AddPackageDependency 相同，则解析的包将先于排名相同的其他包。
        /// </summary>
        AddPackageDependencyOptions_PrependIfRankCollision = 0x00000001,
    }
}

namespace WinUI3App.WindowsAPI.PInvoke.KernelBase
{
    /// <summary>
    /// 定义在使用 TryCreatePackageDependency 函数创建包依赖项时可以应用的选项。
    /// </summary>
    [Flags]
    public enum CreatePackageDependencyOptions
    {
        /// <summary>
        /// 未应用任何选项。
        /// </summary>
        CreatePackageDependencyOptions_None = 0x00000000,

        /// <summary>
        /// 在固定包依赖项时禁用依赖项解析。 这对于作为目标用户以外的用户上下文运行的安装程序非常有用， (例如，作为 LocalSystem) 运行的安装程序。
        /// </summary>
        CreatePackageDependencyOptions_DoNotVerifyDependencyResolution = 0x00000001,

        /// <summary>
        /// 定义系统的包依赖项，默认情况下 (所有用户均可访问，包依赖项是为特定用户) 定义的。 此选项要求调用方具有管理权限。
        /// </summary>
        CreatePackageDependencyOptions_ScopeIsSystem = 0x00000002,
    }
}