古墟大陆资源管线

流程：资源需求与提示词 -> 指定网页生成 -> 缓存原图 -> 处理与检查 -> Unity 接入 -> 验收。
生成入口固定为 https://platform.openai.com/playground/images 。使用用户已登录的浏览器页面，按用户要求选择并核验 Image 2.6，记录实际模型名与尺寸；无法确认或不可用时报告，不自动替换模型。
生成图片不直接覆盖工程。先按用途、尺寸、透明背景要求检查，再执行处理。

角色协作（2026-11-10）
游戏设计师 game_designer：整体设计、需求拆解、实际试玩及体验验收。
开发程序员 game_developer：代码、配置、资源处理与导入、技术自测及打包；需要资源时派发 art_designer。
美术设计师 art_designer：使用指定网页的 Image 2.6 生成、下载、检查资源并交付缓存；可处理预览，不运行 -Import。
完整任务、缓存、验收与构建交接见同目录 AGENT-WORKFLOW.txt。项目角色定义在 .codex/agents，入口规则见根目录 AGENTS.md。
本次建立角色的验证复用现有缓存，不代表重新生图或打包。历史批次缺少的模型观测等字段不得事后猜填。

目录
ResourceCache/<批次>/request.json                开发提交给美术的需求，管线不解析
ResourceCache/<批次>/prompt.txt                  本批完整提示词
ResourceCache/<批次>/raw/                        原始下载，禁止覆盖或编辑
ResourceCache/<批次>/manifest.json               文件对应清单与处理规则
ResourceCache/<批次>/processed/                  可重复生成的处理结果
ResourceCache/<批次>/backups/                    覆盖前的资源与 .meta
ResourceCache/<批次>/pipeline-report.json        处理结果、路径、SHA256、失败原因
ResourceCache/<批次>/receipt.json                生成事实、质量检查、接入/测试/构建阶段证据

网页生成与下载
1. 从需求填写 prompt.txt，明确用途、构图、尺寸及透明度；将相同提示词输入指定网页。
2. 核对生成数量、尺寸及 PNG 输出后生成，检查成品。不要用网页截图代替原图。
3. 使用成品的 Download 按钮下载。如浏览器使用系统下载目录，将本次确切下载文件复制到批次 raw 目录。
4. 保存下载回执：来源网址、网页参数、原文件名、缓存路径、SHA256及网页截图。
登录失效时需恢复用户会话；不要读取浏览器凭证或改用其他生成服务。

处理入口（在 G:/demos/gxdl 下的 PowerShell 7 执行；使用 Windows 自带绘图库，无需 Python）
  ./Tools/ResourcePipeline/pipeline.ps1 -Manifest ./ResourceCache/AppIcon20261110/manifest.json
仅校验并生成 processed，不写 Assets。
  ./Tools/ResourcePipeline/pipeline.ps1 -Manifest ./ResourceCache/AppIcon20261110/manifest.json -Import
全批处理通过后复制到工程；相同 SHA256 自动跳过，替换前备份，不改 .meta/GUID。
若中途复制失败，报告保留已完成条目和备份路径；修复问题后重跑，或从备份恢复对应资源。

清单字段（schemaVersion=1）
generator.url：固定生成网址；其余字段记录页面实测参数。
entries：每个条目包含唯一 key、source（相对 raw）、sourceSize=[宽,高]、crop=[x,y,宽,高]、size=[输出宽,高]、alpha、output（相对 processed）、target（相对 Assets）。
crop=[] 表示整图；坐标以原图左上角为原点。多个条目可引用同一图，完成批量切图。
alpha 可用 preserve / opaque / transparent；不透明图标不可误用透明源，透明素材不可用假透明底代替。
可选 inset：输出画布四边的透明像素边距，用于自适应图标；缩放保持宽高比。
可选 alphaCutoff：0（默认不改）至8；仅清除缩放后 alpha 不高于阈值的极弱边缘杂点，写为透明黑。必须在 manifest 记录，原图保持不变。
纯色资源用 solidColor="#RRGGBB" 替代 source，同时保留 sourceSize=[]、crop=[]。
输出统一 PNG；尺寸、透明度、切片越界、重复目标、路径越界会报错。
同尺寸无裁切的 PNG 直接保留原始文件字节；原图始终保存在 raw。
新图集应使用显式切片清单，沿用工程已有 GuxuFinalArtImporter 及图集描述规则。

Unity 接入
通用资源继续使用现有资源键、加载路径及对应的 TextureImporter。普通素材验收须检查其实际 UI 或战斗使用位置。
应用图标为打包资源，位于 Assets/Art/AppIcon，不放入游戏运行时 Resources。
图标处理完成后，在 Unity 执行 GXDL/Resources/Apply App Icon。
此操作配置默认图标和 Android Legacy/Round/Adaptive 共18个槽位，并保存 ProjectSettings 原始备份。
Unity 按各槽位尺寸生成平台图标，不需要把母版作为桌面显示尺寸。
GXDL/Resources/Verify App Icon 可重复校验。现有 GXDL/Build Android APK 已加入此校验。

本次图标规格
raw 原图：1024x1024，仅缓存留档。
工程默认 / Android Legacy / Round 共用源图：512x512。
Android Adaptive 前景与背景：432x432；前景四边78px透明边距，主体保留在安全区域内。
Unity Legacy/Round 槽位：36、48、72、96、144、192px。
Unity Adaptive 槽位：81、108、162、216、324、432px，每档前景/背景两层。
本次验收为资源处理、Unity导入与打包配置；未重新构建 APK 或进行真机安装测试。

自测
  ./Tools/ResourcePipeline/self-test.ps1
覆盖路径保护、尺寸/透明度/切片错误、重复导入、覆盖备份、GUID保留。结果保存到 ResourceCache/pipeline-self-test.json。

规格参考
https://developer.android.com/codelabs/basic-android-kotlin-compose-training-change-app-icon
https://docs.unity3d.com/2022.3/Documentation/ScriptReference/PlayerSettings.SetPlatformIcons.html
