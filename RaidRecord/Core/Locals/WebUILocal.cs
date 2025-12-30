using System.Reflection;
using System.Text.Json.Serialization;

namespace RaidRecord.Core.Locals;

public class WebUILocal
{
    #region 导航栏链接文本
    [JsonPropertyName("navLinkRaidRecord")]
    public string NavLinkHome { get; set; } = "主页";
    [JsonPropertyName("navLinkList")]
    public string NavLinkList { get; set; } = "战绩列表";
    [JsonPropertyName("navLinkInfo")]
    public string NavLinkInfo { get; set; } = "战绩详情";
    [JsonPropertyName("navLinkPrice")]
    public string NavLinkPrice { get; set; } = "价格页面";
    [JsonPropertyName("navLinkRaidSettings")]
    public string NavLinkSettings { get; set; } = "设置";
    [JsonPropertyName("navLinkAbout")]
    public string NavLinkAbout { get; set; } = "关于";
    #endregion

    #region 主页文本
    [JsonPropertyName("choiceProfile")]
    public string ChoiceProfile { get; set; } = "选择存档";
    #endregion
    
    
    
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