using System.Reflection;
using System.Text.Json.Serialization;

namespace RaidRecord.Core.Locals;

public class WebUILocal
{
    #region 通用文本
    /// <summary>连接标签</summary>
    [JsonPropertyName("linkTag")]
    public string LinkTag { get; set; } = "";
    /// <summary>和</summary>
    [JsonPropertyName("and")]
    public string And { get; set; } = "和";
    /// <summary>全部</summary>
    [JsonPropertyName("all")]
    public string All { get; set; } = "全部";
    /// <summary>基础信息</summary>
    [JsonPropertyName("baseInfo")]
    public string BaseInfo { get; set; } = "基础信息";
    /// <summary>名字</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "名字";
    /// <summary>确认</summary>
    [JsonPropertyName("confirm")]
    public string Confirm { get; set; } = "确认";
    /// <summary>确认支付</summary>
    [JsonPropertyName("confirmPayment")]
    public string ConfirmPayment { get; set; } = "确认支付";
    /// <summary>立即购买</summary>
    [JsonPropertyName("NowPayment")]
    public string NowPayment { get; set; } = "立即购买";
    /// <summary>取消</summary>
    [JsonPropertyName("cancel")]
    public string Cancel { get; set; } = "取消";
    /// <summary>每页行数: </summary>
    [JsonPropertyName("tablePagerRowsPerPageString")]
    public string TablePagerRowsPerPageString { get; set; } = "每页行数: ";
    /// <summary>{first_item}-{last_item} / {all_items}</summary>
    [JsonPropertyName("tablePagerInfoFormat")]
    public string TablePagerInfoFormat { get; set; } = "{first_item}-{last_item} / {all_items}";
    /// <summary>选择的战绩不存在</summary>
    [JsonPropertyName("choiceArchiveNotExist")]
    public string ChoiceArchiveNotExist { get; set; } = "选择的战绩不存在";
    /// <summary>已成功购买物品: {{Name}} x{{StackObjectsCount}}, 消耗{{TotalPrice}}rub</summary>
    [JsonPropertyName("successBuyItemMsg")]
    public string SuccessBuyItemMsg { get; set; } = "已成功购买物品: {{Name}} x{{StackObjectsCount}}, 消耗{{TotalPrice}}rub";
    /// <summary>已将索引{{Index}}(ServerId:{{ServerId}})加入详情信息标签页</summary>
    [JsonPropertyName("addInfoTabSuccess")]
    public string AddInfoTabSuccess { get; set; } = "已将索引{{Index}}(ServerId:{{ServerId}})加入详情信息标签页";
    /// <summary>添加索引{{Index}}(ServerId:{{ServerId}})失败, 请检测是否已添加(重复添加也会该消息触发)</summary>
    [JsonPropertyName("addInfoTabFail")]
    public string AddInfoTabFail { get; set; } = "添加索引{{Index}}(ServerId:{{ServerId}})失败, 请检测是否已添加(重复添加也会该消息触发)";
    /// <summary>战绩</summary>
    [JsonPropertyName("archive")]
    public string Archive { get; set; } = "战绩";
    /// <summary>存档</summary>
    [JsonPropertyName("profile")]
    public string Profile { get; set; } = "存档";
    /// <summary>时间</summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = "时间";
    /// <summary>物品</summary>
    [JsonPropertyName("item")]
    public string Item { get; set; } = "物品";
    /// <summary>列表</summary>
    [JsonPropertyName("list")]
    public string List { get; set; } = "列表";
    /// <summary>当前</summary>
    [JsonPropertyName("curr")]
    public string Curr { get; set; } = "当前";
    /// <summary>价值</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "价值";
    /// <summary>添加</summary>
    [JsonPropertyName("add")]
    public string Add { get; set; } = "添加";
    /// <summary>移除</summary>
    [JsonPropertyName("remove")]
    public string Remove { get; set; } = "移除";
    /// <summary>变化</summary>
    [JsonPropertyName("change")]
    public string Change { get; set; } = "变化";
    /// <summary>职业</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "职业";
    /// <summary>武器</summary>
    [JsonPropertyName("weapon")]
    public string Weapon { get; set; } = "武器";
    /// <summary>身体部位</summary>
    [JsonPropertyName("bodyPart")]
    public string BodyPart { get; set; } = "身体部位";
    /// <summary>距离</summary>
    [JsonPropertyName("distance")]
    public string Distance { get; set; } = "距离";
    /// <summary>玩家击杀信息</summary>
    [JsonPropertyName("victimInfo")]
    public string VictimInfo { get; set; } = "玩家击杀信息";
    /// <summary>折叠</summary>
    [JsonPropertyName("collapse")]
    public string Collapse { get; set; } = "折叠";
    /// <summary>展开</summary>
    [JsonPropertyName("expand")]
    public string Expand { get; set; } = "展开";
    /// <summary>攻击者信息</summary>
    [JsonPropertyName("aggressorInfo")]
    public string AggressorInfo { get; set; } = "攻击者信息";
    /// <summary>攻击者信息加载失败</summary>
    [JsonPropertyName("aggressorInfoLoadFail")]
    public string AggressorInfoLoadFail { get; set; } = "攻击者信息加载失败";
    
    #endregion

    #region List页面名称翻译
    /// <summary>战绩列表</summary>
    [JsonPropertyName("ListTitle")]
    public string ListTitle { get; set; } = "战绩列表";
    /// <summary>点击信息按钮后是否跳转到战绩详情</summary>
    [JsonPropertyName("willClickOpenJump")]
    public string WillClickOpenJump { get; set; } = "点击信息按钮后是否跳转到战绩详情";
    /// <summary>阵营</summary>
    [JsonPropertyName("side")]
    public string Side { get; set; } = "阵营";
    /// <summary>地图</summary>
    [JsonPropertyName("map")]
    public string Map { get; set; } = "地图";
    /// <summary>创建时间</summary>
    [JsonPropertyName("createTime")]
    public string CreateTime { get; set; } = "创建时间";
    /// <summary>进入战局带入</summary>
    [JsonPropertyName("preRaidValue")]
    public string PreRaidValue { get; set; } = "进入战局带入";
    /// <summary>战备价值</summary>
    [JsonPropertyName("equipmentValue")]
    public string EquipmentValue { get; set; } = "战备价值";
    /// <summary>毛利润</summary>
    [JsonPropertyName("grossProfit")]
    public string GrossProfit { get; set; } = "毛利润";
    /// <summary>净利润</summary>
    [JsonPropertyName("netProfit")]
    public string NetProfit { get; set; } = "净利润";
    /// <summary>战损</summary>
    [JsonPropertyName("combatLoss")]
    public string CombatLoss { get; set; } = "战损";
    /// <summary>击杀数</summary>
    [JsonPropertyName("killCount")]
    public string KillCount { get; set; } = "击杀数";
    /// <summary>结果</summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = "结果";
    /// <summary>工具栏</summary>
    [JsonPropertyName("toolBar")]
    public string ToolBar { get; set; } = "工具栏";
    /// <summary>信息</summary>
    [JsonPropertyName("info")]
    public string Info { get; set; } = "信息";
    /// <summary>快捷起装</summary>
    [JsonPropertyName("quickEquip")]
    public string QuickEquip { get; set; } = "快捷起装";
    /// <summary>点击其他区域关闭</summary>
    [JsonPropertyName("clickOtherZoneToClose")]
    public string ClickOtherZoneToClose { get; set; } = "点击其他区域关闭";
    /// <summary>工具菜单</summary>
    [JsonPropertyName("toolMenu")]
    public string ToolMenu { get; set; } = "工具菜单";
    #endregion

    #region 战绩详情页面
    /// <summary>没有选择显示任何对局详情, 请在战绩列表中选择, 或者点击右上角的加号</summary>
    [JsonPropertyName("noArchiveWarn")]
    public string NoArchiveWarn { get; set; } = "没有选择显示任何对局详情, 请在战绩列表中选择, 或者点击右上角的加号";
    /// <summary>请输入你要查看的索引</summary>
    [JsonPropertyName("inputIndexUWantLook")]
    public string InputIndexUWantLook { get; set; } = "请输入你要查看的索引";
    /// <summary>修复当前存档所有对局战损与收益计算</summary>
    [JsonPropertyName("fixAllArchiveCombatLoss")]
    public string FixAllArchiveCombatLoss { get; set; } = "修复当前存档所有对局战损与收益计算";
    #endregion

    #region 对局详情控件
    /// <summary>对局时间</summary>
    [JsonPropertyName("archiveTime")]
    public string ArchiveTime { get; set; } = "对局时间";
    /// <summary>对局结果</summary>
    [JsonPropertyName("archiveResult")]
    public string ArchiveResult { get; set; } = "对局结果";
    /// <summary>玩家昵称</summary>
    [JsonPropertyName("playerNickName")]
    public string PlayerNickName { get; set; } = "玩家昵称";
    /// <summary>玩家等级</summary>
    [JsonPropertyName("playerLevel")]
    public string PlayerLevel { get; set; } = "玩家等级";
    /// <summary>玩家ID</summary>
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = "玩家ID";
    /// <summary>地图名称</summary>
    [JsonPropertyName("mapName")]
    public string MapName { get; set; } = "地图名称";
    /// <summary>存活时间</summary>
    [JsonPropertyName("surviveTime")]
    public string SurviveTime { get; set; } = "存活时间";
    /// <summary>撤离点名称</summary>
    [JsonPropertyName("exitName")]
    public string ExitName { get; set; } = "撤离点名称";
    /// <summary>生存风格</summary>
    [JsonPropertyName("survivorClass")]
    public string SurvivorClass { get; set; } = "生存风格";
    /// <summary>进入战局</summary>
    [JsonPropertyName("enterArchive")]
    public string EnterArchive { get; set; } = "进入战局";
    /// <summary>安全箱内物品价值</summary>
    [JsonPropertyName("securedValue")]
    public string SecuredValue { get; set; } = "安全箱内物品价值";
    /// <summary>离开战局</summary>
    [JsonPropertyName("exitArchive")]
    public string ExitArchive { get; set; } = "离开战局";
    /// <summary>击杀信息</summary>
    [JsonPropertyName("killInfo")]
    public string KillInfo { get; set; } = "击杀信息";
    /// <summary>爆头率</summary>
    [JsonPropertyName("headshotRate")]
    public string HeadshotRate { get; set; } = "爆头率";
    
    #endregion

    #region 快速购买控件
    /// <summary>装备信息</summary>
    [JsonPropertyName("equipmentInfo")]
    public string EquipmentInfo { get; set; } = "装备信息";
    /// <summary>已选择总价值</summary>
    [JsonPropertyName("choiceTotalValue")]
    public string ChoiceTotalValue { get; set; } = "已选择总价值";
    /// <summary>武器配件</summary>
    [JsonPropertyName("weaponMod")]
    public string WeaponMod { get; set; } = "武器配件";
    /// <summary>装备配件</summary>
    [JsonPropertyName("equipMod")]
    public string EquipMod { get; set; } = "装备配件";
    /// <summary>背包</summary>
    [JsonPropertyName("backpack")]
    public string Backpack { get; set; } = "背包";
    /// <summary>头部装备</summary>
    [JsonPropertyName("head")]
    public string Head { get; set; } = "头部装备";
    /// <summary>护甲</summary>
    [JsonPropertyName("armor")]
    public string Armor { get; set; } = "护甲";
    /// <summary>胸挂</summary>
    [JsonPropertyName("vest")]
    public string Vest { get; set; } = "胸挂";
    /// <summary>{{ServerId}}的装备</summary>
    [JsonPropertyName("equipOwn")]
    public string EquipOwn { get; set; } = "{{ServerId}}的装备";
    /// <summary>当前存档没有储存的装备信息</summary>
    [JsonPropertyName("noEquipInfoInThisArchive")]
    public string NoEquipInfoInThisArchive { get; set; } = "当前存档没有储存的装备信息";
    /// <summary>请确认是否消耗{{TotalPrice}}rub购买装备</summary>
    [JsonPropertyName("beforePaymentTip")]
    public string BeforePaymentTip { get; set; } = "请确认是否消耗{{TotalPrice}}rub购买装备";
    /// <summary>已启用修复耐久</summary>
    [JsonPropertyName("alreadyEnableDurabilityRepair")]
    public string AlreadyEnableDurabilityRepair { get; set; } = "已启用修复耐久";
    /// <summary>排除物品</summary>
    [JsonPropertyName("excludeItem")]
    public string ExcludeItem { get; set; } = "排除物品";
    /// <summary>在此处选中的物品将被在购买时排除</summary>
    [JsonPropertyName("excludeItemDesc")]
    public string ExcludeItemDesc { get; set; } = "在此处选中的物品将被在购买时排除";
    /// <summary>选择你需要排除的物品</summary>
    [JsonPropertyName("selectItemToExclude")]
    public string SelectItemToExclude { get; set; } = "选择你需要排除的物品";
    #endregion
    
    #region 价格页面
    /// <summary>模组价格</summary>
    [JsonPropertyName("modPrice")]
    public string ModPrice { get; set; } = "模组价格";
    /// <summary>市场平均价格</summary>
    [JsonPropertyName("ragfairAveragePrice")]
    public string RagfairAveragePrice { get; set; } = "市场平均价格";
    /// <summary>动态价格</summary>
    [JsonPropertyName("dynPrice")]
    public string DynPrice { get; set; } = "动态价格";
    /// <summary>手册价格</summary>
    [JsonPropertyName("handbookPrice")]
    public string HandbookPrice { get; set; } = "手册价格";
    /// <summary>相似度得分</summary>
    [JsonPropertyName("similarity")]
    public string Similarity { get; set; } = "相似度得分";
    /// <summary>价格页面</summary>
    [JsonPropertyName("priceTitle")]
    public string PriceTitle { get; set; } = "价格页面";
    /// <summary>可以用于模糊搜索物品价格, 以及快速购买物品</summary>
    [JsonPropertyName("pricePageDesc")]
    public string PricePageDesc { get; set; } = "可以用于模糊搜索物品价格, 以及快速购买物品";
    /// <summary>购买此物品</summary>
    [JsonPropertyName("buyThisItem")]
    public string BuyThisItem { get; set; } = "购买此物品";
    /// <summary>输入购买{{Name}}的数量</summary>
    [JsonPropertyName("buyItemTitle")]
    public string BuyItemTitle { get; set; } = "输入购买{{Name}}的数量";
    /// <summary>此处不会处理带有内置槽位的物品, 请不要购买带有内置槽位的物品</summary>
    [JsonPropertyName("buyItemWarn1")]
    public string BuyItemWarn1 { get; set; } = "此处不会处理带有内置槽位的物品, 请不要购买带有内置槽位的物品";
    /// <summary>这是消耗你存档的卢布进行购买, 不是零元购</summary>
    [JsonPropertyName("buyItemWarn2")]
    public string BuyItemWarn2 { get; set; } = "这是消耗你存档的卢布进行购买, 不是零元购";
    /// <summary>单价</summary>
    [JsonPropertyName("perPrice")]
    public string PerPrice { get; set; } = "单价";
    #endregion
    
    #region 设置页面
    /// <summary>模组配置</summary>
    [JsonPropertyName("settingTitle")]
    public string SettingTitle { get; set; } = "模组配置";
    /// <summary>选择语言</summary>
    [JsonPropertyName("selectLang")]
    public string SelectLang { get; set; } = "选择语言";
    /// <summary>自动卸载多余语言包信息</summary>
    [JsonPropertyName("autoUnloadOtherLang")]
    public string AutoUnloadLang { get; set; } = "自动卸载多余语言包信息";
    /// <summary>自动卸载多余语言包提示信息</summary>
    [JsonPropertyName("autoUnloadOtherLangDesc")]
    public string AutoUnloadLangDesc { get; set; } = "在模组加载完毕后卸载当前语言与中文以外的语言包内存以节省空间";
    /// <summary>价格缓存更新间隔(毫秒)</summary>
    [JsonPropertyName("priceCacheUpdateMinTime")]
    public string PriceCacheUpdateMinTime { get; set; } = "价格缓存更新间隔(毫秒)";
    /// <summary>保存配置修改</summary>
    [JsonPropertyName("saveConfigChange")]
    public string SaveConfigChange { get; set; } = "保存配置修改";
    /// <summary>保存配置成功(当前语言为 {{CurrLang})</summary>
    [JsonPropertyName("saveConfigSuccess")]
    public string SaveConfigSuccess { get; set; } = "保存配置成功(当前语言为 {{CurrLang})";
    /// <summary>取消配置修改</summary>
    [JsonPropertyName("cancelConfigChange")]
    public string CancelConfigChange { get; set; } = "取消配置修改";
    /// <summary>ModGiveIsFIR</summary>
    [JsonPropertyName("modGiveIsFIR")]
    public string ModGiveIsFIR { get; set; } = "模组购买FIR为状态";
    /// <summary>ModGiveIsFIRDesc</summary>
    [JsonPropertyName("modGiveIsFIRDesc")]
    public string ModGiveIsFIRDesc { get; set; } = "模组给的物资(在模组购买物品, 快速起装)是否是FIR(对局中发现(带勾))状态";
    /// <summary>重新初始化语言</summary>
    [JsonPropertyName("reInitLang")]
    public string ReInitLang { get; set; } = "重新初始化语言";
    /// <summary>重新初始化语言</summary>
    [JsonPropertyName("reInitLangDesc")]
    public string ReInitLangDesc { get; set; } = "重新初始化语言, 重新构建本地化字典, 重新获取SPT本地化数据";
    /// <summary>是否使用暗色模式</summary>
    [JsonPropertyName("isDarkMode")]
    public string IsDarkMode { get; set; } = "是否使用暗色模式";
    /// <summary>快速起装页面购买物品时是否修复物品耐久</summary>
    [JsonPropertyName("isRepairDurability")]
    public string IsRepairDurability { get; set; } = "是否修复耐久";
    /// <summary>快速起装页面购买物品时是否修复物品耐久</summary>
    [JsonPropertyName("isRepairDurabilityDesc")]
    public string IsRepairDurabilityDesc { get; set; } = "快速起装页面购买物品时是否修复物品耐久";
    /// <summary>修复全部存档结束</summary>
    [JsonPropertyName("fixAllArchiveCompleted")]
    public string FixAllArchiveCompleted { get; set; } = "修复全部存档结束";
    #endregion
    
    #region 导航栏链接文本
    /// <summary>主页</summary>
    [JsonPropertyName("navLinkRaidRecord")]
    public string NavLinkHome { get; set; } = "主页";
    /// <summary>战绩列表</summary>
    [JsonPropertyName("navLinkList")]
    public string NavLinkList { get; set; } = "战绩列表";
    /// <summary>战绩详情</summary>
    [JsonPropertyName("navLinkInfo")]
    public string NavLinkInfo { get; set; } = "战绩详情";
    /// <summary>价格页面</summary>
    [JsonPropertyName("navLinkPrice")]
    public string NavLinkPrice { get; set; } = "价格页面";
    /// <summary>设置</summary>
    [JsonPropertyName("navLinkRaidSettings")]
    public string NavLinkSettings { get; set; } = "设置";
    /// <summary>关于</summary>
    [JsonPropertyName("navLinkAbout")]
    public string NavLinkAbout { get; set; } = "关于";
    #endregion

    #region 主页文本
    /// <summary>选择存档</summary>
    [JsonPropertyName("choiceProfile")]
    public string ChoiceProfile { get; set; } = "选择存档";
    #endregion

    #region 存档信息控件
    /// <summary>昵称</summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = "昵称";
    /// <summary>简称</summary>
    [JsonPropertyName("lowerNickname")]
    public string LowerNickname { get; set; } = "简称";
    /// <summary>等级</summary>
    [JsonPropertyName("level")]
    public string Level { get; set; } = "等级";
    /// <summary>游戏版本</summary>
    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = "游戏版本";
    #endregion

    #region SPT信息控件
    /// <summary>创建存档时使用的SPT版本</summary>
    [JsonPropertyName("titleCreateProfileSptVersion")]
    public string TitleCreateProfileSptVersion { get; set; } = "创建存档时使用的SPT版本";
    /// <summary>历史加载的模组信息</summary>
    [JsonPropertyName("historyLoadModInfo")]
    public string HistoryLoadModInfo { get; set; } = "历史加载的模组信息";
    /// <summary>版本</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "版本";
    /// <summary>作者</summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = "作者";
    #endregion

    #region 理赔控件

    /// <summary>理赔</summary>
    [JsonPropertyName("claimCompensation")]
    public string ClaimCompensation { get; set; } = "理赔";
    /// <summary>理赔</summary>
    [JsonPropertyName("claimCompensationDesc")]
    public string ClaimCompensationDesc { get; set; } = "用来一键获取由于模组损失的卢布， 或者移除意外获取的卢布";
    /// <summary>输入你需要获得的卢布数值</summary>
    [JsonPropertyName("enterAmountNeedReceive")]
    public string EnterAmountNeedReceive { get; set; } = "输入你需要获得的卢布数值";
    /// <summary>输入你希望消费的卢布数值</summary>
    [JsonPropertyName("enterAmountWishConsume")]
    public string EnterAmountWishConsume { get; set; } = "输入你希望消费的卢布数值";
    /// <summary>您已通过理赔获取</summary>
    [JsonPropertyName("receivedThroughClaims")]
    public string ReceivedThroughClaims { get; set; } = "您已通过理赔获取";
    /// <summary>您已通过理赔消费</summary>
    [JsonPropertyName("consumedThroughClaims")]
    public string ConsumedThroughClaims { get; set; } = "您已通过理赔消费";
    
    #endregion
    
    /// <summary> 连接多个字符串 </summary>
    public string Link(params string[] args) => string.Join(LinkTag, args);

    /// <summary>
    /// 将所有公共 string 属性以 JsonPropertyName（如有）作为键，属性值作为值，输出为字典。
    /// 若无 JsonPropertyName，则使用属性名。
    /// </summary>
    public Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>();
        PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in properties)
        {
            if (prop.PropertyType != typeof(string)) continue;
            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            string key = jsonPropAttr?.Name ?? prop.Name;
            string value = (string?)prop.GetValue(this) ?? string.Empty;
            dict[key] = value;
        }

        return dict;
    }
}