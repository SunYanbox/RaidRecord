# Tabs {.tabset}

## 简介与安装

### 模组概述

原版游戏仅保存有限统计数据(总览页面), 而本模组精准记录每一场对局的完整战斗日志, 包括:

**击杀**

对局详情页面查看

- 使用的具体武器
- 命中部位(头部、胸部、四肢等)
- 击杀目标类型(PMC、Scav、Boss、Scav Boss、Boss 带的守卫等)
-  击杀目标的时间

**物资**

对局详情页面查看

- 带入对局的物品与带出对局的物品清单
- 这场对局中新搜到的物资, 损失的物资, 变化的物资(子弹数量增加, 药品与武器耐久减少等)

**对局收益**

对局详情页面查看 / 战绩列表页面查看

- 对局地图信息, 对局游玩时间
- 对局入场时的
    - 战备价值(类似三角洲行动, 计算武器, 弹挂, 背包, 护甲等装备的价值和)
    - 安全箱内价值(安全箱内所有物资总价值, 有不少情况下, 这里的价值占非常大的比率)(Scav模式为0)
    - 总的带入价值(该值等于战备价值+安全箱内价值+背包弹挂口袋特殊插槽内的物品价值)
- 离开对局时
    - 带出总价值(算是毛利润)
    - 战损(使用物品消耗, 丢弃物品, 耐久损耗等)
    - 净利润(毛利润-战损)
- 对局结果
  ```csharp
  // 理论上有这些:
  public enum ExitStatus
  {
    SURVIVED, // 幸存
    KILLED, // 被击杀
    LEFT, // 离开对局(指客户端Esc后那个选项)
    RUNNER, // 匆匆撤离
    MISSINGINACTION, // 迷失
    TRANSIT, // 转移
  }
  ```
- 如果幸存/转移/匆匆撤离 : 撤离点信息, 游戏风格信息
- 其他情况: 被哪个阵营的哪个敌人(除了boss, 名称不重要)使用什么武器命中你哪个肢体淘汰你的信息

**价格**

在价格页面查看

> 该功能偏向调试, 主要用于验证模组计算的价格是否正确, 也可以用于模糊搜索与关键词相关的物品名称与ID, 以及购买FIR物品过任务

```
![Quick buy](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/webui_price_ch.png?raw=true)
```

**快捷起装**

对局详情页面查看 / 战绩列表页面查看

> 可以快速购买指定对局进入对局时的装备
> 可以筛选指定需要的装备类别

```
![Quick buy](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/webui_quickEquip_ch.png?raw=true)
```

**意外情况处理**

- 模组会在服务端记录数据, 如果对局结束前服务端在非本模组原因下崩溃, 只要不是客户端刚对局结束时服务端就崩溃, 重启服务端仍然有概率(>90%)正确记录对局数据
- 对局启动后Alt+F4或者客户端崩溃, 下一次启动战局后会导致记录的该战局的缓存对局结果被记录为未知结局

**对局列表页面**

```
![Quick buy](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/webui_list_ch.png?raw=true)
```

**WebUI主页**

```
![Quick buy](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/webui_home_ch.png?raw=true)
```

### 安装方式

安装方法很简单, 只需将 .7z 文件解压到您的SPT游戏根目录即可
安装完成后，您的文件结构应如下所示：
```
你的SPT游戏根目录\SPT\user\mods\RaidRecord\(模组的任何文件)
```

### 致谢

- 感谢SPT团队提供的框架与文档。
- 感谢DrakiaXYZ, HiddenCirno, jbs4bmx, GhostFenix̵̮̀x̴̹̃©, Dsnyder | WTT以及其他所有在社区中分享经验、代码与耐心解答问题的开发者们。
- 感谢您下载并尝试本模组。

## 配置与数据

### 设置

1. 推荐方案, 直接在WebUI更改

   ```
   ![Quick buy](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/webui_setting_ch.png?raw=true)
   ```
2. 前往…/SPT/user/mods/RaidRecord/db 并打开“config.json”
3. 设置`local`为db/locals文件夹下存在的翻译文件的二位名称, 例如"cn"
   > 更改任何语言后如果无效, 应该在Launcher清理本地缓存后再启动客户端
4. 设置`logPath`以更改模组内一些日志的输出目录(这是为了避免模组报错信息导致SPT服务端日志过于繁琐的问题)
5. 设置`autoUnloadOtherLanguages`, 以启用`0.6.4`开始的对多语言化的优化功能
6. 设置`priceCacheUpdateMinTime`, 以更改模组价格缓存的更新间隔, 该设置不会影响`price`命令, `price`只会立刻获取当前模组计算的价格
7. 设置`modGiveIsFIR`, 以修改模组给的物资(在模组购买物品, 快速起装)是否是FIR(对局中发现(带勾))状态

### 数据库说明

> …/SPT/user/mods/RaidRecord/db 文件夹

**~0.6.1**
locals文件夹下为翻译文件
records文件夹(运行过模组后才会创建)为不同账户的记录文件
config.json保存模组配置

安装与数据库的兼容性参考本模组的**Version**中的详细信息

## 命令与语言

### 命令系统

> 所有命令和参数对大小写不敏感, 但推荐全部使用小写字母
> 指令使用方式为`命令键 参数1 参数1的值 参数2 参数2的值 尾缀参数`
> 字符串类型参数的值可以用半角英文双引号包起来以输入空格

进入对局后, 找到一个名为**对局战绩管理**的好友, 对他发送`help`以获取指令的帮助信息

当前版本支持以下指令:
- help: 获取所有命令帮助信息
- cls: 清理对话框聊天记录(推荐多用用)
- info: 获取指定对局收益, 击杀等信息
- items: 获取指定对局物资变化或带入带出清单, 可以限定只输出价格变化量处于[ge, le]之间的物品清单
- list: 列出当前已有的所有对局记录, 页数越靠后, 对局越新; 可以通过`limit`参数调整每页显示数量
- price: 获取指定物品价值, 或者通过名称模糊搜索多个物品价值
- buy: 快速购买指定对局进入对局时的装备(局内可以输入一个buy快速购买上局装备)

### 本地化方式

**AI辅助翻译**
1. 复制一份`ch.json`或`en.json`, 重命名为对应语言的二位名称, 例如"cz.json"(最好与Game Folder\SPT\SPT_Data\database\locales\global\**.json)的名称对应
2. 将翻译文件发送给AI, 要求它保留格式的翻译;
    - 如果AI进行了任何翻译键的操作, 打断他, 并在提示词中要求AI只翻译json的值
    - 如果AI修改了任何{{}}中的内容, 打断他, 并在提示词中要求AI禁止翻译`{{}}`与其中的值
3. 验证翻译结果是否正确, 上述操作基本能进行70%以上的翻译
4. 检查翻译文件`"translations"`键下的各个表的列的变量是否与名称对应, 以防止AI改变语序(列名和表名顺序一起改变是可以的)
5. 检查是否漏了`\n`

**手动翻译**
1. 可以在`Game Folder\SPT\SPT_Data\database\locales\global\**.json`通过找到的翻译
    - "roleNames": 搜索`BotRole`和`ScavRole`
    - "armorZone": 搜索`DeathInfo`, `Collider Type`(推荐), `Armor Zone`, `HeadSegment`
2. 急需使用命令, 优先翻译"translations"下的值
3. 急需查看服务端输出的日志, 优先翻译"serverMessage"下的值

{.endtabset}