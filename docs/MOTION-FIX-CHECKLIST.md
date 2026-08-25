# raindesk 动画修复清单

依据：`PROJECT-AUDIT.md` 的动画与性能发现，以及当前 `MotionTokens`、`SurfaceMotion`、`DialogAnimationHelper`、标签切换和拖拽实现。  
目标：统一交互手感，减少布局开销，支持连续操作与减少动态设置，并使关键交互稳定达到 60fps。

## P1：统一动画基础设施

### 1. 扩充并统一 MotionTokens

- [ ] 在 `Controls/MotionTokens.cs` 中定义完整时间语义，禁止业务代码新增裸毫秒值。
- [ ] 保留 `Feedback = 120ms`，用于按压、标签内容交叉淡化和即时状态反馈。
- [ ] 保留 `Fast = 180ms`，用于拖拽放下、菜单和轻量状态切换。
- [ ] 保留 `Standard = 220ms`，用于普通面板和层级切换。
- [ ] 保留 `Exit = 160ms`，退出约为进入时长的 70%-75%。
- [ ] 新增语义化位移：普通内容 6px，二级内容 4px，轻微反馈 3px。
- [ ] 新增语义化缩放：普通弹窗进入 0.975、退出 0.985；文件夹浮层进入 0.97；拖拽拿起 1.02。
- [ ] 所有进入使用统一 Exponential EaseOut；所有退出使用统一 EaseIn，不使用 bounce 或 elastic。

验收：全项目动画常量搜索结果中，不再出现未解释的 320ms、220ms、8px、0.96 等局部手感常量；例外必须注明空间层级原因。

### 2. 让 SurfaceMotion 支持连续动画

文件：`Controls/SurfaceMotion.cs`

- [ ] 把默认 `translateY` 从 8 调整为 6。
- [ ] 增加二级内容入口，默认位移 3-4px。
- [ ] 启动新动画前读取当前 Opacity、Translation 和 Scale，从当前视觉状态继续。
- [ ] `Stop` 不再无条件随后重置视觉值；区分“中断并保留当前值”和“结束并归一化”。
- [ ] 为每个目标维护 generation/version，完成回调先验证版本再修改状态。
- [ ] 监听 `Unloaded`，停止 Storyboard 并使完成回调失效。
- [ ] 提供显式 `Cancel(target, resetToFinalState)`，供窗口隐藏、导航和控件回收调用。
- [ ] 减少动态时直接设置最终值，且不排队异步完成回调。

验收：连续快速打开/关闭 10 次不会闪回初始状态；卸载控件后旧回调不会更改其 Opacity 或 Transform。

## P1：弹窗与浮层

### 3. 收敛 DialogAnimationHelper

文件：`Controls/DialogAnimationHelper.cs`、`Controls/DialogService.cs`

- [ ] 删除 `EntranceDurationMs = 320` 和 `ExitDurationMs = 220`。
- [ ] 普通弹窗进入使用 `MotionTokens.Standard`，淡入不超过 180ms。
- [ ] 普通弹窗缩放从 0.975 到 1，垂直位移不超过 4px。
- [ ] 普通弹窗退出使用 `MotionTokens.Exit`，缩放到 0.985，位移不超过 3px。
- [ ] 缓动全部使用 `MotionTokens.EaseOut/EaseIn`。
- [ ] 复用与 `SurfaceMotion` 相同的状态、版本和取消策略，避免两套生命周期逻辑。
- [ ] 关闭过程中再次打开时，从当前值反向播放，不先跳到 1 或 0.975。
- [ ] Dialog 内容卸载或宿主窗口关闭时停止 Storyboard。

验收：普通弹窗进入不超过 220ms，退出不超过 160ms；快速开关无闪烁、无重复完成回调。

### 4. 保留文件夹浮层的空间层级

文件：`Features/Launcher/Presentation/WinUI/FolderOverlayMotionController.cs`

- [ ] 进入缩放由当前 0.96 调整为 0.97。
- [ ] 进入使用 `MotionTokens.Standard`，退出使用 `MotionTokens.Exit`。
- [ ] 背景遮罩只做淡化，文件夹面板做 0.97 到 1 的缩放和最多 4px 位移。
- [ ] 快速切换文件夹时复用当前浮层，不先销毁再重建动画状态。
- [ ] 减少动态时立即展示最终层级，不做缩放。

验收：文件夹浮层比普通弹窗略强，但无明显“弹出”或放大感；连续切换文件夹保持视觉连续。

## P1：标签与内容切换

### 5. 工作区标签共享指示器只动画 Transform

文件：`Features/Workspace/Presentation/WinUI/WorkspaceGroupTabSelectionIndicatorController.cs`、`Control/UserControls/PannelCard/RightCardControl.xaml`

- [ ] 保留单一 `GroupTabSelectionIndicator`，不为每个标签创建独立选中背景。
- [ ] 用 Translation 移动指示器，用 Scale 表达宽高变化。
- [ ] 删除 `Canvas.Top` 和 `Height` 的逐帧动画；结束时一次性写入最终布局值。
- [ ] 中断时从当前 Translation/Scale 接续到新标签。
- [ ] 标签快速连续切换时只保留最新目标，旧完成回调不得覆盖新状态。

验收：连续跨越多个标签时指示器沿最短路径移动，无跳回、无强制布局和重影。

### 6. 待办标签改为共享指示器与交叉淡化

文件：`Control/UserControls/ToDo/TodoPanel.xaml`、`TodoPanel.xaml.cs`

- [ ] 移除每个 `TodoTabButtonStyle` 内独立的 Indicator 状态切换。
- [ ] 在标签容器上增加一个共享指示器，根据选中项边界移动。
- [ ] 内容区使用约 `MotionTokens.Feedback` 的 120ms 交叉淡化。
- [ ] 标签切换不再让整个任务列表从 4px 或更大距离重复入场。
- [ ] 筛选、排序和刷新只更新内容，不播放页面级入场。

验收：标签切换只有指示器移动和短交叉淡化；快速点击三个标签时最终状态正确且无排队动画。

## P1：拖拽直接操控

### 7. 统一拿起、重排和放下反馈

文件：`Util/DragAdorner.cs`、`Features/Launcher/Presentation/WinUI/LauncherDragOrchestrator.cs`、`LauncherItemAnimationCoordinator.cs`、`LauncherDropTargetVisualAdapter.cs`

- [ ] 拿起时缩放到 1.02，不超过 120ms。
- [ ] 通过 Z 层级或受限阴影表达提升，不扩大位移幅度。
- [ ] 列表重排只动画 Translation/位置，不动画尺寸、边距或 Opacity。
- [ ] 放下在 160-200ms 内回到 1.0 和最终位置。
- [ ] 删除 bounce、elastic 或过冲；统一使用 EaseOut。
- [ ] 拖拽取消与成功放下使用同一收敛路径，只有最终目标不同。
- [ ] 合并 16ms 帧驱动器，确保同一拖拽会话只有一个 UI tick 来源。
- [ ] 帧循环中禁止新增 `UpdateLayout()`；几何信息从缓存或布局事件获得。

验收：拖拽期间 P95 帧耗时小于 16.7ms；拿起、移动、放下没有尺寸跳变；减少动态时仍保留清晰的选中和放置状态但无动画。

## P2：避免重复入场

### 8. 为页面和层级建立入场策略

文件：设置页、待办页、主面板及其导航控制器

- [ ] 仅首次打开页面时播放普通入场。
- [ ] 仅真正发生层级变化时播放二级入场，例如进入文件夹、打开编辑面板。
- [ ] 筛选、搜索结果更新、排序、数据刷新不播放整页入场。
- [ ] 窗口重新获得焦点、从后台恢复或重复选择当前标签不播放入场。
- [ ] 页面实例记录 `HasPlayedEntrance`，导航层记录当前层级键。
- [ ] 数据加载只对新增项使用位置动画，不重新动画已有项。

验收：同一页面连续筛选或刷新 20 次不会重复整体淡入或位移；返回上一级只动画发生变化的层级。

## P2：生命周期、无障碍与性能

### 9. 完善动画取消和过期保护

- [ ] 所有可编程 Storyboard 均持有引用并可停止。
- [ ] 所有 Completed 回调检查当前 generation 或 Storyboard 引用。
- [ ] `Unloaded`、窗口隐藏、关闭、导航替换时统一取消动画。
- [ ] 被虚拟化回收的 ListViewItem 不保留旧 Transform、Opacity 或事件处理器。
- [ ] 动画取消后确保 IsHitTestVisible、Visibility、Opacity 和 Transform 处于一致状态。

验收：使用快速点击、Esc、窗口隐藏、拖拽取消和列表虚拟化进行压力测试，不出现不可点击、透明残留或过期回调。

### 10. 强制统一减少动态入口

文件：`Controls/MotionPreferences.cs` 及所有动画调用点

- [ ] 所有动画入口先检查 `MotionPreferences.ShouldAnimate`。
- [x] 已移除应用性能模式（PMModel），动画开关统一走系统减少动画偏好。
- [ ] 减少动态模式直接设置最终视觉状态，不使用 0ms Storyboard。
- [ ] 关键状态不能只靠动画表达，拖拽目标、选中、错误和完成状态需有静态视觉反馈。

验收：关闭 Windows 动画或应用动画后，全流程不产生 Storyboard；交互状态仍清晰可辨。

### 11. 禁止布局属性动画

- [ ] 搜索并移除非必要的 Width、Height、Canvas.Top、Margin 动画。
- [ ] 位置变化使用 Translation 或 FLIP。
- [ ] 尺寸变化优先 Scale；必须改变布局时，在动画结束后一次性提交最终尺寸。
- [ ] Opacity、Scale、Translation 之外的效果必须附带性能验证记录。

验收：动画专项搜索不再发现未经批准的布局属性 Storyboard；目标设备上稳定达到 60fps。

## 建议实施批次

1. **基础设施**：MotionTokens、SurfaceMotion、动画取消和减少动态。
2. **弹窗与浮层**：DialogAnimationHelper、DialogService、FolderOverlayMotionController。
3. **标签切换**：工作区共享指示器、待办共享指示器、内容交叉淡化。
4. **拖拽链路**：拿起、重排、放下、单一帧循环和布局缓存。
5. **页面策略**：首次入场、层级入场、刷新不重复入场。
6. **验证与收尾**：减少动态、连续操作、卸载、虚拟化、60fps 和 `$impeccable polish`。

## 完成定义

- [ ] 动画时长、缓动、位移和缩放全部来自 MotionTokens。
- [ ] 普通内容进入位移 6px，二级内容不超过 4px。
- [ ] 普通弹窗 0.975 到 1，退出到 0.985；文件夹浮层 0.97 到 1。
- [ ] 标签使用共享指示器，内容交叉淡化约 120ms。
- [ ] 拖拽拿起 1.02，放下 160-200ms，无弹跳。
- [ ] 刷新、筛选和焦点恢复不触发整页入场。
- [ ] 动画可从当前状态中断续播，卸载后无过期回调。
- [ ] 减少动态模式立即显示最终状态。
- [ ] 高频动画只使用 Opacity、Scale、Translation。
- [ ] 目标设备关键交互达到稳定 60fps。
