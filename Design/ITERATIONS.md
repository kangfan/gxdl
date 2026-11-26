# 古墟大陆迭代记录

维护者：游戏设计师 `game_designer`。每轮有明确目标和完成标准，状态与证据同步更新。

## SETUP-20261110：三角色配置与交接

- 用户目标：创建游戏设计师、开发程序员、美术设计师三个项目角色，串联整体设计、Image 2.6 缓存生产、工程实现、自测和打包职责。
- 范围：角色配置、项目协作说明、设计基线、迭代记录，以及利用现有缓存进行交接演练。
- 状态：blocked（配置及缓存交接检查已通过；当前会话未加载新角色，实际调用待重新加载项目后验证）。
- 负责人：当前主会话；独立只读检查代理 `/root/review_role_setup`（explorer，不冒充三个新角色）。
- 目标平台/构建：not-requested；本轮不修改游戏实现，不重新生成图片，不构建安装包。
- 验收：三个 TOML 可解析且必需字段完整；项目引用存在；角色能实际调用或如实记录会话加载限制；缓存交接可核验真实图片、规格、SHA256 和现有 manifest。
- 证据目录：`Logs/agent-setup/20261110/`。
- 设计、缓存与生产代码的分工：见 `AGENTS.md` 和 `Tools/ResourcePipeline/AGENT-WORKFLOW.txt`。
- 验收结果：三个 TOML 解析、必需字段、角色名与 11 个项目引用通过；无模型/权限/提供商覆盖。独立检查未发现职责、循环委派、manifest 兼容性或 setup 范围冲突。
- 缓存演练：复用图标及跳过按钮，共 2 批、4 项；prepare-only 处理结果与历史 processed/Assets 字节一致，尺寸及真实 alpha 通过，41 个原文件（含 .meta）哈希未变。样本位于 `ResourceCache/AgentSetupVerification20261110/`，没有新生图或导入。
- 调用限制：直接调用 `art_designer` 返回 `unknown agent_type 'art_designer'`；独立检查代理的可用角色列表也未出现三个新角色。新进程启动配置检查退出码为 0，但该命令不列举自定义角色，不能证明注册或调用成功。
- 下一步：重新加载该项目的新会话后，逐个实际调用三个角色完成只读交接验收；不用重新复制角色配置，不把默认代理模拟当作注册成功。当前会话没有可用的角色热重载接口，未自动创建用户聊天或重启应用。
- 证据：`Logs/agent-setup/20261110/definition-validation.json`、`handoff-validation.json`、`交付说明.txt`。

## UI23-20261110：05—27 页面视觉重设计

- 用户目标：执行 05—27 共 23 页重设计，与已完成的 01 世界总览、02 章节选择、03 战前准备、04 养成总览保持一致。
- 授权范围：23页画面、动态UI与已有交互回归；01—04只回归，不重新设计；用户后续追加 `Assets/Resources/Game/Guxu/UI` 现有资源像素与后续Image 2.6输出规格优化；不增加玩法/养成，不构建、不发布。
- 状态：experience-accepted（r02的23页美术来源、接入、技术自测及实际体验已重新完成，最终标准/长屏/登录技术快照PASS；无开放阻断问题）。
- 实际负责人：主代理 `/root`；设计 `/root/ui_design`及接续 `/root/game_designer_finish`（均default）；开发 `/root/ui_development`（worker）；美术 `/root/ui_art`（default）。各自读取项目对应角色TOML履职；自定义 `game_designer` 未被本会话识别，不能记录为注册调用成功。
- 设计与验收依据：`Reference/UIRedesign20261110/DESIGN-AND-ACCEPTANCE.md`；逐页状态 `Reference/UIRedesign20261110/PAGE-TRACKER.json`；资源映射 `page-resource-map.json`。
- 视觉决定：青玉/古铜/暖金延续新版基准；各系统有独立视觉中心；文字与动态数据实时显示；以 `ui23-` 新资源键限定本轮页面，保护01—04。
- 文件归属：设计负责 `Design/`、本轮设计/追踪/体验记录；开发负责生产代码、资源导入、技术自测；美术负责唯一批次缓存与来源证据。不得覆盖他人修改。
- 资源请求：`ResourceCache/UI23-20261110-Shared-r01/request.json`（8种共享组件）；`ResourceCache/UI23-20261110-Pages-r01/request.json`（10种主题背景）；实际生成批次以美术receipt记录为准。
- 环境：Unity 2022.3.62f3c1，主场景 GxdlMain；750×1334及750×1624。设计师完成隔离体验并End恢复后交回主代理；最终技术QA也已恢复并Stop，Console错误0，见Logs/UIRedesign20261110/final-editor-verification.json。
- 体验验收：23页标准/长屏、重点状态和实际交互；01—04回归；保护存档、PlayerPrefs及编辑器状态。看历史截图、首张示范图静态审查均不算本轮试玩。
- 证据目录：`Reference/UIRedesign20261110/experience`为设计师实际操作，`experience/evidence-index.json`按时间与执行者筛出152份状态/对应截图及SHA；最终技术快照为accepted-r02、accepted-r02-tall、login-r02。历史before/after/qa/backups保留，不混作最终通过。
- 构建/安装包运行/真机：not-requested；不将编辑器视觉验收当作真机认证通过。
- 中期静态审查：05—26的22页首轮实际截图已逐张检查；05三项标准图修订成立，仍有共享控件/内容问题待修，见 `qa/LAYOUT-REVIEW-01.md`。这些不是设计师实际试玩，experience状态仍pending。
- 尺寸优化验收：分别核对原始生成像素、工程/Importer尺寸、显示Rect与点击Rect；降纹理分辨率不缩点击区；本轮新标准/长屏图验证无拉伸、糊边、裁切和内容丢失，保留05按钮高度要求。
- r02新增强制来源要求：每次Image 2.6生成必须将对应界面截图作为Prompt中的真实图片附件上传。仅写路径、文字描述或生成后补添reference字段不满足。receipt/manifest须记录referenceImages实际路径、SHA256、对应页面和能看到Prompt附件的页面证据；详见 `qa/ART-PROVENANCE-GATE.md`。
- r01来源核查：据美术向主代理的核查报告，此前已审计11次生成、11批、13纹理均未实际上传对应参考截图。全部r01保留为中间草案，已接入不等于最终美术合规；旧静态评审只证明当时布局观察，不计最终艺术通过。
- 重跑处理：不删除旧raw、布局缺陷与修改证据；r02使用新批次并关联replacesBatch。05r02首屏先静态审图，全部23页资源/接入与技术QA后，再交设计师实际试玩；所有旧结论均需按最终r02复测，01—04保持设计只回归。
- r02尺寸依据：美术核验当前网页最小画布为655360像素；小UI用满足限制的最小画布图集再裁切成较小运行时纹理，竖屏背景按768×1360接近目标生成。实际网页参数、工程输出及显示/点击Rect仍须分别记录，不假填完成值。
- 当前通过数：r02原截图来源覆盖23/23、14批16生产资产来源闭环；05—27实际体验23/23通过，01—04回归通过。27限定UI/本地校验，真实认证排除；各页抽样路线、操作数据及缺陷闭环见EXPERIENCE-REPORT.md和PAGE-TRACKER.json，不以自动断言或来源计数替代试玩。
- 用户追加实时数据要求：所有页面绑定真实配置/规则与玩家状态，禁止照抄概念示例值；操作成功后文字/数量/属性/按钮/解锁即时联动，跨页一致，失败不错误扣资源。逐页标准见 `qa/LIVE-DATA-ACCEPTANCE.md`。
- 05r02概念阶段：已独立核对两个实际参考附件与输出SHA256，并查看Prompt/结果页面证据，完整概念来源通过；仅为含字排版参考，生产背景/组件及实际游戏仍待验。证据 `qa/05-R02-CONCEPT-REVIEW.md`。
- r02共享资产22:33检查：六类运行贴图真实附件/模型/输出/工程哈希及静态观感通过，60次SHA核对和Dark/Modal历史Prompt恢复补证4次均匹配。补证明确事后采集，不冒称生成前；详见 `qa/SHARED-R02-REVIEW.md`。这只是单资产通过，非23页最终艺术或体验通过。
- r02布局22:27批次复查：已逐张看05/10/12/15/16/17/18/19/20/21/22/25；05等级对比、15资源牌/前往、16零差值、10锁定奖励、22表格在本次标准图静态修复。仍有部分flat小按钮主体偏扁，及难度10“主线进度201”需要核对内部通关哨兵文案。证据 `qa/LAYOUT-REVIEW-R02.md`；27及实际交互/长屏待资源和Unity交接。
- r02最终准备：final-r02及final-r02-tall各26页技术截图PASS；10已显示“主线通关”语义并保留真实解锁判断，10/15等flat已60逻辑单位高。长屏750×1624按固定比例居中，上下各约145像素留边，不称铺满适配。来源闭环见 `qa/PROVENANCE-CLOSURE.md`，准备路线 `qa/EXPERIENCE-ROUTE.md`，交付记录 `DELIVERY.md` 待实际体验后定稿。

### r02最终体验与交付收口（2026-11-10 UTC / 北京时间11日凌晨）

- 原设计师独立操作：login-01 8步、login-02 3步、gameplay-01 22步、gameplay-02 001—013；接续设计师独立操作：gameplay-02 014—108及gameplay-finalboss 001—011。actualType均default。152个记录含输入/点击/观察/比例与夹具切换，不等于152次点击。
- 体验覆盖：锻造扣费/切槽；天赋三系前置/修习/全返重置；灵玉槽孔/镶卸/合成/库存；境界实际突破和不足禁用；秘境首通/解锁/常规收益/耗尽扫荡；古塔首层与返回；洞府剧情回看保护存档；伙伴真实未开放状态；技能装备/自动联动；盾10874与冷却2回合、速度、退出；真实败北与同关重试；200终章首通与返回防重。详细范围见体验报告，不宣称人工全通200关或100层。
- 终章通过合法成熟构筑正常UI挑战，第6回合胜利；奖励与预览一致，内部stage201只在界面显示“主线已通关”，继续按钮禁用。最后End于15:59:44.5001643Z确认原Rules/save/PlayerPrefs/GameView及原存档字节恢复；随后Stop。
- 体验缺陷闭环：UI23-27-01勾协议清旧错误、UI23-15-02动画首帧两处符数、UI23-RESIZE-01现有home/弹窗跨比例均独立复测关闭；UI23-TEXT-01的06/09/11/25局部对比实测通过。历史静态记录的“待验”指当时阶段，最终以本段及PAGE-TRACKER为准。
- 技术：数据绑定512断言PASS（15:26:55.5014282Z），完整运行959断言PASS；最终标准16:01:02.9167986Z、长屏16:01:46.7535808Z分别26页面状态PASS，各含额外锻造前对照共27 PNG；登录16:02:34.1252403Z两比例12状态PASS且协议偏好恢复。技术图/断言不归为人工试玩。
- 当前版本：`Reference/UIRedesign20261110/qa/accepted-version.json`记录58份源码/配置/管线文件SHA、16纹理×3阶段48次SHA一致、16GUID保留及66张最终PNG哈希。旧final-version.json保留历史时间与阶段，不重写成最终验收快照。
- 如实记录：gameplay-02有4条过时/禁用导航被Review桥拒绝的操作异常，未改变Save，不作为产品缺陷，也不宣称该段Console为零；最后终章轮及最终技术结束Console错误0。
- 长屏采用固定750×1334内容居中、上下约145留边；无铺满适配/Android dp/真机触控声明。真实SMS/TapTap认证、未配置协议链接、打包/安装包/真机未验证，伙伴数值养成未开放。15装饰小字及27副标题弱对比不阻断本轮主要信息。
- 交付索引：`Reference/UIRedesign20261110/DELIVERY.md`、`EXPERIENCE-REPORT.md`、`PAGE-TRACKER.json`、`page-resource-map.json`、`experience/evidence-index.json`。

## UISTYLE-20261111：完整界面审美返工

- 用户目标：用户新图指出沉剑遗庭20关4×5深色矩形网格明显简陋、不符合游戏风格；授权所有相似页面由美术重新完整设计并出资源。
- 范围：本轮自查全部01—27页，包含此前保留的01—04；按视觉家庭具体判定结构重做、局部完善、保留主体，不一刀切。旧UI23验收为历史，当前用户的视觉否定优先。
- 状态：experience-accepted。当前本轮27/27页编辑器视觉及独立核心路线抽样通过：02/06/13/14既有独立批、18页EXPERIENCE-READY-PAGES、最后04/05/10/11/25见EXPERIENCE-FINAL-PAGES。历史055128恢复缺证保留，不以本次恢复补造。未打包/真机/正式认证；本轮不扩展玩法。
- 实际负责人：主代理/root协调，设计/root/game_designer_finish后由/root/game_designer_review接续（actualType=default，重读game_designer.toml履职）；开发、美术由主代理协调同级接续，实际agentType以其真实记录为准，不循环派发。
- 2026-11-11接续：新角色现已实际加载，当前设计`/root/game_designer_acceptance`（game_designer）、开发`/root/game_developer_finish`（game_developer）、美术`/root/art_designer_finish`（art_designer）。此前default/worker履职记录保留为历史，不能倒填成当时已注册。
- 第一优先02：可滚动遗迹路径保留20关、普通/精英/Boss独立轮廓、通关/当前/锁定双编码、固定选中详情与唯一主CTA，章节宝匣和20关进度整合。完整brief为Reference/UIStyleRework20261111/02-CHAPTER-BRIEF.md。
- 初审清单：结构重做15页02/05/06/07/09/10/11/12/13/14/19/23/24/25/26；局部完善6页08/18/20/21/22/27；保留主体6页01/03/04/15/16/17。27张既有最终运行截图及用户图已静态逐张检查，当前不是本轮新实现试玩。
- 美术方式：指定网页Image 2.6，本次实际模型由美术观测；先完整含字概念，设计师给出通过/返工与具体图证，再拆素材。02首批ResourceCache/UIStyleRework20261111-Chapter-r01；真实参考图必须实际附件上传并记录SHA/证据。不能只给无字背景让开发继续套旧卡片。
- 玩法与数据边界：每章20关、天赋24节点/三系/前置、12装备槽/双孔/4类6级灵玉等保留现配置；文字/奖励/状态由Config/Rules/Save绑定。布局可重排/滚动/收敛重复动作；不加新天赋互斥、新槽位或伙伴养成。
- 视觉验收：完整概念与最终标准/长屏运行图逐项核对视觉重心、层级、节点/状态符号、真实尺寸字体、密度与主动作；若实现回到同款矩形卡片堆叠，即使功能通过也返工。保持数据即时刷新与隔离存档恢复的独立技术/体验验收。
- 环境：Unity实例gxdl@d0106ad3a0b45359，项目G:/demos/gxdl，2022.3.62f3c1，GxdlMain；设计师已核验Stop/idle，未为初审进入Play或改变存档。需要新运行证据时采用Review隔离夹具并End恢复后明确交接。
- 文件归属：设计维护Design/及Reference/UIStyleRework20261111；开发代码/导入/技术验证，美术唯一缓存批次。没有回退旧资源、覆盖他人生产修改或改写旧历史日志。
- 证据与输出：Reference/UIStyleRework20261111/VISUAL-AUDIT.md、PAGE-TRACKER.json、各家庭brief、source-evidence.json；完整概念和拆分素材由美术/开发交付。用户应能看到明确前后图。
- 首屏推进：02完整概念已由设计师实际看图通过，详见CONCEPT-REVIEW-02.md；宝匣三态、锁定后段路径低亮、业务占位替换为生产必改项。750×720无字七地台环境静态通过，节点/组合/滚动接缝仍待验；当前新运行视觉仍0/27。
- 设计交接：06-TALENTS-BRIEF.md、07-GEMS-BRIEF.md、09-11-EXPLORATION-BRIEF.md、12-14-26-COMBAT-BRIEF.md、19-23-24-HANDBOOK-BRIEF.md、05-25-WORKBENCH-BRIEF.md、REFINEMENT-AND-RETAIN.md已覆盖全部27页具体布局与复用边界。开发确认06真实三系各8节点严格单链，禁止臆造分叉/互斥。设计已把Unity只读核验后的Stop状态交给/root/game_developer_rework，本轮尚未开启设计试玩夹具。
- 构建/安装包/真机：本轮未请求，不因视觉返工擅自打包发布；当前仍未验证。
- 接续设计评审：02/06完整概念已通过，13/14 HUD概念于2026-11-11实际看图通过，详见CONCEPT-REVIEW-13-14.md；原角色/敌人/场景继续复用，真实重击/狂暴只进固定警戒位，不新增战斗规则。HUD概念9项来源/证据/原图SHA复核一致；生产资产与运行独立待验。
- 02第二轮静态复查：已实际查看冻结pass02标准/长屏/拖动/10与20 Boss五图，首轮焦点、灰度、悬空线、接片和完成签修订成立；SharedStates替换像素锁及末端提示收敛仍待验。第一轮报告引用已固定到pass01，防止live证据覆盖。设计师未接管Unity，尚无新实际试玩或视觉通过声明。
- 02独立体验收口（02:44—02:51Z）：设计师在sessions/20261111-024459-532独立产生19份新状态快照，完成两比例、锁定与选择/当前分离、真拖430、15/20滚动末端、首通/重访、宝匣实领及重入防重、终章200语义；02运行视觉和实际交互通过，当前新通过数1/27。End于02:51:32.232565Z确认原Rules/Save/PlayerPrefs/GameView和存档字节恢复，随后Stop交还root。见EXPERIENCE-02.md；1次普通click误用于禁用按钮被桥拒绝，后续正确reject-click验证，不掩盖接口误用。
- 新概念：12胜利完整概念、23纸册完整样板已实际看图通过；26失败、19品质规则、24伙伴按明确状态映射复用骨架，最终运行各自待验，不能共享一个“通过”。详见CONCEPT-REVIEW-12-26.md与CONCEPT-REVIEW-19-23-24.md。CombatParts、TalentEnvironment、TalentSymbols单资产静态通过及来源补证见ASSET-REVIEW.md；当前未把素材就绪当整页通过。
- 03本轮追加实际问题STYLE-03-DUPLICATE：主调整养成/开始战斗下又有修行与养成/继续主线，重复动作及旧网格背景边缘与新章节不衔接；已交root/开发保留主体局部清理，03仍待复测。
- 06/HUD首轮运行静态审查：实际查看sessions/20261111-031704-896六张06状态图与20261111-031645-686两张HUD图。结构成立但视觉未通过：06标题压金饰、纵向连接弱、页签占位感；HUD白回合条、长屏横接缝、技能名压饰边、玩家盾信息贴边、预警重复。详见RUNTIME-REVIEW-06.md与RUNTIME-REVIEW-13-14.md。开发25/24技术断言分列；修订代码已由负责者处理，须新图复核再关闭，当前仍1/27。
- 06/HUD后续通过：返修静态复核关闭上述8项，另修真实Boss肖像错误。设计师独立session20261111-041520-731产生31份状态/截图，实际修习三系、锁定拒绝、重置取消/确认、重入、140万蛊蛛母手动护盾/冷却/消耗/狂暴、自动/×2/退出及两比例成立。04:22:10.6836513Z End验证原存档字节和Rules/PlayerPrefs/GameView恢复，Stop+Console0后交回开发。新增06/13/14通过，总计4/27。未打包/真机。
- 09完整概念已由实际game_designer查看与核验三附件/模型/源SHA，四座有身份门庭方向通过；运行修正“难度0/10”为已通关进度，聚灵两种资源分别用真实图标。复用现有庭院背景，仅请求四个无字透明门户；生产及运行未验，详CONCEPT-REVIEW-09.md。
- 05/10/11/25完整概念已实际看图与来源核验通过，全部结构返工页均已有明确完整概念或已审共享骨架。运行纠正分别包括：05明确槽位锻造加成；10真实灵玉奖励图和锁段降亮；11按真实普通巡卫/Boss层映射与完整奖励，不复制过时“章节Boss投影”；25选中/已装备分离和删除重复冷却说明。详各页CONCEPT-REVIEW文件。
- 追加资源静态/来源：03透明面板、结算双印、手册签/窗景和r02可九宫格主体、07工作台/孔座、09四门户均通过，按ASSET-REVIEW记录。Handbook r01主体中段流苏不适多高度已实际返修，旧批保留未倒填；素材通过不计整页。
- 运行中期复核：03两项返修静态关闭；18/21/22两比例静态通过，19/23/24布局成立（23签内字位置最后微调待看）；12/26背景HUD/印章偏轴已修，最后人物残帧待收；07四处对比/间距/字区/装备清晰度、08边距/说明对比仍返修。新独立体验通过仍4/27，详PAGE-TRACKER与RUNTIME-REVIEW各文件，自动断言和看图未计人工试玩。
- 接续实际角色：设计/root/game_designer_closure（game_designer）、开发/root/game_developer_closure（game_developer）、美术/root/art_designer_closure（art_designer）；设计仅维护Design与本轮设计/体验记录，生产由开发负责。旧4页证据保留，不为计数重复长测试。
- 05:05—05:27Z独立体验：本次90份玩法状态/截图和最终7份正常登录本地状态/截图，新增01/03/07/08/09/12/15/16/17/18/19/20/21/22/23/24/26/27共18页通过，总22/27。两比例实际看图；真实镶/卸/合成、突破/不足禁用、秘境首胜与次数、品质规则持久化/自动停、掉落分解、鉴宝消耗/概率刷新、图鉴部位数值、重温开场/跳过存档不变、终章200六奖/通关禁用/返回防重、真实败北/同关重试与本地登录校验均有逐步状态证据。覆盖限具体抽样，不宣称人工全200关/所有槽与所有分支；详细见EXPERIENCE-READY-PAGES.md及experience-ready-pages-evidence-index.json。
- 新问题STYLE-04-RESIZE：修行总览运行时1334→1624后内容移出视口，session050549的013/019/020可重现；重入恢复不算关闭。开发已准备FitGrowthHub回调修复和1334→1624→1334→1624专用QA，须新独立体验复测。其余本批03重复动作、07对比/尺寸/间距、08边距、09标题、12/26人物残帧、23签内字压饰在新运行关闭。
- 本批恢复与交接：玩法End05:24:09.2373018Z验证原Rules/save/Prefs/GameView及原存档字节恢复；正常登录最终End05:27:01.0701594Z恢复consent/Prefs/GameView，随后Stop，Console错误/警告0，明确交开发接入最后四页。登录首次归档UTC筛选错误已如实记录，重新7步冻结后引用最终证据，旧live先备份后恢复。版本哈希记录引用开发接入前closure-backups/20261111-052746，避免拿后续新代码冒充体验版本。
- 最后四批生产资产：DifficultyNode/TowerTier/ForgeEnvironment/LoadoutAltar已实际看成品、暗亮底/拼接与附件可见证据，8参考及相关产物41项SHA一致，来源和单资产静态通过。Tower改512×200保护完整轮廓，低层在前/pitch190；Forge实际台面729需运行适配，不能假称原位置达标；05/25附件补证为生成后Edit prompt恢复，诚实区分前后。见ASSET-REVIEW.md与asset-review-source-final-four.json。未计为05/10/11/25整页通过。

- 最后收口（07:16—07:30Z）：/root/game_designer_final_play（game_designer）在唯一新session071625-157完成72份状态/PNG，05真实升级/双币/重入，10锁阶/首通/扫荡与庭院同步，11锁层/拖动/91—100边界/真实1层胜利更新，25四技能浏览与装备分离、自动开关、重入、战前和HUD一致；两比例新图逐项对照概念。04采用055128-291在05:57Z的003—008连续resize与境界往返独立证据关闭STYLE-04-RESIZE。最后新增5页，合计27/27，详EXPERIENCE-FINAL-PAGES及证据索引。
- 最终技术/版本分开计数：7入口723断言（41/13/59/39/14/24/533）为开发05:28—05:37Z结果，本次未重复运行；当前closure-final-version-qa-recovery.json SHA8fc87a84b3818fe82d28d68e9355b32184042ce54590f55df8107a833e96a247仅比旧723版本改2个Editor恢复保障文件，生产与44资产不变。结束比对106源文件/44资产及meta共298项哈希全部一致。
- 新会话恢复：071625于07:30:05.9747075Z End为RESTORED后Stop/idle，recovery-before/after除at完全相同，原8级7关存档与备份SHA56d21423815a986b2f6a3d4ed2d77ae979607d576880d17b2ac7f5f68e429e8a一致，Prefs/Rules/GameView187恢复，无reload-trigger/restore-error。Console保留1条操作者误点禁用Tower Challenge的Review guard异常，无生产动作或扣费，不能写该会话Console0。Unity已交回root。
- 历史恢复限制：旧055128在04复测后05:58:20 domain reload丢static fixture，旧ACTIVE不改写；原磁盘存档与前bak同SHA仅证明玩家数据保全，旧Prefs/GameView前值缺证。新持久checkpoint/域重载前End保护与065403小核验不能补造旧恢复。详technical/recovery-055128/handoff.txt。
- 本轮交付索引：Reference/UIStyleRework20261111/DELIVERY.md、PAGE-TRACKER.json、四份分批EXPERIENCE报告；18生产批44当前资产、11概念批分别列入production-cache-closure-index。未构建、无新包路径、未安装运行/真机/真实SMS或TapTap认证，保留固定高度页长屏留边等已知限制。

## MOTIONVFX-20261111：角色动作与原生粒子特效

- 用户目标：破封必须连贯呈现施法→符文飞出→击破古石；角色与Boss脚底浮云解决悬空；增强尤其战斗特效。用户追加明确优先Unity原生ParticleSystem。
- 授权范围：现有动作/表现/VFX、必要粒子素材、接入与自测迭代；不打包发布、不重开UI27、不改Rules/经济/掉落或战斗预计算结果。
- 状态：completed within bounded Editor scope；已修pending导致古石提前隐藏，首次/普通飞符命中与碎裂同帧，现有Spine抬臂/转腕与Home站位优化，原生ParticleSystem脚云/拖尾/冲击/盾/治疗/Boss主题已接入。保留规则与预计算结算，不重做UI27。
- 优先级：P0破封相位与命中碎裂同步；P1脚底云锚点/遮挡；P1普通攻击/技能/受击/护盾/Boss重招层次；P0生命周期/一次性结算/原存档保护。
- 验收：Reference/MotionVfx20261111/DESIGN-AND-ACCEPTANCE.md 的MV-01—MV-11；两比例、逐帧/视频、实际pointer与Rules状态、速度/取消/结算清理、ParticleSystem真实性及恢复前后。
- 负责人：设计/root/game_designer_final_play；开发复用/root/game_developer_closure；开发按需复用实际art_designer。设计拥有Design及新Reference体验文档，生产与QA由开发负责，不回退他人修改。
- 资源：指定网页Image2.6，实际上传本轮真实运行截图，记录附件可见证据/路径SHA/覆盖/实际模型；ResourceCache/MotionVfx20261111唯一批次raw/processed，pipeline导入保护meta。
- 环境：gxdl@d0106ad3a0b45359 / G:/demos/gxdl / Unity2022.3.62f3c1 / GxdlMain / Windows Editor 750×1334与750×1624。先仅Editor独立目录与有界记录入口，基线后再生产实现。
- 范围外：安装包/真机/真实认证/长期平衡及留存。UIStyle055128恢复缺证保留，不以新恢复洗白。
- 素材/来源：ParticleTextures-r01指定网页实际gpt-image-2.6-sunburst，3新真实运行附件，832²透明原图→cloud256×128/wisp128²，pipeline接入，根22来源项与150源/接入项独立哈希复核通过；保留真实Spine47骨骼，不声称生成新Spine。
- 技术：Motion Technical85+触点HUD窄回归4通过，包含预计算触点/速度/合成慢帧、215锚点边界、两Boss、取消和带盾inactive停发。合成慢帧不是性能基准；全部Boss分配公式检查不是全部人工战斗。
- 独立体验：084039-119真实pointer，56成功PNG/JSON、17段2817实时采样帧，两比例首次/普通/自动保留/跳过、四技能、200太虚残神/140蛛母、x1→x2、带盾结算/退出及回Home通过；采样峰80粒子，结算/退出0，Home仅两云。精确相位取消由技术覆盖，品质4/5额外揭示未单独人工抽中。
- 冻结/恢复：production-version SHA84063ea39d11a903df12125f186a474ec1d06fd5f23933b8c1b75df4490fa4f4，75文件与对应meta无漂移。08:52:51Z End RESTORED后Stop，before/after除at同值，原存档/备份SHA56d21423815a986b2f6a3d4ed2d77ae979607d576880d17b2ac7f5f68e429e8a。
- 异常如实保留：独立Console1次操作者点击已结算消失的Battle Speed导致Review guard，无生产回调；开发081713 RESTORE FAILED保留，下一081850 before证明原状态回归；081850 reload守卫恢复但中断录制不计最终验收；旧UIStyle055128缺证不变。
- 交付：Reference/MotionVfx20261111/DELIVERY.md、EXPERIENCE-FINAL.md、experience-final-validation.json与evidence-index。未打包、无新包路径，未安装/真机/真实认证。

## 后续待办

| 条目 | 建议目标 | 状态 |
| --- | --- | --- |
| 编辑器UI与核心循环体验 | 已纳入UI23本轮并实际完成，问题修订与独立复测见本轮证据 | completed within UI23 scope |
| 真机/正式账号/安装包验收 | 当前未请求；如后续启动，单独验证真实认证、安装、DPI与触控 | not-requested；未执行 |

## 后续迭代记录格式

复制所需字段即可，不预建空任务：

- ID / 日期 / 用户目标 / 来源与授权范围。
- 当前现状与证据 / 设计方案 / 优先级 / 验收标准。
- 负责人和实际 agent ID / 文件归属 / 资源批次 / 测试环境 / 构建平台。
- 状态：planned、implementing、technical-verified、experience-accepted、delivered；阻塞使用 blocked 并记录原因、已完成内容、下一步。
- 代码配置变更 / 技术测试 / 体验复测 / 包路径、时间和 SHA256 / 未验证项。
- 本轮结论 / 遗留问题 / 下一轮建议。

体验问题记录：问题ID、严重程度、复现步骤、实际结果、期望结果、截图/视频、建议、负责人、复测结论。只读分析、自动测试、实际试玩、安装包运行、真机验证分别说明。

## MOTIONVFX-CINEMATIC-20261111：大幅施法、下肢仙云与宽幅打击

- 用户目标：上一轮施法仍过小，云雾需包围角色/Boss下肢，打击需要修仙战斗的大气魔幻范围；继续优先Unity原生ParticleSystem。
- 状态：experience-accepted / delivered within bounded Editor scope；上一轮薄云/小幅动作结论仅保留历史，本轮三项视觉反馈已重新实现和验收，详本节最终收口。
- 设计：Reference/MotionVfxCinematic20261111/DESIGN-AND-ACCEPTANCE.md；CIN-01—07涵盖大姿态/接触同帧/下肢雾/宽幅攻击/双比例速度/清场/恢复。
- 范围：复用47骨骼Spine与现有2粒子纹理优先，普通蓄势约0.72秒、飞行约0.28秒，首次1.5释放/2.5接触/10.2总长保持；真实规则、奖励、HUD事件同步不变。不重开UI27、不打包和真机。
- 实际负责人：接续设计/root/cinematic_designer；开发/root/game_developer_closure；root负责来源和独立审核。旧设计代理恢复未回报，root已interrupt，生产改动尚未发生。
- 基线：baseline-version.json对75文件核验仅Editor Cinematic入口不同，生产与上一轮冻结版相同；旧084039对照可用，新093824普通/首次最小实录补充。
- Unity：gxdl@d0106ad3a0b45359 / G:/demos/gxdl /2022.3.62f3c1；串行交接，所有试玩隔离存档，结束校验恢复。

- 用户最新追加（本轮进行中）：动作技术路线开放，允许帧动画替代Spine；自然肩肘腕、无异常扭曲/折断及完整衣袖优先。开发需检查大动作中间帧和恢复，若现rig无法自然完成则调美术按真实运行截图附件生成动作序列帧，并保证混合切换/手脚锚点连续；粒子仍优先Unity原生。

- 首版中间帧返工：094412普通003/0017—0019确认袖口/前臂背景间隙，CIN-01不通过；已派开发实际调用美术做完整角色大施法8姿态序列帧，普通/首次可共用，保留自然成熟动作。用户允许攻击角色与握持剑同帧生成，剑气/云/冲击继续独立原生粒子。不是假定所有Spine都不可用，而是本次具体大动作已出现变形需换路线。

- 美术实际派发：开发followup /root/art_designer_closure，缓存ResourceCache/MotionVfxCinematic20261111-UnsealFrames-r01，request.json已含真实Home/断肘负例/095248新战斗三参考SHA；目标16连续帧，8为主姿态节点。当前art-requested、未生成/接入，不提前记就绪。
- 云/战斗首版静态修订：095248-168/005帧0000/0028/0088/0175实际查看，三高度云连续承托下肢，宽剑气/紫色冲击范围增大且HUD可读，方向可用；仍待最终动作资源、独立运行/速度/清场验收。

- 新施法美术art-ready：UnsealFrames-r01两次实际Image2.6 sunburst/2048×1536/透明PNG，三真实运行附件+后次额外原A表作连续性参考；固定裁16张512×768（15独特+末00闭环），release8、实际掌脚锚点齐。设计已实看暗亮整序、锚点图和两次Prompt证据，静态通过；工程与独立体验pending。

- 新帧首轮实装：100802-930显示整身序列替代大幅破封，抬臂/推掌肩肘完整，三层脚云与宽战斗粒子继续使用原生PS；原有自然Spine待机/成熟战斗保留。首拍冷加载使首采样直接frame2，开发补Home预热和播放起点帧钟后重验；100802不作最终时序通过。末帧回待机约10—15像素头脚差已提交正常速度复查，最终体验尚未完成。

- 最终收口：新Home整身16帧/release8替代断肘首版，统一真实鞋底注册与比例，预热后从00开始；保留自然Spine待机/战斗。三高度下肢云、宽气击/御剑/重招与自然尾波均为本轮实际完成。用户偏好已入GAME-DESIGN：后续新挥剑帧人物和手持剑同帧生产，外部剑气/命中独立；本轮没有新增挥剑动作帧。
- 技术：101756-824于10:18:25Z最终125 PASS/RESTORED，101213-871四项HUD PASS；后者之后只改Home注册。技术/美术/独立体验分别记录，不合并冒称人工测试数。
- 独立体验：设计/root/cinematic_designer在102210-741真实pointer完成37份状态PNG/JSON、15段1764张实时采样，普通/首次/长屏自动保留/真实Skip、1334御剑×1含普通气击与重招、1624玄甲盾×2、带盾结算/再入退出/返Home。CIN-01—07按所录范围通过，普通全部16姿态与Full关键相邻过渡实际看图，时间轴和限制详EXPERIENCE-FINAL；不冒称人眼播放连续视频。
- 清场/范围：两个结算与退出整段0原生PS/粒子，返Home仅2云；采样峰232，不是移动性能。衣摆/发丝回旧待机仍有细节轮廓切换，头脚比例稳定；Home长屏留边。未重跑全UI、全技能/Boss，未打包/安装/真机/真实认证。
- 冻结/恢复：production-version SHA4b1778922f01627432295fd240d3c509f6fee45cb6a9c9441d7f4fbbd5171124，93文件及对应meta无漂移，root核7业务文件未改。10:31:47.1752410Z End RESTORED后Stop/idle，before/after除at全等，原Save SHA56d21423815a986b2f6a3d4ed2d77ae979607d576880d17b2ac7f5f68e429e8a，Prefs/GameView187恢复，独立Console错误/警告0，无reload-trigger/restore-error。Unity明确交回root。
- 交付：Reference/MotionVfxCinematic20261111/DELIVERY.md、EXPERIENCE-FINAL.md、experience-final-validation.json、FINAL-PREVIEW.png；首版失败证据不覆盖，技术报告和root最终审计分别引用。

## HUD-REWORK-20261111：Home/战斗五区轻量重设计

- 用户目标/授权：本轮用户否定任务板与附图5处厚重HUD，要求参考其他游戏，任务可无底板、彩色文字和下划线；明确授权设计、出图、实现和自测，不需再确认。
- 状态：experience-accepted / delivered within bounded Editor scope。范围仅Home身份货币、破封组、主线追踪，以及战斗上部导航/Boss、下部玩家/技能；不重开全部UI，不打包（build=not-requested）。
- 负责人：原设计/root/cinematic_designer已中断，2026-11-11由/root/hud_designer_acceptance接替；开发复用/root/game_developer_closure，概念美术/root/art_designer_closure，最终六件无字组件由root协调/root/hud_art_parts实际生产。设计与本轮Reference归设计，代码/QA/Assets归开发，ResourceCache归美术；不回退他人修改，Unity串行交接。
- 依据：Reference/HudRework20261111/DESIGN-AND-ACCEPTANCE.md与user-redlines.png；root实际浏览FFXIV任务/HUD及剑网3历史官方任务图，SOURCE-NOTES区分真实观察与本项目下划线要求。
- 设计目标：任务去厚纸板且整区透明可点；轻身份/货币组、唯一强破封动作、薄Boss血盾、紧凑技能自动组，375宽可读。先两张完整概念经设计/root审，再拆无字共享小组件并pipeline接入。
- 已核现状：93生产文件仍匹配CIN冻结，可用102210-741干净整页作同版参考（不冒称新录）；Header/破封/任务集中GuxuHome，战斗集中GuxuCombatView。Home保留设置关闭auto但背后文字未即时刷、任务标题硬编码纳入本轮修正；不改Rules公式。
- 验收HUD-01—08：来源、视觉、真实绑定、普通/禁用/费用不足/自动/任务阶段、血盾技能/速度/退出重入、两比例与恢复分别通过；以本轮新技术与真实体验为据，旧CIN通过不抵消本轮审美要求。
- 概念审查：接替设计师实际查看Home/Battle的375宽概念和用户红框，两页方向通过，允许最小6个无字组件生产；root另有独立审查和23项来源哈希复核。详DESIGNER-CONCEPT-REVIEW.md。完成态采用与Adventure去向一致的“重访遗域 ›”；未完成态用“前往真实区域”与当前目标/关卡，不从概念新增规则。实际代码已有半血狂暴机制，真实mechanic提示保留。概念通过不代表资源或运行通过。
- 现有窄技术修复：11:07:59Z显示逻辑15项PASS并恢复，仅覆盖自动保留刷新和已展示事件累计，不代表新布局完成。独立体验仅围绕HUD-01—08，不重复无关旧125项检查；明确开发交回Unity后开始。
- 最终接入/技术：六无字组件经pipeline导入并核尺寸/九宫格/透明/引用；最终113857-494布局77PASS、113945-772显示累计15PASS，均恢复。修正技能生效时冷却更新滞后、HP首字贴端帽，保留初次失败记录。源码冻结f5fc6884551e58f0ed524e634ede39cddad1571f0b8137c6056768738f9ff754。
- 独立体验：114157-466真实pointer完成47份状态PNG/JSON、10段525采样帧；两比例、任务真实去向、破封/分解装备/自动保留/零符、御剑手动与自动/×1×2/玄甲盾/结算与提前退出重入通过。405个战斗采样HUD与显示统计一致；042/0007—0008盾0→20052与冷却同帧刷新。FINAL-PREVIEW及四375预览由设计/root实际看图通过。
- 恢复/日志：11:48:47.9743943Z End RESTORED后Stop/idle，原Lv8/stage7、Save SHA56d214…429e8a、Prefs和GameView187恢复；102文件+100meta无漂移。保留1条结算后尝试点已消失退出按钮的Review ElementAt异常，随后039/043提前退出复测通过；无其他error/warning，不声称Console0。
- 交付：Reference/HudRework20261111/DELIVERY.md、EXPERIENCE-FINAL.md、experience-final-validation.json、FINAL-PREVIEW.png；根侧来源/生产/最终审计分别记录。无包、安装/真机、正式认证、全27页或长期平衡验证；Home既有长屏居中留边保留，无本轮未关闭生产问题，不自动启动下一轮。

## PHONE-TEST-APK-20261111：指定手机号真机测试包

- 用户授权：手机13527645605、验证码123456成功登录并打包APK；本轮以独立测试包实现，不将固定凭据带入正式发布认证。
- 负责人：主会话/root协调与独立核验，开发程序员/root/phone_test_apk实现、技术自测、构建和模拟器运行。范围限账号测试分支/构建/交付，不改玩法、美术或经济规则。
- 已接入：仅GXDL_PHONE_TEST一次性构建定义、Development模式、专用包名com.localrebuild.guxudalu.phonetest共同满足才启用固定凭据；正常发布保留配置预检与真实认证。测试账号稳定隔离，不要求先发送短信，不调用短信或实名SDK；界面明确测试状态。
- 技术自测：20:06:56+08:00 PASS ALL，错号错码、真实输入框/按钮、Loading→开场跳过→Home、退出重登、后台恢复与存档隔离通过，测试存档文件集合/字节及观看标记恢复。见Reference/PhoneTestApk20261111-2010/phone-test-qa.txt；首轮点击时序失败记录保留。
- 构建：独立快照Assets 1050文件逐一SHA256核对，Unity2022.3.62f3c1 Android IL2CPP/ARM64+ARMv7，20:23:59+08:00 BuildReport Succeeded，0错误/1条无图形模式探针警告；签名v2核验通过。
- 产物：G:/demos/gxdl/Builds/Android/GuxuDalu-0.1.3-PhoneTest.apk；409887612字节；SHA256 FC67F55C167472E402E842C09AA6AC08BC507118912783530D816D0D4058EAD4。BuildReport总bytes包含其他构建产物，不作为APK大小。
- 安装运行：现有Android模拟器安装启动、实际手机号/错码拒绝、正确码→Loading→开场→跳过后Home已产生截图；根侧独立查看wrong-code-confirmed、loading、opening、home-after-skip并核APK哈希/ZIP架构。首张wrong-code.png为空输入，不计错码用例。实体手机、真实短信/实名认证及移动性能未验证。
- 证据：Reference/PhoneTestApk20261111-2010；完整原生构建日志、BuildReport、签名、快照哈希、编辑器恢复及模拟器截图分别保留。安装包已交付路径，使用范围为测试包，非正式发布包。

## GROWTH-REWARD-20261111：成长曲线与双倍奖励

- 用户目标：协调所有已开放养成与主线/副本难度、收益和提升节奏，实际试玩并修复有证据的UI问题；当前最新决定为保留广告占位按钮，点击直接领取双倍，不接SDK/不模拟视频。
- 状态：experience-accepted / delivered within bounded Editor scope。设计 `/root/balance_reward_designer`；实际开发 `/root/balance_reward_designer/growth_reward_dev`；root独立审查。目标Editor竖屏与Android兼容运行逻辑；build=not-requested。
- 范围：100境界/20封灵树、15档12槽装备、锻造/24天赋/灵玉/4技能，当前200主线/40资源副本/100塔；先真实规则基线和可获得资源模拟，再针对卡点调参，不盲调一个Power乘数。
- 双倍覆盖：主线通关基础奖励、章节任务宝匣、资源副本通关/扫荡、古塔首次奖励；基础照常到账，按钮一次补原快照一份，保存来源/ID/领取状态，失败重复重入不多发，不重复首通/进度/日次数。不新建未存在的任务系统。
- 优先级：P0自然经济/存档和奖励幂等；P1成长可感知及Boss/普通关节奏；P1真实UI刷新和双比例；P2无证据外观建议进待办。
- 设计及可观察验收：Reference/GrowthBalance20261111/DESIGN-AND-ACCEPTANCE.md GB-01—07；技术/模拟由开发，真实新手及可获得中后段抽样由设计师实际交互。结果分模拟/体验/构建设备，不冒称全真人长周期已测。
- 当前基线：已核最新PhoneTest分支仍在；Unity2022.3.62f3c1、G:/demos/gxdl、GxdlMain。旧Review new含测试资源，本轮须真默认新手；Power不计词条/技能，当前缺陷不可固化为目标。
- 文件归属：Design和体验报告归设计；Assets/配置/QA及必要资源接入归开发。证据根 Reference/GrowthBalance20261111，修改前备份当前文件，不覆盖其他工作。
- 最终实现：境界关口平滑、鉴宝20级递增品质/成本重排、主线与副本真实规则配装校准、塔新层跟随主线且已过层保留。旧鉴宝13级起100%橙改为13级29%/20级74%，影响未来掉落，既有装备不变。保留基础Power并统一名称；词条前后值与24场真实战前抽样补足机制评价，不承诺必胜。
- 最终模拟：三掉落种子与独立战斗评估组；同日常规/有限操作202次主线、414破封、318总战斗通200，全双倍200/400/312。完全不养成停5、仅装备境界同日停25；分日明记实际日上限和日期，资源按来源/用途闭账。详simulation四组r03；不是人工200关通关或耗时/留存结论。
- 技术与体验分开：规则85、两比例UI12、停止修复25、刷新/引导55项分别通过；Unity/.NET256组一致。独立实际默认新手失败/换装/突破、1玄铁副本+2锻造通6，中段天赋/技能、后段灵玉/全双倍160/章节箱/塔零收益通过，31项实际记录复核通过；3个真实问题已修并复测，见EXPERIENCE-FINAL。
- 冻结/恢复：production-freeze-r03 SHA DEBE021B92C612E6DA4ACC442AAC2A8A401497D6B35AC884F81549036878A706；233文件和既有.meta/PhoneTest保护边界匹配。8个会话各自RESTORED，最后13:50:22Z End+Stop；root13:52:43Z保护恢复本轮最初Save56d214…429e8a，Prefs/账号范围/GameView9保持。root-final-audit PASS；此后不再Play。
- 交付：Reference/GrowthBalance20261111/DELIVERY.md、EXPERIENCE-FINAL.md、experience-validation.json、FINAL-PREVIEW.png和preview-manifest.json。无新美术/包/安装/真机或真人长期留存结论；当前按钮即领符合用户决定，SDK未来未请求。此前PhoneTest APK不包含本轮改动；本轮无未关闭问题，不自动开下一轮。

## EXPERIENCE-ACCOUNTS-20261112：新手／中级／高级体验账号

- 用户目标：创建可实际登录体验的三个成长阶段账号，装备等级、战力、通关、锻造等均符合正常游戏可达数据，保留后续游玩进度。
- 状态：implementing。主会话/root协调；设计/root/experience_accounts负责阶段与可达性/体验文档；开发/root/phone_test_apk负责生成/接入/技术测试及新测试APK，不回退其他会话修改。
- 当前目标：新手真实默认新档待挑战1关/已通0关，首次播放开场；中级/高级待挑战80/160关、已通79/159。中高档等级、12槽装备、锻造、天赋灵玉、资源和基础战力由最新生产规则真实回放得出，不硬填战力；主动领取当前可用奖励加倍，避免初次登录积压大量未领历史，明确该合法策略不代表真人平均曲线。
- 依据/验收：Reference/ExperienceAccounts20261112-r01/DESIGN-AND-ACCEPTANCE.md EA-01—07；默认40符300尘、种子42、每章一个模拟日、实际战斗/经济闭账。禁止调用旧调参模式，正式经济与认证不变。
- 范围：测试存档/登录/初始化隔离/新测试APK；不新增美术、不重调平衡。登录形式等待用户可选回复，规则生成先进行。当前MCP工具不可用，编译/生成可用.NET与隔离Unity batch，真实运行通过ADB模拟器另列；实体手机不提前宣称。
- 用户登录方式补充已落实为设计约束：手机号验证内容不变，同时支持账号密码登录；三个体验号使用gxdl_new/GxdlNew123!、gxdl_mid/GxdlMid123!、gxdl_high/GxdlHigh123!，仅该测试包。旧13527645605/123456手机测试身份与存档保持，真实手机验证不改，新增账号各自独立scope。
- 用户最新纠正覆盖早先第6关新手目标：gxdl_new必须真实默认新档从0通关开始，Lv1/初始装备和40符300尘/零锻造天赋玉/无奖励历史，不执行模拟动作，不预写OpeningSeen或FirstUnseal；新scope确保旧手机号标记不影响，首次登录Loading后播放开场，可主动跳过，后续体验进度照常保存。中级待80、高级待160保持。
- 数据静态验收：新手Lv1/基础1462/通0/锻造0；中级Lv52/基础55008/通79/锻造总142；高级Lv84/基础107272/通159/锻造总256。中高使用真实即时加倍的固定种子42第4/8模拟日轨迹；设计师独立累加30资源delta与3档实际JSON完全匹配，8源SHA/3档SHA匹配。首版报告可变数组引用污染已修复，实际JSON未污染；完整字段表与限制见DATA-REVIEW.md、designer-data-audit.json。当前只是数据通过，真实登录/安装/体验验收仍进行中。
- 用户追加强调副本通关/解锁/任务匹配：EA-02扩展为生产CanEnter/RequiredProgress/CanClaimChapter的逐档全矩阵，包括主线firstClears、10章节箱/任务文案、4类40难度已通/可入/可扫荡/当日次数、100塔层已通/下一层门槛；中高真实打开章节任务/副本/塔抽样。当前没有独立每日任务系统，不新增虚构任务标记。该追加在本轮验收范围内。
- 副本体验适配决定：中高达标当日四类次数均已消耗，初始体验改为“达标后次日首次登录”；各自快照深拷贝调用生产RefreshDaily，成长日4/8→登录日5/9，仅rewardDay/dungeonClaims变化，资源/装备/通关完全保持。首次导入映射实际日，重登不刷新，正常跨日照原规则。必须同步新JSON、全解锁矩阵及最终APK；该合法日切改善测试可操作性，不给测试号注资。

### EXPERIENCE-ACCOUNTS-20261112 交付范围更新

- 用户最新指令：生成APK即可停止测试。2026-11-12已停止后续测试和设计体验，不继续开展三个账号的安装包交互验收。
- 最终APK已生成：G:/demos/gxdl/Builds/Android/GuxuDalu-0.1.4-ExperienceAccounts.apk；409983168字节；SHA256 D82C2B4E484E52C87406AF54F3B9D7BE43B42BDC794105810C1DD38093FD4183；BuildReport Succeeded/0错误/0警告，v2签名有效，versionCode5。
- 已有验证仅按实际范围保留：真实生产规则生成、.NET首登持久化和账号隔离、完整解锁矩阵、源码保护及APK签名/产物核验。APK已在模拟器覆盖安装并启动到登录页；未输入体验账号、未进行本轮三档APK交互验收，未做实体手机验证。
- 新手是默认零进度并沿现有首次开场规则；中高级为正常成长后的次日初始状态。手机号发送/登录代码逐字保持原样；三体验账号密码仅专用测试包可用。原手机号存档和Prefs按备份收尾恢复。
- 设计师未操作设备，无设计试玩通过结论。旧18:26静态数据审计为日切前记录，日切后采用开发18:35规则矩阵及全字段不变断言；未将旧设计审计冒充最终日切验收。

## UNSEAL-VIDEO-20261112：镜像视频动作与取消首破封引导

- 用户已确认执行：镜像参考视频为朝右对古石，连续大幅送符、透明资源清理与待机衔接；首次破封去专属引导/长演出，使用同一普通流程。登录开场保留。
- 状态：experience-accepted / delivered within bounded Editor scope。设计/root/unseal_video_designer；开发/root/unseal_video_designer/unseal_video_dev；美术由开发按角色链派发。Design/体验归设计，Assets/导入/QA归开发，缓存归美术；不得回退其他修改。
- 证据Reference/UnsealVideo20261112-r01，缓存ResourceCache/UnsealVideo20261112-r01；DESIGN-AND-ACCEPTANCE.md定义UV-01—07，root-baseline保留当前712文件与存档hash。最新ExperienceAccounts0.1.4实现受保护。
- 优先级：P0真实扣符/一次掉落/首破封行为一致；P1动作自然与镜像方向、手符接触、同源待机；P1绿幕/异常色清理、两比例、清场和存档恢复。
- 目标Editor实际运行及Android兼容代码，build=not-requested。当前MCP工具缺失，开发核可用运行入口；未实际运行前不标体验通过。仅本轮相关回归，不重跑200关或其他页面。
- 最终实现：用户视频水平镜像/绿幕清理，57张512×768连续透明帧，24fps、frame16/0.667秒离手，2.333秒末帧、普通演出约2.45秒；Home同源待机，独立128²符纸/96²显示，原生粒子云雾/拖尾/冲击。首次10.2秒专属演出及入口取消，普通首次完成仍写兼容key以保留仅一次首武器，第二次恢复正常随机槽；登录开场与战斗角色保持。
- 美术与布局返工：早期紫色鞋块/下摆碎片及错误矩形擦除导致腿缺口均经暗浅底审查修正；实际Play发现蓄势手压在左入口下、前伸穿石，改Home527显示高/固定脚位/人物左20/石右35，HUD不挪。最终375宽和12姿态两组由设计/root实际看图通过；源事实明确user-provided-video，不伪填Image2.6。
- 技术：带图形batch Unity2022.3.62f3c1、GxdlMain真实Play，123项运行断言、5项来源管线校验通过；57帧ASTC6×6、无ReadWrite/无mipmap，Editor纹理44,865,384字节，Android块存储估算10,039,296字节，旧16帧加载0。不是设备峰值/帧率结论。
- 独立体验：session20261112-130039-493，实际EventSystem pointer点击首次1334/后续1624、装备/分解、自动中途停止、修行返回与待机；首次40→39/slot0、第二次39→38/slot6、结束约2.46/2.49秒，停止后35符/5次/同一pending稳定。19项结果复核PASS；两次错误按钮名属于设计师桥操作，纠正并复测，未宣称零异常。长屏保持既有居中留边，非原生窗口/真机体验。
- 恢复/保护：13:05:42Z End+Stop恢复、13:05:48Z桥关闭；root13:06:27Z严格比对仅空rewardReceipts差异后恢复最初56D214…429E8A。root-final-audit确认834冻结文件、既有meta及两份原存档全匹配；13:07:34Z结束本轮专用PID98756恢复最初无gxdl进程，两档再次核对。之后不再Play。
- 交付：Reference/UnsealVideo20261112-r01/DELIVERY.md、EXPERIENCE-FINAL.md、TECHNICAL-DELIVERY.md、experience-validation.json、FINAL-PREVIEW.png。美术/接入/技术/Editor独立验收已完成；未生成APK、未安装或真机测试，先前0.1.4包不含本轮改动；不扩全游戏/不自动下一轮。

## UNSEAL-ORIGINAL-HERO-20261113：保留原角色，只参考视频动作

- 用户纠正“只是参考动作不换角色”，直接授权修正；上一轮视频人物替换被否定，旧技术历史保留但不抵销用户反馈。状态partial-complete / new-motion-blocked：原身份恢复完成，视频宽幅动作尚未完成。
- 设计/root/unseal_video_designer负责Design/实际体验，原开发/root/unseal_video_designer/unseal_video_dev负责代码/导入/QA，原美术沿角色链负责缓存。新证据Reference/UnsealOriginalHero20261113-r01，缓存同名；root-baseline由根维护，开发先保护真实原档/Prefs再Play。
- P0恢复原角色脸/发冠发带/白金青绿袍/护腕靴/比例；P1自然宽幅动作与原待机连续，原生粒子与掌脚锚点准确；保留已正确取消首引导及首武器/扣符结算。保护今日动作库和其他最新代码，不整文件退旧。
- 先核旧16帧与新动作库；现已实际看原home-hero及旧00/08，身份匹配但08是竖直抬掌，仍需判断足够动作姿态。必要新图需指定网页实际Image2.6、真实原角色/镜像动作附件，不能再抠视频人物。HERO-IDENTITY-ANCHOR.png/manifest固定原身份。
- DESIGN-AND-ACCEPTANCE.md定义OH-01—07：身份、发力、来源规格、符石同步、玩法、两比例实际交互、最终精确恢复。目标Editor与Android兼容，build=not-requested；不扩大其他UI/战斗/成长、不以旧123项冒充新测。
- 可行修正已完成：原16帧恢复Home待机/施法/收势，OriginalHero元数据明确interim；Home人物右移40至-25让原宽袍完整。首次普通流程、首武器/扣符与奖励保持，旧视频人物57PNG/57meta/JSON/meta共116文件逐SHA备份后移出Assets，只留独立符纸。保护现有动作库及其他最新代码。
- 必要技术17项PASS；最终022042-853的1334首次/1624自动中停与两比例图审通过。设计独立022348-023长屏真实pointer破封40→39/slot0、2.081秒样本结束、装备300→320尘/pending清除、原待机按钮恢复；不将旧抬掌视作新宽甩通过。375并排预览RESTORED-HERO-PREVIEW.png已实看。
- 生图阻塞：指定Playground两个原页/一次正常刷新/新页均白屏，无模型/上传入口；modelObserved=null、uploaded=false、generated=false。五份原角色/历史运行/镜像动作本地参考与prompt备好，raw/processed空，缓存receipt blocked；没有替换服务。OH-02新动作待页面恢复后继续，整轮不标experience-accepted。
- 收尾：02:24:35Z End/Stop、02:25:02Z桥关闭；两原档SHA56D214…429E8A/D26661…8F64B、Prefs/scope/GameView匹配。02:25:39Z精确核项目命令行后退出本轮专用86264并复核两档，见designer-recovery-check.json/designer-editor-shutdown.json；之后不再Play。生产freeze DCBEC073…C59，root只读复核。
- 交付Reference/UnsealOriginalHero20261113-r01/DELIVERY.md、EXPERIENCE-RESTORATION.md、TECHNICAL-RESTORATION.md。无APK/安装/真机/性能验证，剩余仅原身份视频动作迁移及其新验收，不自动扩游戏。

## UNSEAL-ORIGINAL-HERO-20261116：原角色视频动作迁移续作

- 用户要求：修改角色动作，视频只参考动作，不更换角色；无需重复确认。状态 implementing。
- 负责人：设计 /root/original_hero_motion_20261116；开发 /root/original_hero_motion_20261116/original_motion_dev；美术由开发调用，root独立关键姿态目视。
- 现状：20261113仅恢复原身份与旧16帧，宽幅视频动作仍未完成，不能以旧恢复或调速交付。
- 目标：保持原Home角色脸冠发袍/比例，参考镜像视频回身蓄势、宽弧甩臂向右送符、指向与衣发余势后收势；先4关键姿态通过再补帧/接入，手符接触同步与首引导取消保持。
- 优先级：P0身份/存档/真实结算；P1幅度自然、手臂连续、两比例与375宽；P1实际Image2.6/真实Prompt附件来源。
- 边界：Design/体验归设计，Assets/配置/QA/导入归开发，美术仅缓存；不回退library/attack_combo/成长奖励体验账号，不打包。Unity单owner、真实当前存档备份恢复。
- 证据与可观察门槛：Reference/UnsealOriginalHero20261116-r01/DESIGN-AND-ACCEPTANCE.md OM-01—07；ResourceCache同名；root-baseline归root。
- 本轮收口：blocked。角色代理曾受usage limit中断，恢复后root接管指定网页；最终因浏览器识别限制停止操作，没有本轮可核模型/附件/生成产物。生产代码和资源0修改、Unity0操作，无本轮新技术/体验通过，不重复使用旧恢复结果。两实际原档只读备份核验，无存档写入；未打包。

### 2026-11-16 用户授权接续（原角色动作，Chrome）

- 状态：implementing；历史blocked记录保留。设计 /root/original_hero_motion_continue，开发 /root/original_hero_motion_continue/motion_dev_resume；新证据写Reference/UnsealOriginalHero20261116-r01/resume-20261116，不覆盖历史。
- 继续OM-01—07：指定Chrome Images Playground实际Image 2.6，原角色与镜像动作真实附件，四关键姿态内部审查后补帧/接入；只参考动作，原角色身份不变，不打包。
- 浏览器交开发链美术单owner；新资产就绪前不Play，技术结束交设计实际体验；最终精确恢复存档/Prefs/编辑器。


### 2026-11-16 本次接续收口：新候选已生成，接入仍阻塞

- 状态：blocked / art-candidates-only。实际角色设计 /root/original_hero_motion_continue、开发 /root/original_hero_motion_continue/motion_dev_resume、美术 /root/original_hero_motion_continue/motion_dev_resume/motion_art_resume。
- 指定Chrome Playground恢复可用，实际gpt-image-2.6-sunburst及原身份/真实Home/镜像动作附件均有页面、AX与SHA来源证据；r02/r03四姿态、r04/r05单蓄势已生成下载，原角色身份保留。
- 设计/root实际逐批审图：r02/03蓄势与送符换臂且送符手越格；r04同一近侧右臂正确但仍竖掌，仅可作中间姿态；r05幅度变大却仍远侧臂，不能与近侧送符拼成连续动作。关键姿态未整体通过，未补连续帧、未接入。
- 透明勘误：raw工具预览的灰雾是alpha0隐藏RGB，实际暗浅底合成干净；r02/03/04 receipt及设计审查已更正，不需要清边损伤衣发。
- r06根因策略：固定相机不锁躯干，允许原角色胸肩转前3/4，原hero.png胸襟参考与关节示意真实附加。浏览器恢复后先核History无r06，再重附5图/参数且仅提交一次，0s及1m7s见生成中；结果未下载/未审，receipt=submitted-result-unverified。
- 用户明确授权“允许，用原生方式继续 Chrome”，此授权保留。但后续原生Computer Use因无法可靠确认当前Windows浏览器URL以实施策略而强制stop work/send final，故停止；这不是自动审批拒绝，也不证明生图失败。确切原文/调用见r06/evidence/browser-stop.txt。
- 本次生产代码/Assets资源0修改，Unity0操作，运行技术QA/独立体验均未开展，未打包。开发核root-resume-baseline679项SHA零漂移；两实际原档与root最初值的只读核验见technical交付/根侧核验。
- 交付Reference/UnsealOriginalHero20261116-r01/resume-20261116/DELIVERY.md及KEYPOSE-REVIEW-01—04.md、SOURCE-REVIEW.md。下一次先查已提交r06结果，不能重复生成；同臂/身份/自然关键姿态审过后再补帧/接入/新运行验收。

## ICON-125-20261116：参考图风格的图标与道具UI

- 用户明确要求参考效果截图的风格同时设计图标和UI，最终图标统一125×125；美术产出后由用户指定requirement_developer替换。本任务不采用其外部工单流程，仅G:/demos/gxdl。
- 状态 implementing；设计/root/icon_redesign_125，美术/icon_art，实际开发/icon_impl（requirement_developer，调用成功）。本轮不推进旧动作r06、不打包。
- 范围：12装备/7资源/24灵玉共43业务图标，旧41frame核复用；共享品质格及背包、装备详情、奖励/灵玉道具呈现。导航技能角色场景不扩展。用户截图实物材质、暗灰槽底、薄圆角边框和紧凑层次作为方向，不能只缩放旧图。
- 设计/可观察验收见Reference/IconRedesign125-20261116-r01/DESIGN-AND-ACCEPTANCE.md I125-01—06；真实动态数量、原五品级色/状态保持，禁止照搬参考不存在的丹药碎片系统。美术指定Playground实际Image2.6并真实上传用户图/本游戏截图，先概念后无字125生产资源。
- 当前无新资源/生产修改/Unity运行，源与体验分别待验收，root-baseline归root。

### ICON-125 本轮前检更新

- 精确atlas盘点：43业务图标，41frame仅历史空框生产未引用；实际品质框由quality-1..5/quality-atlas提供，因此制作43实物+5品质框+共享暗槽，不制作41重复空框。UI覆盖共享GearCard、背包、灵玉、锻造槽与奖励格；既有状态语义保持。
- 指定Chrome连接本轮实际恢复，但两个既有Playground标签均authentication token invalidated，需要用户正常重登。无模型/Prompt入口，modelObserved=null、uploaded=false、generation=false；美术停止在handoff，不操作凭据或旧动作r06。证据缓存evidence/playground-auth-invalid.png及txt。
- root内部已审通过设计规格（不是概念/美术通过）。当前生产和Unity仍0修改，先整理可审输入，状态partial-preparation / blocked-auth。

### ICON-125 本轮阶段交付

- 状态 partial-preparation / blocked-auth。设计规格、43真实keys映射、49规划资源（43物品+5框+1暗底）、共享UI状态规格、3份可上传参考SHA与概念/生产Prompt已就绪。
- 指定Chrome本轮实际可连接，Playground两标签token invalidated需用户正常重登；尚无模型/Prompt入口，未上传或生成，无art-ready概念/生产资源。
- 生产代码/Assets/Unity/存档0修改，无本轮技术/体验通过，无APK。详Reference/IconRedesign125-20261116-r01/DELIVERY.md；缓存receipt=blocked。原动作r06未碰。

### ICON-125 用户授权继续

- 用户现要求继续，本轮重开implementing，保留上述登录阻塞历史。设计重新唤醒原icon_art，唯一网页持有者核当前登录/model；恢复即沿既有参考/Prompt生成完整UI概念和代表图，再生产43+5+1的125资源。
- 新美术ready前不修改Assets或启动Play，不重新全盘盘点；旧动作r06不操作。目标与I125-01—06保持，不打包。

### ICON-125 新美术进展（本次继续）

- 登录已实际恢复；美术在指定Playground实际gpt-image-2.6-sunburst上传用户效果图+历史真实背包/灵玉截图，生成r02完整概念。设计/root审结构通过，实物荧光糖果感提出返工，正式分组压金并改写实材料。
- 正式r03装备12、r04资源7、r05红绿灵玉12、r06蓝紫灵玉12，均125透明单图；设计与root实际看原尺寸暗浅底/合成通过，43实物齐。来源/附件/下载/裁切SHA由各receipt/processing记录，详ART-REVIEW.md。
- r07五品质框+暗底/弹窗面板生产中。尚无Assets导入或运行验收，不能将美术通过视为运行通过。开发原14文件/13meta及84子Sprite身份已备份，最终49独特格125，7×7/903²含2像素padding、90entry保兼容名方案通过。

### ICON-125 美术就绪/接入实施

- r07五品质框与暗槽125、独立面板512已设计/root实际合成看图通过；全部49张125+panel共50文件就绪，r01/delivery-index.json提供r03—07来源SHA与裁切索引。
- 美术已停生图，receipt art-ready/integration pending；原需求开发者icon_impl已获得导入/最小UI实现授权，按唯一903² atlas保GUID/旧sprite身份及独立panel接入，避免49闲置单图冗余。
- 技术与体验尚待本轮运行；Unity由开发单owner，编译/导入/QA后交设计，不打包。

### ICON-125 本轮交付收口（2026-11-16）

- 状态：delivered within bounded Editor scope。r03—07全部49个125切片与512面板已通过美术，指定Image2.6及真实图片附件来源留存；唯一903²图集接入，旧GUID/84子Sprite身份保持。
- 背包/装备/灵玉库存/锻造槽/奖励格使用参考图写实材质、暗底与薄品质框，真实文字数量分层。技术232导入+25运行+4修复回归通过；设计独立session142612-320共12实际命令，主会话375宽预览实看通过。灵玉反馈遮挡已修复。
- 原档BF902…B74BC9/56D214…429E8A精确恢复，Prefs/scope/GameView匹配、桥关闭、用户PID39600保留。已发送Ctrl+P恢复原普通Play，但原生截图超时，最终登录画面未独立目视确认；不声称该项通过。root-final-audit685文件零漂移。
- 交付Reference/IconRedesign125-20261116-r01/DELIVERY.md、EXPERIENCE-FINAL.md、ROOT-RUNTIME-PREVIEW.png。无APK/真机，不扩全游戏或其他会话动作任务。主会话接手短文档收口，代理停止后续操作。


## SUBMISSION-FEEDBACK-20261117：版署反馈整批修复

- 用户确认执行Excel 34条反馈、替换两首背景乐、最终打包；明确要求整批修改完再批量技术验收和体验验收，不能逐条边改边验。
- 状态：implementing；未运行本轮技术或体验验收，未构建。
- 实际设计：/root/submission_feedback_20261117（game_designer）；实际开发：/root/submission_feedback_20261117/feedback_implementation（game_developer），由开发调用美术。
- 归属：设计维护Design及Reference/SubmissionFeedback20261117/TRACKER.json和体验报告；开发独占生产代码、配置、Server、资源接入、Unity与构建；美术只写唯一缓存批次。非Git工程先备份，不回退其他人的修改。
- 优先级：P0第34项继续挑战关闭弹窗失去操作；P1真实社交、账号/存档/状态联动及文本一致；P2美术与战斗可读性。全部纳入同一整批验收门。
- Unity由root只读核实G:/demos/gxdl、gxdl@d0106ad3a0b45359、2022.3.62f3c1、GxdlMain场景且idle后交开发独占；设计等待整批技术完成再接手。
- 逐项可观察标准：Reference/SubmissionFeedback20261117/TRACKER.json（34项，技术与体验分别pending-batch）。验收须新证据，保护原存档/Prefs/账号和Editor。
- 音乐：G:/背景乐/主界面.mp3用于非战斗，战斗副本.mp3用于主线/副本/塔；沿用循环淡化，处理音量及重复播放，保持GUID。
- 待用户决定：适龄等级、社交服务复用条件、最终包型。先实现不依赖这些的内容；适龄不自行认定，真实社交不假造，正式构建不绕空配置检查。
- 目标Android APK；正式认证参数及签名尚不完整，不将旧Development体验包冒充版署正式提交包。最终须产物路径/大小/SHA/新BuildReport；安装运行、真机、外部服务另列。


2026-11-17 本轮用户追加确认：适龄年龄为8，按“8岁及以上（8+）”配置标识及说明。此为用户指定口径，不声称独立评级。此前适龄待定解除；包型尚未明确，社交外部接入与Playground恢复仍待条件。


2026-11-17 用户包型确认：内部测试APK，沿用现有三档体验账号与原手机号入口。使用受控PhoneTest构建及独立phonetest包名，新版本新路径，不覆盖旧包、不改变账号存档作用域、正式认证不变。完整批量验收后再构建。社交需明确现有体验账号与真实服务身份的接入限制，不因此伪造在线聊天。


2026-11-17 内部体验社交接入决定：按已授权好友私聊与体验账号范围，采用独立本机测试服务、独立数据库/进程和显式开关；体验账号校验后发真实会话，真实关系/消息落盘。体验登录路由不注册到生产服务，客户端只在GXDL_PHONE_TEST通过loopback访问，正式HTTPS认证保持。设备联调需核对测试设备后ADB反向端口，最终APK社交依赖该本机服务，不承诺脱离电脑可用；不部署公网。集中验收覆盖双账号与服务/数据恢复。


### SUBMISSION-FEEDBACK-20261117 当前检查点

- 状态partial / waiting-art；开发可做实施已停止写入，53条文件路径（production45含meta/音频迁移，Editor3，Server5），清单和备份SHA在IMPLEMENTATION-CHECKPOINT.json。不是34项验收通过。
- 用户8+和内部0.1.5/versionCode6已配置准备；真实社交本机独立服务已写但未启动，内部APK社交需本机服务，TapTap生产社交身份桥未接。
- #23缺24独特天赋PNG，#29缺20普通精英PNG；#6/30完整概念未生成需方向审查；#19/22中性旧图替换已做但观感/覆盖未验；#27未盲改现有rig，仍需集中运行定位必要修复。详DESIGN-CHECKPOINT.txt。
- 指定Playground持续白屏，参考19份准备、生成下载0，art receipt=blocked，空manifest不可导入。
- Unity开发最终只读核idle/non-Play/non-compiling/non-tests后交回root；本轮只必要编译/语法检查，整批QA、独立体验、服务联调、Build/安装/真机均未开始。恢复页面后原代理followup续做，不提前跳过整批验收门。


### SUBMISSION-FEEDBACK-20261117：2026-11-18 用户恢复页面后续作

- 用户明确指定Playground页面恢复，原执行授权继续；状态implementing，历史partial记录保留。当前恢复仅用户报告，需美术本轮实际核验页面/model/附件后记录，不事后补造。
- 实际复用原开发/root/submission_feedback_20261117/feedback_implementation，要求其复用原feedback_art；生产/浏览器权限分离，Unity须重新确认G:/demos/gxdl后开发单人持有。
- 剩余资源按24天赋+20普通精英+首页/战场完整概念与必要资源推进；#19/#22覆盖、#27动作问题纳入整批集中验证与必要修复。保护原53路径和备份，不回退其他迭代。
- 所有修改及资源接入完成后集中技术→设计体验→批量返修/回归→8+内部体验APK0.1.5/versionCode6。仍不逐条修改后穿插验收，不以来源或编译通过代替运行。

- 2026-11-18 内部包口径：保留Development/独立包名保护，Unity Development Build引擎水印列内部测试包限制，不当游戏自身英文残留，也不宣称全包无英文。启动Logo及玩家可见游戏文案仍须中文化；正式提交包另行区分。

- 2026-11-18 资源制作进展：56/56静态视觉通过（24天赋、20普通精英、3首页入口、2中性徽章、7补齐主题战场）。末4天赋遇页面额度耗尽后，通过原raw的连续alpha映射清边闭环，不更换模型/服务、不冒称新生成。最终索引r02/delivery-index56.json冻结，root来源抽核、开发最后接入中，尚无集中技术或体验通过声明。

- 2026-11-18 整批门开启：56最终资源接入/尺寸/目标SHA/GUID/十章映射核实，root独立来源抽核PASS（ROOT-ART-SOURCE-AUDIT.json）；开发现在启动集中技术验证，设计等待恢复交接后实际体验。此前无逐项穿插运行验收。所有34项仍未标体验通过。

- 集中技术首批证据：Node服务11/11、规则/账号规则、PhoneTest真实UI登录重登与隔离档恢复已通过；#34首跑因旧胜利夹具实战失败未进入目标分支，保留失败记录后只修隔离夹具重跑。此为技术进展，设计体验和APK仍待。

## OPENING-AFTER-LOADING-20261121：登录后开场缺失修复

- 用户报告：登录成功、Loading结束后没有显示开场动画界面。
- 状态：implementing；本轮尚未宣称技术或体验通过。
- 范围：仅登录→真实Loading与收尾→独立开场→主界面的衔接缺陷；不扩展整批prefab迁移、不生图、不打包。
- 当前规则：编辑器每次登录都播放开场；构建按账号已看记录决定首次播放。仅自然结束或主动跳过写已看，中断不记。Loading资源完成、进度100%与0.35秒收尾都完成后才移交。
- 实际角色：设计 /root/fix_opening_after_loading（game_designer）；开发 /root/fix_opening_after_loading/opening_flow_implementation（game_developer）。开发独占生产代码与技术测试；技术恢复后交设计独立体验，不并行操作Unity。
- 验收：实际登录按钮→真实Loading→开场可见且画面推进→自然结束或跳过→主界面；再次登录仍重播；核对中断标记与构建分支。若构建/正式外部认证未实际运行，明确分层，不以编辑器替代认证结论。
- 数据保护：测试前保存原存档、PlayerPrefs、账号与编辑器状态；结束验证恢复。前轮结算类型名和天赋悬空调用修复保持。
- 证据：Reference/OpeningAfterLoading20261121/；技术在technical，独立体验在experience。

## MVP-FLOW-20261124：主界面 prefab 化、全流程审计修复、商城/排行榜、资产清理

- 用户目标：按 UI 规范重构主界面；逐界面验证不得有流程中断；补齐商城/聊天/排行榜；最后清理无用代码与资源，减小工程与包体。MVP 阶段功能优先，视觉后续按系统出效果图再迭代。
- 状态：technical-verified（Editor 自动流程验证 17/17 PASS，含清理后复测）。未打包、未真机。
- 主界面：`HomeView` 四件套重做（View / `HomePrefabSource` / `HomeView.prefab` / `ShowHomePrefab` 接线），新背景 `home-bg.png`，右侧五入口（账号/商城/鉴宝台/资源秘境/背包），旧构建保留回退。
- 全局流程修复：`OpenScreen` 改为先关旧页再开新页（重入同页不再自开自关）；`UiManager` 缓存改按 ModuleId；`Clear()` 同步收起 prefab 页；prefab 页根节点显式给 750×1334（根因：0 尺寸导致背景/底栏不渲染）。
- 页面修复：战前准备关闭回来源页、开战先收起准备页、结算前隐藏战斗 HUD；成长总览 Bottom 绑定与缩放；战备返回键与技能图标；图鉴返回/品质表默认隐藏；世界地图走统一 OpenScreen；古塔选层/翻段/提示绑定；秘境详情难度切换；章节/战前重复监听；登录失败页空引用。
- 新系统（MVP）：商城（灵尘兑换破封符/玄铁/悟道点/突破石，先校验后扣费，规则自检 9 项）；排行榜（本机真实战力/主线/塔层，联网待服务器）；聊天复用既有好友私聊，非测试包显示不可用提示。价格为首版验证值。
- 验证方式：Unity MCP（127.0.0.1:8080）驱动 Play；`Assets/Scripts/QA/GxdlAutoFlow.cs` 在 `Tools/qa/flowcheck.run` 存在时自动登录→跳过开场→逐页断言并截图→恢复存档。报告 `Logs/qa/flow-check.txt`，截图 `Logs/qa/shots/`。另有 `Tools/qa/generate.trigger`（重建 7 个 prefab）与 `Tools/qa/reimport-textures.trigger`（重导入贴图）。
- 清理（均经 GUID + 字符串引用核实，git 可恢复）：4 个 `.bak` 生成器；3 张无引用布局皮肤；旧 `Game/UI/` 13 张与 `Game/Music.mp3`；`Guxu/character|stone|Guardians|Items` 旧素材；`SpineHero/` 重复源（仅留材质）；`Assets/Screenshots` 空目录。Assets 220M→204M。
- 包体根因与处理：229 张贴图 220 张原为 Uncompressed（RGBA32 估算 449MB，对应 APK 409MB）。`GuxuFinalArtImporter.ApplyBuildCompression` 统一 Android 覆盖 ASTC 6x6（分辨率上限不变），`GuxuStoryCheck`/旧 `Guxu/UI`/两张整页背景同步纳入；重导入后 228/229 覆盖（余 1 张 Video 帧原有独立 ASTC 设置）。运行时无 GetPixels/ReadPixels，压缩不影响玩法；清理后复测 17/17 PASS，主界面目视无压缩瑕疵。预计 Android 包体降至约 1/4—1/5，需实际打包核实。
- 历史证据目录清理（用户 2026-11-24 授权）：`Reference/`（11G）、`ResourceCache/`（1.2G）已整体删除，`Logs/` 仅余编辑器进程占用的 6 个小日志；工程目录 17G→5G（其中 Library 4.2G 可由 Unity 重建）。此前各轮 ITERATIONS 条目引用的 Reference 证据文件已不存在，记录仅作历史结论。29 个旧 Editor QA 脚本仍会向 `Reference/...` 写报告，需运行时按需补建目录。
- 遗留：古塔楼层列表美术未挂上（功能可用）；成长总览"前往资源秘境"按钮文字对比弱；主界面"账号"入口无专用圆环图。均属视觉，留到各系统效果图阶段。

## HOME-RESKIN-20261124：主界面成品切图换皮

- 用户授权把 `Art/主界面/主界面切图` 用于现有主界面。状态：technical-verified（Unity 导入、编译与 prefab 回读）；运行体验未验收。本轮使用用户成品，无新生图；效果图作为布局参考，`bg.png` 与已接入 `home-bg.png` 的 SHA256 相同，保留已有背景引用。
- 设计 `/root/home_reskin` 负责映射、规范及静态验收；开发 `/root/home_reskin/dev_home_reskin` 负责接入、备份及 Unity 导入编译；主会话独立审查。仅开发持有已核对路径的 Unity 实例。
- 范围：更新头像、身份线、双资源栏、破封主按钮、自动/保留工具条、任务条、三项右侧入口及底栏。账号和商城保留既有入口与回退素材；按实际可用功能处理装饰性加号。角色 Spine 2.0、古石待机及碎裂、现有业务规则与结算保持。
- 验收规格见 `Reference/HomeReskin20261124/DESIGN-AND-ACCEPTANCE.md`。本轮以用户成品、代码、原尺寸图像、资源导入与 prefab 绑定核对；不另启 Play、不运行 QA、不修改现有会话和存档、不打包。实际运行体验仍待验证。
- 收口证据：11 张新 Sprite 已导入、序列化至 `HomeView.prefab`，哈希与批次 manifest 一致；原 prefab GUID 保留。设计独立回读角色、古石、伙伴及遗宝四组位置/尺寸/pivot 与备份一致。五个底栏热区和独立自动/保留入口保留，真实灵尘图标定位与改名唯一热区可回读。证据在 `Reference/HomeReskin20261124/technical/` 与 `ResourceCache/20261124-HomeReskin-r01/`。原 Play 与场景保持，既有缓存页面的即时刷新未验证。

## RESIDENCE-RESKIN-20261125：洞府系统换皮重构

- 用户确认执行：按 `Art/洞府` 效果图重做洞府面板与遗宝弹窗，要求文字颜色和字体与效果图一致。状态：technical-verified（Unity MCP 驱动 Play，自动流程 21/21 PASS 含新增 4 用例；运行截图与效果图逐项对照）。真机与打包未验。
- 切图：开发用 Python PIL 直接从用户成品效果图切片并修补文字区（用户本轮提供成品图，未走 Image 2.6 新生成；pipeline manifest 的生图来源校验不适用，改为手动导入并在本目录记录来源）。原图与产物存 `ResourceCache/residence-20261125/raw|processed/`，切片脚本 `Tools/ResidenceReskin/slice.py`。文字一律实时渲染：效果图内烘焙文字全部修补擦除，颜色逐一采样（标题 #532E06、正文 #2C2C27/#5D6270、铭牌 #494641/#302D25、青按钮 #FDFCF7、金按钮 #432B0F、弹窗名 #333333、战力 #654116）。字体沿用工程唯一字体 `Resources/Original/GameFont`（效果图字体与之同风格，未引入新字体文件）。
- 新贴图 15 张入 `Assets/Resources/Game/Guxu/Expansion/UI/`：residence-bg/panel/window/plaque/divider/close、residence-btn-{teal,gold}-*（共 6）、relic-modal-panel、relic-btn-{teal,gold}。`GuxuFinalArtImporter` 对上述前缀强制 Sprite/Single 导入并按用途设分辨率上限（panel 1024、bg 2048、按钮 256/512、小件 128—256），Android ASTC 6x6 沿用既有规则；重导入后 328 张中 327 覆盖。注意：prefab 生成器以 Sprite 引用加载，贴图必须先以 Sprite 类型重导入再重建 prefab，顺序颠倒会存入空引用（本轮实际踩坑两次：Default 类型、`HomePrefabSource.SkinPath` 固定指向 HomeReskin/ 子目录导致 relic 切片加载 null，已改为根目录优先回退）。
- 洞府四件套：`Modules/ResidenceView`（Apply 只填境界/战力，BindActions 七回调，IsComplete 门控回退）、`Editor/ResidencePrefabSource`（注册 "Residence"）、`UI/ResidenceView.prefab`（Modal 呈现：全屏蓝白背景 + 居中 700×654 装饰面板，六入口按钮 + 关闭）、`ShowFinalResidence` 接线（OpenScreen 优先，失败回退旧 OpenHandbook 构建；排行榜/商城走各自 OpenScreen）。`GxdlPrefabRefresh` 加入 Residence。
- 遗宝弹窗换皮：HomeView.prefab 的 Relic Decision 重排为 700×316 金纹横幅（图标位于光晕位、名/战力/词条实时文字、分解/装备独立按钮皮），`HomeView.ApplyRelic` 正向战力色改暖棕 #66400F 系；旧构建 `ShowDrop` 同步（`SkinSealButton` 只换皮保回调）。与效果图的唯一偏差：擦除了弹窗右上角烘焙的关闭 X——本弹窗必须装备/分解二选一，不允许关闭，保留会误导。
- 自动验证：`GxdlAutoFlow` 新增 4 用例——打开洞府为 ResidenceView、BtnClose 回主页、Chop 造待处理遗宝刷新主页弹窗、分解后弹窗关闭（其后等 3 秒让灵尘演出结束再继续导航，否则 busy 阻塞后续步骤）。报告 `Logs/qa/flow-check.txt`，截图 `Logs/qa/shots/`。

## BAG-RESKIN-20261125：背包系统换皮重构

- 用户确认执行：按 `Art/背包` 效果图换皮背包面板与装备详情弹窗；弹窗带缩放动画。状态：technical-verified（Unity MCP 驱动 Play，自动流程 25/25 PASS 含背包新增 4 用例；运行截图与效果图逐项对照）。真机与打包未验。
- 切图：开发用 Python PIL 从用户成品图切片并修补（未走 Image 2.6/pipeline；来源与脚本在 `ResourceCache/bag-20261125/` 与 `Tools/BagReskin/`）。**两张原图分辨率不同**（bag 1440×2560、详情截图 1125×2000），坐标分别按 0.521/0.667 折算，首轮详情切片因沿用 bag 比例整体偏移返工。烘焙文字全部修补擦除实时渲染，颜色逐一采样（标题 #9B6728、分隔标题 #363D47、槽位名 #3A414A、提示 #3F4750、角标白 #FFFFFE、详情名 #3D4248、副题 #6A5944、分段标题 #5C4B36、按钮字 #5C4318/#394A53）。字体沿用 GameFont。
- 新贴图 12 张入 `Expansion/UI/`：bag-panel/close/header/slot/badge/divider + gear-detail-panel/iconbox/header/close + gear-btn-gold/teal。`GuxuFinalArtImporter` 前缀分支补 Sprite/Single 导入与分辨率上限；Android ASTC 沿用。关闭按钮与阶数角标用"原位覆盖"贴法（live 图略大于烘焙图，免去异形抠图）。
- 四件套：`Modules/BagView`（Apply 填 12 槽角标、ShowDetail/HideDetail 缩放动画、BindActions 槽位/关闭/锻造/镶嵌/详情关闭；动画协程挂在目标节点宿主上随销毁终止）、`Editor/BagPrefabSource`（注册 "Bag"）、`UI/BagView.prefab`（面板 44,334,666,682 + 详情子面板 200,384,364,588 默认隐藏）、`ShowBag` 接线。`UiScreens.Bag`、GxdlPrefabRefresh 同步。
- 呈现：按用户确认"弹窗带缩放效果"。背包要透出主界面，**不走 OpenScreen**（它先关当前页）——直接 `UiModules.Open` 叠开并手动 FitPage 挂 canvas，主页 prefab 保留在背后；详情做成 BagView 子面板（OpenScreen 无法叠页）。
- 功能映射（真实系统为准）：效果图"卸下/出售价格"游戏无对应功能，不展示；按钮为真实存在的"槽位锻造"（金）与"灵玉镶嵌"（青），点击打开既有旧弹窗（旧弹窗为 canvas 最新兄弟节点，层级在 prefab 页之上）。基础属性=四维真实值，附加属性=战斗词条，装备描述=槽位锻造与强化说明。
- 自动验证：`GxdlAutoFlow` 新增 4 用例——打开背包为 BagView、点 Slot 1 开详情（以 BtnClose2 出现断言）、关详情、关背包回主页（缩放动画各 0.1 秒，断言前等待）。
- 本轮踩坑（已回写 skill）：生成器 `builder.Node/TextNode` 父路径必须全路径（Slot 0 → BagPanel/Slot 0，两处）；panel-local 坐标与 screen 坐标不能混用（槽位行/饰线/提示被二次减面板原点，截图对照发现）；prefab 重生成前贴图必须先重导入为 Sprite；详情关闭按钮 local y 为 3 不是负值；编辑器在 Play 模式时 generate/flowcheck 触发器不会被消费。与效果图剩余偏差：背包标题饰带烘进面板裁切区外，标题以金色实时文字直接呈现（无饰带底）。

## BAG-ATLAS-20261125：背包换皮改用官方图集

- 用户指出此前从效果图自切的方式有问题，改提供透明底官方图集（`ResourceCache/bag-atlas/raw/atlas-bag|atlas-detail.png`，各 1024×1024），要求按图集重新换皮、与效果图一致。状态：technical-verified（25/25 PASS；截图对照图集渲染与效果图一致）。真机未验。
- 图集切图方式：图集自带 alpha，PIL 按像素矩形 `crop` 直接裁剪并原样保留透明轮廓，不做抠图/去底；网格+2x 刻度放大定坐标。烘焙文字（背包/当前装备/页脚提示/卸下/强化）仍修补擦除实时渲染；圆形关闭按钮加椭圆 alpha 蒙版；详情分段饰线左右对切成两片、右片 X 翻转对称，中间实时文字。切片脚本 `Tools/BagReskin/slice3.py`。
- 替换贴图：bag-panel/close/slot + 新增 bag-cloud（面板左下云纹）+ gear-detail-panel/header-l/header-r/close + gear-btn-gold/teal 全部换成图集版本；删除被替代的 bag-divider、gear-detail-header、bag-title。面板比例按图集（bag 682×563、详情 373×516），内部布局相应压缩，文字颜色沿用此前采样值。
- 分段标题饰线为中央菱形+两侧长线（无烘焙文字），实时文字置于中间；详情关闭按钮用图集圆形 X 原位覆盖。

## RELIC-ATLAS-20261125：新获得装备弹窗改用官方图集

- 用户提供新获得装备弹窗官方透明图集（`ResourceCache/relic-atlas/raw/relic.png` 1024×1024），要求重新换皮该弹窗，且装备详情 tips 弹窗关闭按钮使用图集中的按钮。状态：technical-verified（Unity MCP 驱动 Play，自动流程 24/24 PASS 含遗宝弹窗用例；运行截图与效果图对照一致）。真机与打包未验。
- 图集切片（`Tools/BagReskin/slice-relic.py`，纯矩形 crop 保 alpha）：relic-modal-panel（1000×496，分隔线/金饰带已烘焙保留，无烘焙文字）、relic-close（116×128，椭圆 alpha 蒙版圆形 X）、relic-btn-teal/gold（256×95/93）。分隔线切片资源共用：面板切片已烘分隔线，不另切 divider。
- 布局按图集比例重排：面板 700×346 @ (25,876)；名/战力/词条实时文字居中（#333333/#654116/#5B5248 沿用采样色）；分解 (506,138,170,63)、装备 (506,215,170,63)。节点名 Resolve Equipment/Equip Relic 不变，Bind 回调不动。
- 装备详情 tips 弹窗关闭按钮 BtnClose2 由 gear-detail-close 换为 relic-close（64×70 原位覆盖），两个弹窗共用同一关闭钮贴图；删除旧 gear-detail-close.png。
- 导入器 `/relic-` 前缀统一 Sprite/Single 导入，relic-close 128、relic-btn 256、relic-modal-panel 1024。重导入 341 张；Home/Bag 等 9 个 prefab 全部重建。
- 验证证据：`Logs/qa/flow-check.txt`（24 PASS 0 FAIL，RESULT PASS）、`Logs/qa/shots/07-RelicDrop.png`（弹窗金饰带横幅+居中文字+青金按钮）、`04-after-Slot_1.png`（详情弹窗图集圆形 X）。本轮 flowcheck 断言数由 25 收敛为 24（详情关闭用例合并），全绿。

## TOAST-TEXT-20261125：tips 提示改纯文本动画

- 用户要求去掉 tips 弹窗深色底板，只留文本：颜色 #2BEA9E、字号 40、宽 500×高 60，从屏幕中间 0.3s 渐显同时缓动上升 150px，停留 0.8s，0.2s 渐隐消失。状态：technical-verified（24/24 PASS，含遗宝分解触发提示的路径）。动画为 1.3s 瞬态，未截到逐帧运行图；真机未验。
- 实现：`GxdlGame.Notify` 改为纯文本 Text（无底板，raycast 关闭），`ToastMotion` 协程用 unscaled 时间驱动 alpha 与世界坐标上升（乘 canvas.scaleFactor 适配分辨率），缓动用 easeOutCubic；结束销毁提示层。gem 页旧的特殊定位（FitGemFeedback/小号字分支）随底板一并移除。所有调用 Notify 的提示统一新样式。

## RESIDENCE-ATLAS-20261125：洞府界面改用官方图集

- 用户提供洞府界面官方透明图集（`Art/洞府/洞府切图图集.png` 1024×1024，alpha 完整），按图集重新换皮洞府。状态：technical-verified（Unity MCP 驱动 Play，自动流程 24/24 PASS；运行截图 `Logs/qa/shots/01-Residence.png` 与图集/效果图对照）。真机与打包未验。
- 图集切片（`Tools/ResidenceReskin/slice-atlas.py`，网格+2x 刻度定坐标；**像素对象选取**：粗框外扩 ≥4px 后做 alpha 连通域分析——框中心种子提取主对象连通体（含柔光晕）、框内完整小件保留、触界邻居组件清零、裁紧贴包围盒；用户指正"应像 Photoshop 一样按像素框选对象"后由纯矩形 crop 返工为此法，已回写 ui-reskin skill；纯矩形版旧切片目录 `ResourceCache/residence-20261125/processed/` 保留作来源记录，不影响工程，旧方法仅限洞府轮替换、背包/遗宝等其他界面换皮不受影响）：residence-panel 578×480、residence-plaque-title 624×128、residence-window 326×210、residence-divider 208×45、6 个入口按钮（整理行囊/同伴招募/好友与聊天/商城带图标切片 + 悬浮/点击两枚纯蓝状态按钮）、residence-plaque 状态铭牌底、residence-cloud 云纹。烘焙文字（洞府·修行手记、四枚入口按钮字、三枚状态按钮字）全部逐列渐变修补擦除实时渲染，图标保留；文字采样：标题 #472205，蓝按钮 #134359 系（白字沿用 #FDFCF7），金按钮 #3D280A 系（沿用 #432B0F）。首轮漏擦 悬浮/点击状态 两枚蓝按钮与商城残留，返工一次。residence-bg 整页背景图集未提供，沿用上一轮切片。
- 替换贴图 12 张覆盖旧截图切片；residence-close 改复用新获得装备图集圆形 X（切片共用）；新增 residence-plaque-title、residence-cloud。`GuxuFinalArtImporter`：residence-window/plaque-title 上限 512，plaque/divider/cloud 256。重导入 343 张。
- 生成器几何按图集对象切片比例重排（`ResidencePrefabSource`）：面板 700×580 @ (25,377)，标题铭牌盖左上（460×94），窗景 250×161，分隔线 200×43，双状态铭牌 190×71，六按钮高度统一 62、宽度按各切片原比例（132—196）两列对齐；节点名与 Bind 全部不变。返工两次：同行档案按钮过窄致文字换行（按新切片宽度重排）、故事文本与基础战力铭牌压面板边框（左移）。
- 踩坑：改生成器后立即触发的重建可能在脚本重编译前用旧程序集执行（Tick 先删触发器后构建）——prefab 时间戳更新但内容仍旧几何；需在编译完成后再触发一轮重建并抽查 prefab 序列化值。后台无焦点编辑器的 EditorApplication.update 被节流，触发器长期不被消费——`GxdlPrefabRefresh` 静态构造函数改为域重载后立即消费（先 `RunClassConstructor` 强制注册各生成器）；首版直接在未就绪时构建导致 9 个 prefab 全部返回 null、HomeView.prefab 根节点丢失 UiModule 组件（运行时报"prefab 根节点缺少 UiModule 组件：UI/HomeView"，flowcheck 因回退路径假绿）——Tick 已加就绪探测：先构建 Home，返回 null 则保留触发器待下轮重试，不再写坏 prefab；最终形态：构建后读回校验（根节点须有对应 UiModule，GetComponent(string) 对命名空间类型不可靠，改按类型简单名匹配），任一失败保留触发器 3 秒后整轮重试；refresh 期间 isUpdating 挡下后由 delayCall 接力；后台编辑器 isUpdating 可能长期为真导致 update/delayCall 链全部静默挂起——新增菜单 `Gxdl/QA/Rebuild Main Prefabs`（直调绕过守卫，同步构建+校验+写报告），MCP/人工均可确定性触发。
- 验证证据：`Logs/qa/flow-check.txt`（24 PASS 0 FAIL，RESULT PASS）、`Logs/qa/shots/01-Residence.png`。
- 用户对照效果图反馈按钮大小/样式与文本样式/位置偏差后重排（RESIDENCE-ATLAS 返工 2）：面板按效果图比例放大为 700×640（图集切片纵向 +10% 轻微拉伸），按钮统一 224×76 两列对齐（列位 84/386，效果图为等宽大按钮），文本字号整体上调（标题 27、故事题 30、故事文 22、铭牌签 20/值 32、按钮字 25），铭牌放大 240×90（签上值下两行居中），窗景 252×162，标题铭牌 360×66（文字居米白带），关闭钮 (642,12,56,60)。验证同上轮证据路径。
- 全屏 UI 背景统一：用户新图（`ResourceCache/fullscreen-bg-20261125/raw/bg.png` 844×1500）覆盖 residence-bg.png；用户明确主界面背景不得覆盖，home-bg.png 已从 `ResourceCache/20261124-HomeReskin-r01/raw/bg.png` 恢复原图。源图内容替换、GUID 不变，prefab 引用无需重建。

## FIVE-SYS-RESKIN-20261125：商城/图鉴/修行/寻墟/战备 五系统按效果图换皮

- 用户要求以 `Art/{商城,图鉴,修行,寻墟,战备}/` 的切图与效果图（待替换界面.png 为被替换页）对五个系统换皮，切图用洞府像素对象选取新方法，数据用正式配置与真实玩法逻辑，文本颜色/位置/样式与效果图一致。经确认：五系统一次全做、书法标题保留烘焙其余文字擦除实时渲染、修行→GrowthHubView、寻墟→WorldMapView、战备→LoadoutView（战前布置主动技能页）。状态：technical-verified（Unity MCP 驱动 Play，自动流程 25/25 PASS 含新增 Codex/Loadout 步骤；运行截图 `Logs/qa/shots/{17-Shop,14-GrowthHub,21-Codex,09-WorldMap,23-Loadout}.png` 与 `ResourceCache/{shop,codex,growth,ruins,prep}-20261125/raw/mockup.png` 逐页对照）。真机与打包未验。
- 切片：`Tools/ResidenceReskin/slice-five.py` + `slice-five-fix.py`（像素对象选取法），产出至 `ResourceCache/{sys}-20261125/processed/`（58 张带系统前缀 shop-/codex-/growth-/ruins-/prep- 拷入 `Assets/Resources/Game/Guxu/Expansion/UI/`）。首轮踩坑：切片只落在 ResourceCache 未入 Assets、生成器引用前缀名全部加载失败（白页）——补拷贝修复；`Resources.Load` 路径误带 `Assets/Resources/` 前缀返回 null——改为 `Game/Guxu/...`。商城残留烘焙字（购买×2、380+、底栏整句、副题鬼影）用 `erase-shop-text.py` 文字像素 bbox 检测 + 垂直渐变修补擦除；首版把灵尘图标与底栏装饰符一起擦掉，`recrop-shop-2.py` 从图集重裁并限定擦除区域返工。
- 全屏背景：五页统一 ui-bg-fullscreen.png。踩坑：生成器用 `Sprite.Create` 造的精灵是运行时对象，存 prefab 后引用失效（白底）——`GuxuFinalArtImporter` 增加规则把 ui-bg-fullscreen 按 Sprite/Single 2048 导入，生成器改用 `AssetDatabase.LoadAssetAtPath<Sprite>`。
- 生成器重写（节点名与 Bind 全部不变，仅外观/布局）：Shop（卡行 687×194 四行、shop-card/shop-buy-gold/shop-back/shop-subtitle-bar/shop-currency/shop-footer-bar/shop-title-art 书法烘焙，Coin Balance 改纯数字 MiddleRight 82 宽——原 "灵尘 N" 24 号在 62 宽文本框折行只显示"灵尘"）；Codex（620×780 面板 codex-panel，四列格子 codex-cell，标题下移避面板顶边花纹，Tier 弹窗纸色化）；GrowthHub（四行 growth-card-* 777×205，底部按钮从板坐标 1245 下移至 1362——原 940 板 ×0.798 换算后与第四行卡重叠）；WorldMap（十卡 ruins-ch-N 297×137 行距压缩 {300,480,660,840,1020}，页签下移 1190 避让，锁定暗罩 alpha 归 0——效果图锁定章不压暗、仅"尚未解锁"文字；进度/页签文字仍为真实存档数据）；Loadout（prep-skill-1..4 圆盘 160×172 + prep-nameplate，已装备角标移到圆盘上方 (12,-28)，技能名对准铭牌可见区（切片可见图案仅上部 1/3，Name y 迭代 172→158→150），详情卡 Selected Icon 白方块根因是 `LoadoutView.Apply` 从未赋图——补 `selectedIcon.sprite = skillIcons[selected].sprite`；装备/自动释放为真实玩法按钮效果图未绘制故保留）。
- 坐标约定记录：`UiPrefabBuilder.Node` 为左上锚 pivot(0,1)、y 存负（生成器 y 正=向下）；`AnchoredNode` pivot(0.5,1) 即 x 为水平中心——Loadout 返回钮按 (20,45) 写导致左切出屏，需写中心坐标 (95,74)。排版核查用 prefab YAML 直读 RectTransform（dump-rects/dump-plates 脚本在 `Tools/ResidenceReskin/`）。
- 验证：`Logs/qa/flow-check.txt` 25 PASS 0 FAIL（含 Codex 打开/返回、Loadout 打开/返回新断言），五页截图对照效果图通过；购买扣灵尘、进度显示、选中技能联动均为真实配置/存档数据。

- 破封演出：碎裂后出现的遗宝增加上下浮动（±6px @ 2Hz，正弦叠加在原 20px 上升之上，停留阶段持续），幅度小于古石待机浮动（`GuxuUnsealParticles` 破裂与 reveal 两个循环）。
