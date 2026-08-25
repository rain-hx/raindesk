---
name: rainspace (rainspace)
description: 现代、活力、友好的 Windows 桌面效率工具
colors:
  clear-lake-blue: "#087BFF"
  dark-navy: "#071A4D"
  light-cyan-white: "#EAFBFDFF"
  blue-gray: "#E5ECF7"
  soft-blue: "#F5F9FF"
  whisper-blue: "#F6FAFF"
  divider-blue: "#E7EDF6"
  medium-slate: "#56658C"
  accent-light: "#EAF4FF"
  pure-white: "#FDFEFF"
  control-white: "#FFFFFF"
  control-border: "#DCE7F5"
  success-green: "#10C875"
  warning-amber: "#F59E0B"
  soft-purple: "#8B5CF6"
  alert-red: "#EF4444"
  fresh-cyan: "#06B6D4"
typography:
  display:
    fontFamily: "Segoe UI, Microsoft YaHei, sans-serif"
    fontSize: "22px"
    fontWeight: 600
    lineHeight: 1.3
  body:
    fontFamily: "Segoe UI, Microsoft YaHei, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Segoe UI, Microsoft YaHei, sans-serif"
    fontSize: "13px"
    fontWeight: 600
    lineHeight: 1.4
  icon:
    fontFamily: "Segoe Fluent Icons"
    fontSize: "22px"
rounded:
  sm: "8px"
  md: "10px"
  lg: "14px"
spacing:
  xs: "2px"
  sm: "4px"
  md: "8px"
  lg: "16px"
  xl: "22px"
components:
  button-primary:
    backgroundColor: "{colors.clear-lake-blue}"
    textColor: "{colors.control-white}"
    rounded: "{rounded.md}"
    padding: "22px 10px"
  button-secondary:
    backgroundColor: "{colors.pure-white}"
    textColor: "{colors.dark-navy}"
    rounded: "{rounded.md}"
    padding: "16px 8px"
  card:
    backgroundColor: "{colors.light-cyan-white}"
    textColor: "{colors.dark-navy}"
    rounded: "{rounded.lg}"
    padding: "16px"
  input:
    backgroundColor: "{colors.control-white}"
    textColor: "{colors.dark-navy}"
    rounded: "{rounded.md}"
    padding: "10px 6px"
---

# Design System: rainspace (rainspace)

## 1. Overview

**Creative North Star: "雨滴轻语" (Raindrop Whisper)**

rainspace 的设计系统传达一种轻盈、流畅、舒适的桌面体验。就像雨滴轻柔地落在窗上，这个界面应该让人感到平静而专注，同时保持现代感和活力。

整体设计哲学是"柔和的友好"——圆润的边角、充足的留白、温和的色彩对比，让普通办公用户感到亲切而非压迫。系统明确拒绝过度装饰或游戏化的风格，坚持 Windows 11 原生设计语言的简洁与优雅。

**Key Characteristics:**
- 轻盈透气：大量留白，避免视觉拥挤
- 柔和友好：圆润边角，温和色彩，易于亲近
- 现代活力：适度使用亮色点缀，保持界面生命力
- 系统一致：遵循 Fluent Design，感觉像 Windows 原生应用

## 2. Colors

色彩系统以"清澈湖蓝"为主色调，搭配中性蓝灰色调，营造清新专业的工作氛围。

### Primary
- **清澈湖蓝** (#087BFF): 主要交互元素、按钮、链接、选中状态。这是界面中最具活力的颜色，用于引导用户注意力。

### Secondary
- **活力绿** (#10C875): 成功状态、完成确认、正面反馈
- **警告琥珀** (#F59E0B): 警告状态、需要注意的信息
- **柔紫** (#8B5CF6): 特殊标记、高级功能
- **警示红** (#EF4444): 错误状态、危险操作、删除确认
- **清新青** (#06B6D4): 辅助信息、次要操作

### Neutral
- **深海军蓝** (#071A4D): 主要文本、标题、强调内容
- **中石板灰** (#56658C): 次要文本、描述、辅助信息
- **柔蓝白** (#F5F9FF): 悬停状态、轻微高亮
- **耳语蓝** (#F6FAFF): 背景微妙变化、分割区域
- **纯净白** (#FFFFFF): 输入框背景、卡片表面
- **控制边框蓝** (#DCE7F5): 输入框、按钮边框

### Named Rules

**清澈湖蓝规则**：主色仅用于 ≤15% 的界面面积。它的稀缺性正是重点——过度使用会削弱其引导作用。

**柔和对比规则**：文本与背景的对比度保持在 4.5:1 以上，但避免纯黑配纯白的强烈对比。使用深海军蓝 (#071A4D) 配柔蓝白背景，既保证可读性又保持柔和感。

## 3. Typography

**Display Font:** Segoe UI (with Microsoft YaHei fallback for CJK characters)
**Body Font:** Segoe UI (with Microsoft YaHei fallback)
**Icon Font:** Segoe Fluent Icons

**Character:** 清晰、现代、易读。Segoe UI 是 Windows 系统原生字体，与 Fluent Design 完美契合，同时对中文有良好的支持。

### Hierarchy
- **Display** (SemiBold, 22px, line-height 1.3): 页面标题、主要标题。用于当前视图的核心标识。
- **Headline** (SemiBold, 16px, line-height 1.4): 区域标题、卡片标题。用于分组内容的标识。
- **Title** (SemiBold, 15px, line-height 1.4): 子区域标题、设置分类。用于二级内容分组。
- **Body** (Regular, 14px, line-height 1.5): 正文内容、描述文本。最大行宽 65-75ch。
- **Label** (SemiBold, 13px, line-height 1.4): 按钮文本、标签、小标题。紧凑场景使用。

### Named Rules

**系统字体规则**：始终使用 Segoe UI 作为主字体，不引入自定义字体。这保证了与 Windows 系统的一致性，同时避免了字体加载问题。

**中文适配规则**：所有文本元素必须同时考虑英文和中文显示。中文行高通常需要比英文大 0.1-0.2，确保中英文混排时的阅读舒适度。

## 4. Elevation

系统采用"扁平优先，状态提升"的层级策略。默认状态下界面元素是扁平的，只有在交互状态（悬停、聚焦、按下）时才通过微妙的阴影或背景变化来表达层级。

### Shadow Vocabulary
- **Ambient Low** (`box-shadow: 0 4px 24px rgba(0,0,0,0.12)`): 卡片悬停时的轻微阴影，表示可交互性。
- **Surface Lift** (`box-shadow: 0 2px 8px rgba(0,0,0,0.08)`): 弹出菜单、工具提示的轻微浮起效果。

### Named Rules

**扁平默认规则**：界面在静止状态时保持扁平。阴影仅作为状态响应出现（悬停、提升、聚焦），而不是装饰性元素。

**柔和阴影规则**：阴影使用低透明度（0.08-0.12）和较大模糊半径（8-24px），营造柔和的浮起效果，避免生硬的投影。

## 5. Components

### Buttons
- **Shape:** 圆润友好 (CornerRadius 8-10px)
- **Primary:** 清澈湖蓝背景 (#087BFF)，白色文字，SemiBold 字重，内边距 10,7。用于主要操作。
- **Hover:** 背景色加深 10%，轻微上移效果。
- **Secondary:** 白色背景，深海军蓝文字，浅蓝边框。用于次要操作。
- **Ghost:** 透明背景，无边框，仅在悬停时显示背景。用于图标按钮。

### Cards / Containers
- **Corner Style:** 柔和圆润 (CornerRadius 14px)
- **Background:** 浅青白 (#EAFBFDFF)，与主背景形成微妙层次
- **Shadow Strategy:** 默认无阴影，悬停时显示轻微阴影
- **Border:** 浅蓝灰边框 (#E5ECF7)，1px 宽度
- **Internal Padding:** 标准内边距 (16px)

### Inputs / Fields
- **Style:** 白色背景，浅蓝灰边框，圆润边角 (CornerRadius 10px)
- **Focus:** 边框变为清澈湖蓝，显示轻微外发光
- **Placeholder:** 中石板灰 (#56658C)，确保 4.5:1 对比度

### Navigation (Rail)
- **Style:** 左侧垂直导航栏，透明背景
- **Items:** 图标 + 文本标签，垂直居中排列
- **Default:** 透明背景，中石板灰图标
- **Hover:** 柔蓝白背景 (#F5F9FF)
- **Selected:** 浅蓝背景 (#E8F1FF)，清澈湖蓝图标

### Dialogs
- **Shape:** 圆润边角 (CornerRadius 14px)
- **Background:** 白色或浅蓝白
- **Border:** 浅蓝灰边框
- **Padding:** 标准内边距 (16-22px)

## 6. Do's and Don'ts

### Do:
- **Do** 使用圆润边角（8-14px）来传达友好感，与 Windows 11 设计语言保持一致。
- **Do** 保持充足的留白（至少 8px 间距），让界面呼吸。
- **Do** 使用 Segoe Fluent Icons 图标字体，确保图标风格一致。
- **Do** 为所有交互元素提供清晰的悬停和聚焦状态反馈。
- **Do** 确保文本对比度达到 WCAG 2.1 AA 标准（至少 4.5:1）。
- **Do** 使用 Fluent Design 的动画曲线（ease-out），保持动效自然流畅。

### Don't:
- **Don't** 使用尖锐边角（0-4px）或过度圆润（>16px）。保持在 8-14px 范围内。
- **Don't** 使用纯黑 (#000000) 配纯白 (#FFFFFF) 的强烈对比。使用深海军蓝 (#071A4D) 配柔蓝白背景。
- **Don't** 过度使用主色。清澈湖蓝仅用于关键交互元素，不超过界面的 15%。
- **Don't** 使用 macOS 风格的 Dock 栏设计。坚持 Windows 11 原生的 Rail 导航模式。
- **Don't** 使用过度装饰的 Material Design 风格。保持 Fluent Design 的简洁优雅。
- **Don't** 使用游戏化或花哨的动画。目标用户是办公场景，需要专注而非娱乐。
- **Don't** 使用渐变文本（background-clip: text）或玻璃拟态效果。这些是过度装饰的表现。
- **Don't** 在卡片上使用超过 1px 的彩色边框条纹。这是常见的 AI 设计陷阱。
