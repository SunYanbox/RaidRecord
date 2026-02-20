### 0.6.11

重构本地化:
- [x] ChatBot
- [ ] Services
- [ ] Systems
- [ ] Utils
- [ ] Layouts
- [ ] Pages
- [ ] Cards
- [ ] Widgets

### 0.6.9

- [x] 优化快速购买界面的装备选择逻辑, 允许修改逻辑为 **排除已选中**
- [x] 理赔控件
- [x] 快速起装购买装备时修复装备耐久

### 0.6.7

TODO
- WebUI配置界面
    - [x] 在模组中购买物资是否是FIR状态
    - [x] 当前语言修改 
- [x] WebUI一键配装界面
    - ~~通过选择[MudChip](https://www.mudblazor.com/components/chips) 快速决定购买起装列表中的哪些类型的物品~~
    - [x] [树图](https://www.mudblazor.com/components/treeview)显示起装列表(尤其用于槽位的显示) 也可以用[桑基图](https://www.mudblazor.com/components/sankeychart)显示价格
- [x] 为战绩详情显示详细带入带出物品 考虑[纸张](https://www.mudblazor.com/components/paper)+树图实现
- [ ] 为带内置槽位的物品提供初始化生成方式
- ~~为PricePage的快速购买提示框提供动态价格显示(MudMessageBox内部难以实现)~~

### 0.6.4

更新计划
- [x] 依赖注入重构
    - [x] RaidRecord/Core/Utils/ItemUtil.cs
- [x] 本地化重构
    - [x] RaidRecord/Core/Locals/LocalizationManager.cs
    - [x] RaidRecord/Core/ChatBot/RaidRecordManagerChat.cs
    - [x] RaidRecord/Core/ChatBot/Commands/ClsCmd.cs
    - [x] RaidRecord/Core/ChatBot/Commands/HelpCmd.cs
    - [x] RaidRecord/Core/ChatBot/Commands/InfoCmd.cs
    - [x] RaidRecord/Core/ChatBot/Commands/ItemsCmd.cs
    - [x] RaidRecord/Core/ChatBot/Commands/ListCmd.cs
    - [x] RaidRecord/Core/Systems/CustomStaticRouter.cs
    - [x] RaidRecord/Core/Systems/RecordManager.cs
    - [x] RaidRecord/Core/Utils/CmdUtil.cs
    - [x] RaidRecord/Core/Utils/ItemUtil.cs
    - [x] RaidRecord/RaidRecord.cs
- [x] 价格计算方式重构
- [x] 对局元数据优化: 新增击杀数量, 爆头数等
- [x] list指令优化: 增加击杀数量信息
