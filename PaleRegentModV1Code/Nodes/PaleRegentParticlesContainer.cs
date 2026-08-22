using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace PaleRegentModV1.PaleRegentModV1Code.Nodes;

/// <summary>
/// 仅用于场景导出期的脚本解析。运行时保留游戏原版 NParticlesContainer 的粒子重启行为，
/// 让既有 Harmony 补丁继续作用于同一个 Restart 方法。
/// </summary>
public partial class PaleRegentParticlesContainer : NParticlesContainer
{
}
